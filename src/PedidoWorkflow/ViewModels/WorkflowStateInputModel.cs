using System.ComponentModel.DataAnnotations;

namespace PedidoWorkflow.ViewModels;

public sealed class WorkflowStateInputModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe o nome do estado.")]
    [StringLength(80)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a descricao do estado.")]
    [StringLength(200)]
    public string Descricao { get; set; } = string.Empty;

    public bool Ativo { get; set; } = true;
    public bool Inicial { get; set; }
}
