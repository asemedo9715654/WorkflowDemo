namespace PedidoWorkflow.Models;

public sealed class ErrorViewModel
{
    public string RequestId { get; init; } = string.Empty;
    public bool ShowRequestId => !string.IsNullOrWhiteSpace(RequestId);
}
