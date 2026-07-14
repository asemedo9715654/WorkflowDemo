using Microsoft.Data.SqlClient;
using PedidoWorkflow.Models;
using PedidoWorkflow.ViewModels;

namespace PedidoWorkflow.Data;

public sealed class WorkflowRepository
{
    private readonly SqlConnectionFactory _connectionFactory;
    private readonly AppStorageMode _storageMode;
    private readonly InMemoryStore _inMemoryStore;

    public WorkflowRepository(
        SqlConnectionFactory connectionFactory,
        AppStorageMode storageMode,
        InMemoryStore inMemoryStore)
    {
        _connectionFactory = connectionFactory;
        _storageMode = storageMode;
        _inMemoryStore = inMemoryStore;
    }

    public async Task<IReadOnlyList<EstadoPedido>> GetStatesAsync()
    {
        if (_storageMode.UseInMemory)
        {
            return _inMemoryStore.GetStates();
        }

        var estados = new List<EstadoPedido>();

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, Nome, Descricao, Ativo, Inicial, CriadoEmUtc
            FROM dbo.EstadosPedido
            ORDER BY Inicial DESC, Nome;
            """;

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

    public async Task<IReadOnlyList<TransicaoWorkflow>> GetTransitionsAsync()
    {
        if (_storageMode.UseInMemory)
        {
            return _inMemoryStore.GetTransitions();
        }

        var transicoes = new List<TransicaoWorkflow>();

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                transicao.Id,
                transicao.Label,
                transicao.EstadoOrigemId,
                origem.Nome,
                transicao.EstadoDestinoId,
                destino.Nome,
                transicao.Ativa
            FROM dbo.TransicoesWorkflow transicao
            INNER JOIN dbo.EstadosPedido origem ON origem.Id = transicao.EstadoOrigemId
            INNER JOIN dbo.EstadosPedido destino ON destino.Id = transicao.EstadoDestinoId
            ORDER BY origem.Nome, destino.Nome;
            """;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            transicoes.Add(new TransicaoWorkflow
            {
                Id = reader.GetInt32(0),
                Label = reader.GetString(1),
                EstadoOrigemId = reader.GetInt32(2),
                EstadoOrigemNome = reader.GetString(3),
                EstadoDestinoId = reader.GetInt32(4),
                EstadoDestinoNome = reader.GetString(5),
                Ativa = reader.GetBoolean(6)
            });
        }

        return transicoes;
    }

    public async Task AddStateAsync(WorkflowStateInputModel input)
    {
        if (_storageMode.UseInMemory)
        {
            _inMemoryStore.AddState(input);
            await Task.CompletedTask;
            return;
        }

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            if (input.Inicial)
            {
                var clearCommand = connection.CreateCommand();
                clearCommand.Transaction = (SqlTransaction)transaction;
                clearCommand.CommandText = "UPDATE dbo.EstadosPedido SET Inicial = 0;";
                await clearCommand.ExecuteNonQueryAsync();
            }

            var command = connection.CreateCommand();
            command.Transaction = (SqlTransaction)transaction;
            command.CommandText =
                """
                INSERT INTO dbo.EstadosPedido (Nome, Descricao, Ativo, Inicial, CriadoEmUtc)
                VALUES (@Nome, @Descricao, @Ativo, @Inicial, SYSUTCDATETIME());
                """;
            command.Parameters.AddWithValue("@Nome", input.Nome.Trim());
            command.Parameters.AddWithValue("@Descricao", input.Descricao.Trim());
            command.Parameters.AddWithValue("@Ativo", input.Ativo);
            command.Parameters.AddWithValue("@Inicial", input.Inicial);
            await command.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task UpdateStateAsync(WorkflowStateInputModel input)
    {
        if (_storageMode.UseInMemory)
        {
            _inMemoryStore.UpdateState(input);
            await Task.CompletedTask;
            return;
        }

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            if (input.Inicial)
            {
                var clearCommand = connection.CreateCommand();
                clearCommand.Transaction = (SqlTransaction)transaction;
                clearCommand.CommandText = "UPDATE dbo.EstadosPedido SET Inicial = 0;";
                await clearCommand.ExecuteNonQueryAsync();
            }

            var command = connection.CreateCommand();
            command.Transaction = (SqlTransaction)transaction;
            command.CommandText =
                """
                UPDATE dbo.EstadosPedido
                SET Nome = @Nome,
                    Descricao = @Descricao,
                    Ativo = @Ativo,
                    Inicial = @Inicial
                WHERE Id = @Id;
                """;
            command.Parameters.AddWithValue("@Id", input.Id);
            command.Parameters.AddWithValue("@Nome", input.Nome.Trim());
            command.Parameters.AddWithValue("@Descricao", input.Descricao.Trim());
            command.Parameters.AddWithValue("@Ativo", input.Ativo);
            command.Parameters.AddWithValue("@Inicial", input.Inicial);
            await command.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task AddTransitionAsync(WorkflowTransitionInputModel input)
    {
        if (_storageMode.UseInMemory)
        {
            _inMemoryStore.AddTransition(input);
            await Task.CompletedTask;
            return;
        }

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
            """
            IF NOT EXISTS (
                SELECT 1
                FROM dbo.TransicoesWorkflow
                WHERE EstadoOrigemId = @EstadoOrigemId
                  AND EstadoDestinoId = @EstadoDestinoId
            )
            BEGIN
                INSERT INTO dbo.TransicoesWorkflow (Label, EstadoOrigemId, EstadoDestinoId, Ativa)
                VALUES (@Label, @EstadoOrigemId, @EstadoDestinoId, 1);
            END
            """;
        command.Parameters.AddWithValue("@Label", input.Label.Trim());
        command.Parameters.AddWithValue("@EstadoOrigemId", input.EstadoOrigemId);
        command.Parameters.AddWithValue("@EstadoDestinoId", input.EstadoDestinoId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task ToggleTransitionAsync(int transitionId)
    {
        if (_storageMode.UseInMemory)
        {
            _inMemoryStore.ToggleTransition(transitionId);
            await Task.CompletedTask;
            return;
        }

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE dbo.TransicoesWorkflow
            SET Ativa = CASE WHEN Ativa = 1 THEN 0 ELSE 1 END
            WHERE Id = @Id;
            """;
        command.Parameters.AddWithValue("@Id", transitionId);
        await command.ExecuteNonQueryAsync();
    }
}
