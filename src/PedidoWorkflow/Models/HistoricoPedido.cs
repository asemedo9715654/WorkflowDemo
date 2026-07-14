namespace PedidoWorkflow.Models;

public sealed class HistoricoPedido
{
    public int Id { get; init; }
    public int PedidoId { get; init; }
    public string EstadoOrigem { get; init; } = string.Empty;
    public string EstadoDestino { get; init; } = string.Empty;
    public string Observacao { get; init; } = string.Empty;
    public DateTime AlteradoEmUtc { get; init; }
}
