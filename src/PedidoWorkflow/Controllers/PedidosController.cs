using Microsoft.AspNetCore.Mvc;
using PedidoWorkflow.Services;
using PedidoWorkflow.ViewModels;

namespace PedidoWorkflow.Controllers;

public sealed class PedidosController : Controller
{
    private readonly PedidoService _service;

    public PedidosController(PedidoService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? busca)
    {
        var pedidos = await _service.SearchAsync(busca);
        return View(new PedidoListViewModel
        {
            Busca = busca,
            Pedidos = pedidos
        });
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new PedidoCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PedidoCreateViewModel input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var pedidoId = await _service.CreateAsync(input);
        TempData["SuccessMessage"] = "Pedido criado com sucesso.";
        return RedirectToAction(nameof(Details), new { id = pedidoId });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var details = await _service.GetDetailsAsync(id);
        if (details is null)
        {
            return NotFound();
        }

        return View(details);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(PedidoStatusUpdateViewModel input)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Selecione um estado valido para a transicao.";
            return RedirectToAction(nameof(Details), new { id = input.PedidoId });
        }

        var updated = await _service.UpdateStatusAsync(input);
        TempData[updated ? "SuccessMessage" : "ErrorMessage"] = updated
            ? "Status do pedido atualizado."
            : "A transicao escolhida nao e permitida.";

        return RedirectToAction(nameof(Details), new { id = input.PedidoId });
    }
}
