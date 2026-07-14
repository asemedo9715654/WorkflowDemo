namespace PedidoWorkflow.Models;

public sealed class Pedido
{
    public int Id { get; init; }
    public string Numero { get; init; } = string.Empty;
    public string Cliente { get; init; } = string.Empty;
    public string EmailCliente { get; init; } = string.Empty;
    public decimal ValorTotal { get; init; }
    public string Observacoes { get; init; } = string.Empty;
    public int EstadoAtualId { get; init; }
    public string EstadoAtualNome { get; init; } = string.Empty;
    public bool EstadoAtualFinal { get; init; }
    public DateTime CriadoEmUtc { get; init; }
    public DateTime AtualizadoEmUtc { get; init; }
}
