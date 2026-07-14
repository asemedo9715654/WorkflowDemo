using System.ComponentModel.DataAnnotations;
using PedidoWorkflow.Validation;

namespace PedidoWorkflow.ViewModels;

public sealed class PedidoCreateViewModel
{
    [Required(ErrorMessage = "Informe o nome do cliente.")]
    [StringLength(120)]
    public string Cliente { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o email do cliente.")]
    [EmailAddress(ErrorMessage = "Informe um email valido.")]
    [StringLength(180)]
    public string EmailCliente { get; set; } = string.Empty;

    [DecimalGreaterThan("0.00", ErrorMessage = "Informe um valor maior que zero.")]
    public decimal ValorTotal { get; set; }

    [StringLength(500)]
    public string Observacoes { get; set; } = string.Empty;
}
