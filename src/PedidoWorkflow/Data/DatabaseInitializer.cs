using Microsoft.Data.SqlClient;

namespace PedidoWorkflow.Data;

public sealed class DatabaseInitializer
{
    private readonly SqlConnectionFactory _connectionFactory;
    private readonly AppStorageMode _storageMode;
    private readonly InMemoryStore _inMemoryStore;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        SqlConnectionFactory connectionFactory,
        AppStorageMode storageMode,
        InMemoryStore inMemoryStore,
        ILogger<DatabaseInitializer> logger)
    {
        _connectionFactory = connectionFactory;
        _storageMode = storageMode;
        _inMemoryStore = inMemoryStore;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            await EnsureDatabaseExistsAsync();
            await EnsureSchemaAsync();
            await SeedWorkflowAsync();
        }
        catch (Exception ex)
        {
            _storageMode.UseInMemory = true;
            _storageMode.InitializationError = ex.Message;
            _inMemoryStore.EnsureSeeded();
            _logger.LogWarning(ex, "SQL Server indisponivel. A aplicacao sera executada com armazenamento em memoria.");
        }
    }

    private async Task EnsureDatabaseExistsAsync()
    {
        await using var connection = _connectionFactory.CreateMasterConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
            """
            IF DB_ID(N'PedidoWorkflowDb') IS NULL
            BEGIN
                CREATE DATABASE PedidoWorkflowDb;
            END
            """;

        await command.ExecuteNonQueryAsync();
    }

    private async Task EnsureSchemaAsync()
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
            """
            IF OBJECT_ID(N'dbo.EstadosPedido', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.EstadosPedido (
                    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    Nome NVARCHAR(80) NOT NULL UNIQUE,
                    Descricao NVARCHAR(200) NOT NULL,
                    Ativo BIT NOT NULL,
                    Inicial BIT NOT NULL,
                    CriadoEmUtc DATETIME2 NOT NULL
                );
            END;

            IF OBJECT_ID(N'dbo.TransicoesWorkflow', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.TransicoesWorkflow (
                    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    Label NVARCHAR(120) NOT NULL,
                    EstadoOrigemId INT NOT NULL,
                    EstadoDestinoId INT NOT NULL,
                    Ativa BIT NOT NULL,
                    CONSTRAINT FK_Transicoes_Origem FOREIGN KEY (EstadoOrigemId) REFERENCES dbo.EstadosPedido(Id),
                    CONSTRAINT FK_Transicoes_Destino FOREIGN KEY (EstadoDestinoId) REFERENCES dbo.EstadosPedido(Id),
                    CONSTRAINT UQ_Transicao UNIQUE (EstadoOrigemId, EstadoDestinoId)
                );
            END;

            IF COL_LENGTH(N'dbo.TransicoesWorkflow', N'Label') IS NULL
            BEGIN
                ALTER TABLE dbo.TransicoesWorkflow
                ADD Label NVARCHAR(120) NOT NULL CONSTRAINT DF_TransicoesWorkflow_Label DEFAULT N'Transicao';
            END;

            IF OBJECT_ID(N'dbo.Pedidos', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Pedidos (
                    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    Numero NVARCHAR(30) NOT NULL UNIQUE,
                    Cliente NVARCHAR(120) NOT NULL,
                    EmailCliente NVARCHAR(180) NOT NULL,
                    ValorTotal DECIMAL(18,2) NOT NULL,
                    Observacoes NVARCHAR(500) NOT NULL,
                    EstadoAtualId INT NOT NULL,
                    CriadoEmUtc DATETIME2 NOT NULL,
                    AtualizadoEmUtc DATETIME2 NOT NULL,
                    CONSTRAINT FK_Pedidos_Estado FOREIGN KEY (EstadoAtualId) REFERENCES dbo.EstadosPedido(Id)
                );
            END;

            IF OBJECT_ID(N'dbo.HistoricoPedidos', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.HistoricoPedidos (
                    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    PedidoId INT NOT NULL,
                    EstadoOrigemId INT NULL,
                    EstadoDestinoId INT NOT NULL,
                    Observacao NVARCHAR(250) NOT NULL,
                    AlteradoEmUtc DATETIME2 NOT NULL,
                    CONSTRAINT FK_Historico_Pedido FOREIGN KEY (PedidoId) REFERENCES dbo.Pedidos(Id),
                    CONSTRAINT FK_Historico_EstadoOrigem FOREIGN KEY (EstadoOrigemId) REFERENCES dbo.EstadosPedido(Id),
                    CONSTRAINT FK_Historico_EstadoDestino FOREIGN KEY (EstadoDestinoId) REFERENCES dbo.EstadosPedido(Id)
                );
            END;
            """;

        await command.ExecuteNonQueryAsync();
    }

    private async Task SeedWorkflowAsync()
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var defaults = new (string Nome, string Descricao, bool Inicial)[]
        {
            ("AguardandoPagamento", "Pedido criado, aguardando confirmacao do pagamento.", true),
            ("PagamentoConfirmado", "Pagamento aprovado e pronto para processamento.", false),
            ("SeparandoEstoque", "Pedido em separacao no deposito.", false),
            ("Transporte", "Pedido enviado para transporte.", false),
            ("Entregue", "Pedido recebido pelo cliente.", false),
            ("Cancelado", "Pedido cancelado no fluxo operacional.", false),
            ("Devolvido", "Pedido devolvido apos a entrega.", false)
        };

        foreach (var estado in defaults)
        {
            var command = connection.CreateCommand();
            command.CommandText =
                """
                IF NOT EXISTS (SELECT 1 FROM dbo.EstadosPedido WHERE Nome = @Nome)
                BEGIN
                    INSERT INTO dbo.EstadosPedido (Nome, Descricao, Ativo, Inicial, CriadoEmUtc)
                    VALUES (@Nome, @Descricao, 1, @Inicial, SYSUTCDATETIME());
                END
                """;
            command.Parameters.AddWithValue("@Nome", estado.Nome);
            command.Parameters.AddWithValue("@Descricao", estado.Descricao);
            command.Parameters.AddWithValue("@Inicial", estado.Inicial);
            await command.ExecuteNonQueryAsync();
        }

        var transitionCommand = connection.CreateCommand();
        transitionCommand.CommandText =
            """
            DECLARE @Transitions TABLE (Label NVARCHAR(120), Origem NVARCHAR(80), Destino NVARCHAR(80));
            INSERT INTO @Transitions (Label, Origem, Destino)
            VALUES
                (N'Confirmar pagamento', N'AguardandoPagamento', N'PagamentoConfirmado'),
                (N'Cancelar antes do pagamento', N'AguardandoPagamento', N'Cancelado'),
                (N'Enviar para separacao', N'PagamentoConfirmado', N'SeparandoEstoque'),
                (N'Cancelar apos confirmacao', N'PagamentoConfirmado', N'Cancelado'),
                (N'Liberar para transporte', N'SeparandoEstoque', N'Transporte'),
                (N'Cancelar na separacao', N'SeparandoEstoque', N'Cancelado'),
                (N'Confirmar entrega', N'Transporte', N'Entregue'),
                (N'Cancelar transporte', N'Transporte', N'Cancelado'),
                (N'Registar devolucao', N'Entregue', N'Devolvido');

            INSERT INTO dbo.TransicoesWorkflow (Label, EstadoOrigemId, EstadoDestinoId, Ativa)
            SELECT t.Label, origem.Id, destino.Id, 1
            FROM @Transitions t
            INNER JOIN dbo.EstadosPedido origem ON origem.Nome = t.Origem
            INNER JOIN dbo.EstadosPedido destino ON destino.Nome = t.Destino
            WHERE NOT EXISTS (
                SELECT 1
                FROM dbo.TransicoesWorkflow existente
                WHERE existente.EstadoOrigemId = origem.Id
                  AND existente.EstadoDestinoId = destino.Id
            );

            UPDATE transicao
            SET Label = t.Label
            FROM dbo.TransicoesWorkflow transicao
            INNER JOIN dbo.EstadosPedido origem ON origem.Id = transicao.EstadoOrigemId
            INNER JOIN dbo.EstadosPedido destino ON destino.Id = transicao.EstadoDestinoId
            INNER JOIN @Transitions t ON t.Origem = origem.Nome AND t.Destino = destino.Nome
            WHERE (transicao.Label IS NULL OR transicao.Label = N'' OR transicao.Label = N'Transicao');
            """;

        await transitionCommand.ExecuteNonQueryAsync();
    }
}
