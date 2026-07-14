using PedidoWorkflow.Data;
using PedidoWorkflow.ViewModels;

namespace PedidoWorkflow.Services;

public sealed class WorkflowService
{
    private readonly WorkflowRepository _repository;

    public WorkflowService(WorkflowRepository repository)
    {
        _repository = repository;
    }

    public async Task<WorkflowViewModel> GetViewModelAsync()
    {
        var estados = await _repository.GetStatesAsync();
        var transicoes = await _repository.GetTransitionsAsync();

        return new WorkflowViewModel
        {
            Estados = estados,
            Transicoes = transicoes
        };
    }

    public Task AddStateAsync(WorkflowStateInputModel input)
    {
        return _repository.AddStateAsync(input);
    }

    public Task UpdateStateAsync(WorkflowStateInputModel input)
    {
        return _repository.UpdateStateAsync(input);
    }

    public Task AddTransitionAsync(WorkflowTransitionInputModel input)
    {
        return _repository.AddTransitionAsync(input);
    }

    public Task ToggleTransitionAsync(int transitionId)
    {
        return _repository.ToggleTransitionAsync(transitionId);
    }
}
