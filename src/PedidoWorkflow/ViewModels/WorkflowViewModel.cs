using PedidoWorkflow.Models;

namespace PedidoWorkflow.ViewModels;

public sealed class WorkflowViewModel
{
    public IReadOnlyList<EstadoPedido> Estados { get; init; } = [];
    public IReadOnlyList<TransicaoWorkflow> Transicoes { get; init; } = [];
    public WorkflowStateInputModel NovoEstado { get; init; } = new();
    public WorkflowTransitionInputModel NovaTransicao { get; init; } = new();
}
