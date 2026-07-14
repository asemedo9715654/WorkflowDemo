using Microsoft.Data.SqlClient;
using PedidoWorkflow.Models;

namespace PedidoWorkflow.Data;

public sealed class DashboardRepository
{
    private readonly SqlConnectionFactory _connectionFactory;
    private readonly AppStorageMode _storageMode;
    private readonly InMemoryStore _inMemoryStore;

    public DashboardRepository(
        SqlConnectionFactory connectionFactory,
        AppStorageMode storageMode,
        InMemoryStore inMemoryStore)
    {
        _connectionFactory = connectionFactory;
        _storageMode = storageMode;
        _inMemoryStore = inMemoryStore;
    }

    public async Task<DashboardStats> GetStatsAsync()
    {
        if (_storageMode.UseInMemory)
        {
            return _inMemoryStore.GetStats();
        }

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                COUNT(*) AS TotalPedidos,
                SUM(CASE WHEN estado.Nome NOT IN (N'Entregue', N'Cancelado', N'Devolvido') THEN 1 ELSE 0 END) AS PedidosAtivos,
                SUM(CASE WHEN estado.Nome = N'Entregue' THEN 1 ELSE 0 END) AS PedidosEntregues,
                SUM(CASE WHEN estado.Nome = N'Cancelado' THEN 1 ELSE 0 END) AS PedidosCancelados
            FROM dbo.Pedidos pedido
            INNER JOIN dbo.EstadosPedido estado ON estado.Id = pedido.EstadoAtualId;
            """;

        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();

        return new DashboardStats
        {
            TotalPedidos = reader.GetInt32(0),
            PedidosAtivos = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
            PedidosEntregues = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
            PedidosCancelados = reader.IsDBNull(3) ? 0 : reader.GetInt32(3)
        };
    }

    public async Task<IReadOnlyList<Pedido>> GetRecentOrdersAsync(int limit)
    {
        if (_storageMode.UseInMemory)
        {
            return _inMemoryStore.GetRecentOrders(limit);
        }

        var pedidos = new List<Pedido>();

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT TOP (@Limit)
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
            ORDER BY pedido.CriadoEmUtc DESC;
            """;
        command.Parameters.AddWithValue("@Limit", limit);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            pedidos.Add(MapPedido(reader));
        }

        return pedidos;
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
