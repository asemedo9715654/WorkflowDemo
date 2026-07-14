using System.ComponentModel.DataAnnotations;

namespace PedidoWorkflow.ViewModels;

public sealed class WorkflowTransitionInputModel
{
    [Required(ErrorMessage = "Informe o label da transicao.")]
    [StringLength(120, ErrorMessage = "O label deve ter no maximo 120 caracteres.")]
    public string Label { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escolha o estado de origem.")]
    public int EstadoOrigemId { get; set; }

    [Required(ErrorMessage = "Escolha o estado de destino.")]
    public int EstadoDestinoId { get; set; }
}
