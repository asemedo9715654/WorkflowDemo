namespace PedidoWorkflow.Models;

public sealed class TransicaoWorkflow
{
    public int Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public int EstadoOrigemId { get; init; }
    public string EstadoOrigemNome { get; init; } = string.Empty;
    public int EstadoDestinoId { get; init; }
    public string EstadoDestinoNome { get; init; } = string.Empty;
    public bool Ativa { get; init; }
}
