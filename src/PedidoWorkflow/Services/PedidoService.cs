using PedidoWorkflow.Data;
using PedidoWorkflow.ViewModels;

namespace PedidoWorkflow.Services;

public sealed class PedidoService
{
    private readonly PedidoRepository _repository;

    public PedidoService(PedidoRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<Models.Pedido>> SearchAsync(string? busca)
    {
        return _repository.SearchAsync(busca);
    }

    public async Task<PedidoDetailsViewModel?> GetDetailsAsync(int id)
    {
        var pedido = await _repository.GetByIdAsync(id);
        if (pedido is null)
        {
            return null;
        }

        var historico = await _repository.GetHistoryAsync(id);
        var proximosEstados = await _repository.GetAllowedNextStatesAsync(pedido.EstadoAtualId);

        return new PedidoDetailsViewModel
        {
            Pedido = pedido,
            Historico = historico,
            ProximosEstados = proximosEstados
        };
    }

    public Task<int> CreateAsync(PedidoCreateViewModel input)
    {
        return _repository.CreateAsync(input);
    }

    public Task<bool> UpdateStatusAsync(PedidoStatusUpdateViewModel input)
    {
        var observacao = string.IsNullOrWhiteSpace(input.Observacao)
            ? "Status atualizado manualmente."
            : input.Observacao;

        return _repository.UpdateStatusAsync(input.PedidoId, input.NovoEstadoId, observacao);
    }
}
