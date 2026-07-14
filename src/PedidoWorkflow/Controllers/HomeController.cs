using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PedidoWorkflow.Data;
using PedidoWorkflow.Models;
using PedidoWorkflow.ViewModels;

namespace PedidoWorkflow.Controllers;

public sealed class HomeController : Controller
{
    private readonly DashboardRepository _repository;

    public HomeController(DashboardRepository repository)
    {
        _repository = repository;
    }

    public async Task<IActionResult> Index()
    {
        var viewModel = new DashboardViewModel
        {
            Stats = await _repository.GetStatsAsync(),
            PedidosRecentes = await _repository.GetRecentOrdersAsync(6),
            Estados = await _repository.GetStatesAsync()
        };

        return View(viewModel);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
