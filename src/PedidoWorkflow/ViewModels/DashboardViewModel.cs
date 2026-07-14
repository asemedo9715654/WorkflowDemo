using PedidoWorkflow.Models;

namespace PedidoWorkflow.ViewModels;

public sealed class DashboardViewModel
{
    public DashboardStats Stats { get; init; } = new();
    public IReadOnlyList<Pedido> PedidosRecentes { get; init; } = [];
    public IReadOnlyList<EstadoPedido> Estados { get; init; } = [];
}
