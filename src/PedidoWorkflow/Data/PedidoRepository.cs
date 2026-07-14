using Microsoft.Data.SqlClient;
using PedidoWorkflow.Models;
using PedidoWorkflow.ViewModels;

namespace PedidoWorkflow.Data;

public sealed class PedidoRepository
{
    private readonly SqlConnectionFactory _connectionFactory;
    private readonly AppStorageMode _storageMode;
    private readonly InMemoryStore _inMemoryStore;

    public PedidoRepository(
        SqlConnectionFactory connectionFactory,
        AppStorageMode storageMode,
        InMemoryStore inMemoryStore)
    {
        _connectionFactory = connectionFactory;
        _storageMode = storageMode;
        _inMemoryStore = inMemoryStore;
    }

    public async Task<IReadOnlyList<Pedido>> SearchAsync(string? busca)
    {
        if (_storageMode.UseInMemory)
        {
            return _inMemoryStore.SearchOrders(busca);
        }

        var pedidos = new List<Pedido>();

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                pedido.Id,
                pedido.Numero,
                pedido.Cliente,
                pedido.EmailCliente,
                pedido.ValorTotal,
                pedido.Observacoes,
                pedido.EstadoAtualId,
                estado.Nome,
                CASE WHEN estado.Nome IN (N'Entregue', N'Cancelado', N'Devolvido') THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END,
                pedido.CriadoEmUtc,
                pedido.AtualizadoEmUtc
            FROM dbo.Pedidos pedido
            INNER JOIN dbo.EstadosPedido estado ON estado.Id = pedido.EstadoAtualId
            WHERE @Busca IS NULL
               OR pedido.Numero LIKE '%' + @Busca + '%'
               OR pedido.Cliente LIKE '%' + @Busca + '%'
               OR estado.Nome LIKE '%' + @Busca + '%'
            ORDER BY pedido.AtualizadoEmUtc DESC;
            """;
        command.Parameters.AddWithValue("@Busca", string.IsNullOrWhiteSpace(busca) ? DBNull.Value : busca.Trim());

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            pedidos.Add(MapPedido(reader));
        }

        return pedidos;
    }

    public async Task<Pedido?> GetByIdAsync(int id)
    {
        if (_storageMode.UseInMemory)
        {
            return _inMemoryStore.GetOrderById(id);
        }

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                pedido.Id,
                pedido.Numero,
                pedido.Cliente,
                pedido.EmailCliente,
                pedido.ValorTotal,
                pedido.Observacoes,
                pedido.EstadoAtualId,
                estado.Nome,
                CASE WHEN estado.Nome IN (N'Entregue', N'Cancelado', N'Devolvido') THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END,
                pedido.CriadoEmUtc,
                pedido.AtualizadoEmUtc
            FROM dbo.Pedidos pedido
            INNER JOIN dbo.EstadosPedido estado ON estado.Id = pedido.EstadoAtualId
            WHERE pedido.Id = @Id;
            """;
        command.Parameters.AddWithValue("@Id", id);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return MapPedido(reader);
    }

