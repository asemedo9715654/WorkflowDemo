namespace PedidoWorkflow.Models;

public sealed class DashboardStats
{
    public int TotalPedidos { get; init; }
    public int PedidosAtivos { get; init; }
    public int PedidosEntregues { get; init; }
    public int PedidosCancelados { get; init; }
}
