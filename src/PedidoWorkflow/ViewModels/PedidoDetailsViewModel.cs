using PedidoWorkflow.Models;

namespace PedidoWorkflow.ViewModels;

public sealed class PedidoDetailsViewModel
{
    public Pedido Pedido { get; init; } = new();
    public IReadOnlyList<HistoricoPedido> Historico { get; init; } = [];
    public IReadOnlyList<EstadoPedido> ProximosEstados { get; init; } = [];
}