    public async Task<IReadOnlyList<HistoricoPedido>> GetHistoryAsync(int pedidoId)
    {
        if (_storageMode.UseInMemory)
        {
            return _inMemoryStore.GetHistory(pedidoId);
        }

        var historico = new List<HistoricoPedido>();

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                historico.Id,
                historico.PedidoId,
                ISNULL(origem.Nome, N'Criacao'),
                destino.Nome,
                historico.Observacao,
                historico.AlteradoEmUtc
            FROM dbo.HistoricoPedidos historico
            LEFT JOIN dbo.EstadosPedido origem ON origem.Id = historico.EstadoOrigemId
            INNER JOIN dbo.EstadosPedido destino ON destino.Id = historico.EstadoDestinoId
            WHERE historico.PedidoId = @PedidoId
            ORDER BY historico.AlteradoEmUtc DESC;
            """;
        command.Parameters.AddWithValue("@PedidoId", pedidoId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            historico.Add(new HistoricoPedido
            {
                Id = reader.GetInt32(0),
                PedidoId = reader.GetInt32(1),
                EstadoOrigem = reader.GetString(2),
                EstadoDestino = reader.GetString(3),
                Observacao = reader.GetString(4),
                AlteradoEmUtc = reader.GetDateTime(5)
            });
        }

        return historico;
    }

    public async Task<IReadOnlyList<EstadoPedido>> GetAllowedNextStatesAsync(int estadoAtualId)
    {
        if (_storageMode.UseInMemory)
        {
            return _inMemoryStore.GetAllowedNextStates(estadoAtualId);
        }

        var estados = new List<EstadoPedido>();

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT destino.Id, destino.Nome, destino.Descricao, destino.Ativo, destino.Inicial, destino.CriadoEmUtc
            FROM dbo.TransicoesWorkflow transicao
            INNER JOIN dbo.EstadosPedido destino ON destino.Id = transicao.EstadoDestinoId
            WHERE transicao.EstadoOrigemId = @EstadoAtualId
              AND transicao.Ativa = 1
              AND destino.Ativo = 1
            ORDER BY destino.Nome;
            """;
        command.Parameters.AddWithValue("@EstadoAtualId", estadoAtualId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            estados.Add(new EstadoPedido
            {
                Id = reader.GetInt32(0),
                Nome = reader.GetString(1),
                Descricao = reader.GetString(2),
                Ativo = reader.GetBoolean(3),
                Inicial = reader.GetBoolean(4),
                CriadoEmUtc = reader.GetDateTime(5)
            });
        }

        return estados;
    }

