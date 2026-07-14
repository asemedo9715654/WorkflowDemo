using Microsoft.AspNetCore.Mvc;
using PedidoWorkflow.Services;
using PedidoWorkflow.ViewModels;

namespace PedidoWorkflow.Controllers;

public sealed class WorkflowController : Controller
{
    private readonly WorkflowService _service;

    public WorkflowController(WorkflowService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await _service.GetViewModelAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddState(WorkflowStateInputModel input)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Revise os campos do novo estado.";
            return RedirectToAction(nameof(Index));
        }

        await _service.AddStateAsync(input);
        TempData["SuccessMessage"] = "Estado adicionado ao workflow.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateState(WorkflowStateInputModel input)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Nao foi possivel atualizar o estado.";
            return RedirectToAction(nameof(Index));
        }

        await _service.UpdateStateAsync(input);
        TempData["SuccessMessage"] = "Estado atualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTransition(WorkflowTransitionInputModel input)
    {
        if (!ModelState.IsValid || input.EstadoOrigemId == input.EstadoDestinoId)
        {
            TempData["ErrorMessage"] = "Selecione uma origem e um destino diferentes.";
            return RedirectToAction(nameof(Index));
        }

        await _service.AddTransitionAsync(input);
        TempData["SuccessMessage"] = "Transicao adicionada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleTransition(int id)
    {
        await _service.ToggleTransitionAsync(id);
        TempData["SuccessMessage"] = "Transicao atualizada.";
        return RedirectToAction(nameof(Index));
    }
}
