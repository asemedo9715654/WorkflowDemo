namespace PedidoWorkflow.Models;

public sealed class EstadoPedido
{
    public int Id { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string Descricao { get; init; } = string.Empty;
    public bool Ativo { get; init; }
    public bool Inicial { get; init; }
    public DateTime CriadoEmUtc { get; init; }
}