    public async Task<int> CreateAsync(PedidoCreateViewModel input)
    {
        if (_storageMode.UseInMemory)
        {
            return _inMemoryStore.CreateOrder(input);
        }

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var estadoInicialId = await GetInitialStateIdAsync(connection, (SqlTransaction)transaction);
            var numero = $"PED-{DateTime.UtcNow:yyyyMMddHHmmssfff}";

            var command = connection.CreateCommand();
            command.Transaction = (SqlTransaction)transaction;
            command.CommandText =
                """
                INSERT INTO dbo.Pedidos (Numero, Cliente, EmailCliente, ValorTotal, Observacoes, EstadoAtualId, CriadoEmUtc, AtualizadoEmUtc)
                OUTPUT INSERTED.Id
                VALUES (@Numero, @Cliente, @EmailCliente, @ValorTotal, @Observacoes, @EstadoAtualId, SYSUTCDATETIME(), SYSUTCDATETIME());
                """;
            command.Parameters.AddWithValue("@Numero", numero);
            command.Parameters.AddWithValue("@Cliente", input.Cliente.Trim());
            command.Parameters.AddWithValue("@EmailCliente", input.EmailCliente.Trim());
            command.Parameters.AddWithValue("@ValorTotal", input.ValorTotal);
            command.Parameters.AddWithValue("@Observacoes", input.Observacoes?.Trim() ?? string.Empty);
            command.Parameters.AddWithValue("@EstadoAtualId", estadoInicialId);

            var pedidoId = (int)(await command.ExecuteScalarAsync() ?? 0);

            var historyCommand = connection.CreateCommand();
            historyCommand.Transaction = (SqlTransaction)transaction;
            historyCommand.CommandText =
                """
                INSERT INTO dbo.HistoricoPedidos (PedidoId, EstadoOrigemId, EstadoDestinoId, Observacao, AlteradoEmUtc)
                VALUES (@PedidoId, NULL, @EstadoDestinoId, N'Pedido criado no sistema.', SYSUTCDATETIME());
                """;
            historyCommand.Parameters.AddWithValue("@PedidoId", pedidoId);
            historyCommand.Parameters.AddWithValue("@EstadoDestinoId", estadoInicialId);
            await historyCommand.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
            return pedidoId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> UpdateStatusAsync(int pedidoId, int novoEstadoId, string observacao)
    {
        if (_storageMode.UseInMemory)
        {
            return _inMemoryStore.UpdateOrderStatus(pedidoId, novoEstadoId, observacao);
        }

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var currentStateCommand = connection.CreateCommand();
            currentStateCommand.Transaction = (SqlTransaction)transaction;
            currentStateCommand.CommandText =
                """
                SELECT EstadoAtualId
                FROM dbo.Pedidos
                WHERE Id = @PedidoId;
                """;
            currentStateCommand.Parameters.AddWithValue("@PedidoId", pedidoId);
            var estadoAtualId = (int?)await currentStateCommand.ExecuteScalarAsync();
            if (estadoAtualId is null)
            {
                await transaction.RollbackAsync();
                return false;
            }

            var validationCommand = connection.CreateCommand();
            validationCommand.Transaction = (SqlTransaction)transaction;
            validationCommand.CommandText =
                """
                SELECT COUNT(*)
                FROM dbo.TransicoesWorkflow
                WHERE EstadoOrigemId = @OrigemId
                  AND EstadoDestinoId = @DestinoId
                  AND Ativa = 1;
                """;
            validationCommand.Parameters.AddWithValue("@OrigemId", estadoAtualId.Value);
            validationCommand.Parameters.AddWithValue("@DestinoId", novoEstadoId);
            var allowed = (int)(await validationCommand.ExecuteScalarAsync() ?? 0) > 0;
            if (!allowed)
            {
                await transaction.RollbackAsync();
                return false;
            }

            var updateCommand = connection.CreateCommand();
            updateCommand.Transaction = (SqlTransaction)transaction;
            updateCommand.CommandText =
                """
                UPDATE dbo.Pedidos
                SET EstadoAtualId = @NovoEstadoId,
                    AtualizadoEmUtc = SYSUTCDATETIME()
                WHERE Id = @PedidoId;
                """;
            updateCommand.Parameters.AddWithValue("@NovoEstadoId", novoEstadoId);
            updateCommand.Parameters.AddWithValue("@PedidoId", pedidoId);
            await updateCommand.ExecuteNonQueryAsync();

            var historyCommand = connection.CreateCommand();
            historyCommand.Transaction = (SqlTransaction)transaction;
            historyCommand.CommandText =
                """
                INSERT INTO dbo.HistoricoPedidos (PedidoId, EstadoOrigemId, EstadoDestinoId, Observacao, AlteradoEmUtc)
                VALUES (@PedidoId, @EstadoOrigemId, @EstadoDestinoId, @Observacao, SYSUTCDATETIME());
                """;
            historyCommand.Parameters.AddWithValue("@PedidoId", pedidoId);
            historyCommand.Parameters.AddWithValue("@EstadoOrigemId", estadoAtualId.Value);
            historyCommand.Parameters.AddWithValue("@EstadoDestinoId", novoEstadoId);
            historyCommand.Parameters.AddWithValue("@Observacao", observacao.Trim());
            await historyCommand.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static async Task<int> GetInitialStateIdAsync(SqlConnection connection, SqlTransaction transaction)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            SELECT TOP (1) Id
            FROM dbo.EstadosPedido
            WHERE Inicial = 1 AND Ativo = 1
            ORDER BY Id;
            """;

        return (int)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("Nenhum estado inicial ativo foi encontrado."));
    }

    private static Pedido MapPedido(SqlDataReader reader)
    {
        return new Pedido
        {
            Id = reader.GetInt32(0),
            Numero = reader.GetString(1),
            Cliente = reader.GetString(2),
            EmailCliente = reader.GetString(3),
            ValorTotal = reader.GetDecimal(4),
            Observacoes = reader.GetString(5),
            EstadoAtualId = reader.GetInt32(6),
            EstadoAtualNome = reader.GetString(7),
            EstadoAtualFinal = reader.GetBoolean(8),
            CriadoEmUtc = reader.GetDateTime(9),
            AtualizadoEmUtc = reader.GetDateTime(10)
        };
    }
}
