using PedidoWorkflow.Models;

namespace PedidoWorkflow.ViewModels;

public sealed class PedidoListViewModel
{
    public string? Busca { get; init; }
    public IReadOnlyList<Pedido> Pedidos { get; init; } = [];
}
