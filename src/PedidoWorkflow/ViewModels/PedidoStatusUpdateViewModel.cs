using System.ComponentModel.DataAnnotations;

namespace PedidoWorkflow.ViewModels;

public sealed class PedidoStatusUpdateViewModel
{
    public int PedidoId { get; set; }

    [Required(ErrorMessage = "Escolha o proximo estado.")]
    public int NovoEstadoId { get; set; }

    [StringLength(250)]
    public string Observacao { get; set; } = string.Empty;
}
