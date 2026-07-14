using PedidoWorkflow.Models;
using PedidoWorkflow.ViewModels;

namespace PedidoWorkflow.Data;

public sealed class InMemoryStore
{
    private readonly Lock _syncRoot = new();
    private readonly List<EstadoPedido> _states = [];
    private readonly List<TransicaoWorkflow> _transitions = [];
    private readonly List<Pedido> _orders = [];
    private readonly List<HistoricoPedido> _history = [];
    private int _nextStateId = 1;
    private int _nextTransitionId = 1;
    private int _nextOrderId = 1;
    private int _nextHistoryId = 1;

    public void EnsureSeeded()
    {
        lock (_syncRoot)
        {
            if (_states.Count > 0)
            {
                return;
            }

            AddStateInternal("AguardandoPagamento", "Pedido criado, aguardando confirmacao do pagamento.", true, true);
            AddStateInternal("PagamentoConfirmado", "Pagamento aprovado e pronto para processamento.", true, false);
            AddStateInternal("SeparandoEstoque", "Pedido em separacao no deposito.", true, false);
            AddStateInternal("Transporte", "Pedido enviado para transporte.", true, false);
            AddStateInternal("Entregue", "Pedido recebido pelo cliente.", true, false);
            AddStateInternal("Cancelado", "Pedido cancelado no fluxo operacional.", true, false);
            AddStateInternal("Devolvido", "Pedido devolvido apos a entrega.", true, false);

            AddTransitionByNameInternal("Confirmar pagamento", "AguardandoPagamento", "PagamentoConfirmado");
            AddTransitionByNameInternal("Cancelar antes do pagamento", "AguardandoPagamento", "Cancelado");
            AddTransitionByNameInternal("Enviar para separacao", "PagamentoConfirmado", "SeparandoEstoque");
            AddTransitionByNameInternal("Cancelar apos confirmacao", "PagamentoConfirmado", "Cancelado");
            AddTransitionByNameInternal("Liberar para transporte", "SeparandoEstoque", "Transporte");
            AddTransitionByNameInternal("Cancelar na separacao", "SeparandoEstoque", "Cancelado");
            AddTransitionByNameInternal("Confirmar entrega", "Transporte", "Entregue");
            AddTransitionByNameInternal("Cancelar transporte", "Transporte", "Cancelado");
            AddTransitionByNameInternal("Registar devolucao", "Entregue", "Devolvido");
        }
    }

    public IReadOnlyList<EstadoPedido> GetStates()
    {
        lock (_syncRoot)
        {
            return _states
                .OrderByDescending(x => x.Inicial)
                .ThenBy(x => x.Nome)
                .ToList();
        }
    }

    public IReadOnlyList<TransicaoWorkflow> GetTransitions()
    {
        lock (_syncRoot)
        {
            return _transitions
                .OrderBy(x => x.EstadoOrigemNome)
                .ThenBy(x => x.EstadoDestinoNome)
                .ToList();
        }
    }

    public DashboardStats GetStats()
    {
        lock (_syncRoot)
        {
            return new DashboardStats
            {
                TotalPedidos = _orders.Count,
                PedidosAtivos = _orders.Count(x => !x.EstadoAtualFinal),
                PedidosEntregues = _orders.Count(x => x.EstadoAtualNome == "Entregue"),
                PedidosCancelados = _orders.Count(x => x.EstadoAtualNome == "Cancelado")
            };
        }
    }

    public IReadOnlyList<Pedido> GetRecentOrders(int limit)
    {
        lock (_syncRoot)
        {
            return _orders
                .OrderByDescending(x => x.CriadoEmUtc)
                .Take(limit)
                .ToList();
        }
    }

    public IReadOnlyList<Pedido> SearchOrders(string? busca)
    {
        lock (_syncRoot)
        {
            var query = _orders.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(busca))
            {
                var term = busca.Trim();
                query = query.Where(x =>
                    x.Numero.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.Cliente.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.EstadoAtualNome.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            return query
                .OrderByDescending(x => x.AtualizadoEmUtc)
                .ToList();
        }
    }

    public Pedido? GetOrderById(int id)
    {
        lock (_syncRoot)
        {
            return _orders.FirstOrDefault(x => x.Id == id);
        }
    }

    public IReadOnlyList<HistoricoPedido> GetHistory(int pedidoId)
    {
        lock (_syncRoot)
        {
            return _history
                .Where(x => x.PedidoId == pedidoId)
                .OrderByDescending(x => x.AlteradoEmUtc)
                .ToList();
        }
    }

    public IReadOnlyList<EstadoPedido> GetAllowedNextStates(int estadoAtualId)
    {
        lock (_syncRoot)
        {
            var allowedIds = _transitions
                .Where(x => x.EstadoOrigemId == estadoAtualId && x.Ativa)
                .Select(x => x.EstadoDestinoId)
                .ToHashSet();

            return _states
                .Where(x => allowedIds.Contains(x.Id) && x.Ativo)
                .OrderBy(x => x.Nome)
                .ToList();
        }
    }

    public int CreateOrder(PedidoCreateViewModel input)
    {
        lock (_syncRoot)
        {
            var initialState = _states.First(x => x.Inicial && x.Ativo);
            var now = DateTime.UtcNow;
            var order = new Pedido
            {
                Id = _nextOrderId++,
                Numero = $"PED-{now:yyyyMMddHHmmssfff}",
                Cliente = input.Cliente.Trim(),
                EmailCliente = input.EmailCliente.Trim(),
                ValorTotal = input.ValorTotal,
                Observacoes = input.Observacoes?.Trim() ?? string.Empty,
                EstadoAtualId = initialState.Id,
                EstadoAtualNome = initialState.Nome,
                EstadoAtualFinal = IsFinalState(initialState.Nome),
                CriadoEmUtc = now,
                AtualizadoEmUtc = now
            };

            _orders.Add(order);
            _history.Add(new HistoricoPedido
            {
                Id = _nextHistoryId++,
                PedidoId = order.Id,
                EstadoOrigem = "Criacao",
                EstadoDestino = initialState.Nome,
                Observacao = "Pedido criado no sistema.",
                AlteradoEmUtc = now
            });

            return order.Id;
        }
    }

    public bool UpdateOrderStatus(int pedidoId, int novoEstadoId, string observacao)
    {
        lock (_syncRoot)
        {
            var orderIndex = _orders.FindIndex(x => x.Id == pedidoId);
            if (orderIndex < 0)
            {
                return false;
            }

            var currentOrder = _orders[orderIndex];
            var transitionAllowed = _transitions.Any(x =>
                x.EstadoOrigemId == currentOrder.EstadoAtualId &&
                x.EstadoDestinoId == novoEstadoId &&
                x.Ativa);

            if (!transitionAllowed)
            {
                return false;
            }

            var targetState = _states.First(x => x.Id == novoEstadoId);
            var now = DateTime.UtcNow;
            _orders[orderIndex] = new Pedido
            {
                Id = currentOrder.Id,
                Numero = currentOrder.Numero,
                Cliente = currentOrder.Cliente,
                EmailCliente = currentOrder.EmailCliente,
                ValorTotal = currentOrder.ValorTotal,
                Observacoes = currentOrder.Observacoes,
                EstadoAtualId = targetState.Id,
                EstadoAtualNome = targetState.Nome,
                EstadoAtualFinal = IsFinalState(targetState.Nome),
                CriadoEmUtc = currentOrder.CriadoEmUtc,
                AtualizadoEmUtc = now
            };

            _history.Add(new HistoricoPedido
            {
                Id = _nextHistoryId++,
                PedidoId = currentOrder.Id,
                EstadoOrigem = currentOrder.EstadoAtualNome,
                EstadoDestino = targetState.Nome,
                Observacao = observacao.Trim(),
                AlteradoEmUtc = now
            });

            return true;
        }
    }

    public void AddState(WorkflowStateInputModel input)
    {
        lock (_syncRoot)
        {
            if (input.Inicial)
            {
                ResetInitialFlagInternal();
            }

            AddStateInternal(input.Nome.Trim(), input.Descricao.Trim(), input.Ativo, input.Inicial);
        }
    }

    public void UpdateState(WorkflowStateInputModel input)
    {
        lock (_syncRoot)
        {
            var existingIndex = _states.FindIndex(x => x.Id == input.Id);
            if (existingIndex < 0)
            {
                return;
            }

            if (input.Inicial)
            {
                ResetInitialFlagInternal();
            }

            var current = _states[existingIndex];
            var updated = new EstadoPedido
            {
                Id = current.Id,
                Nome = input.Nome.Trim(),
                Descricao = input.Descricao.Trim(),
                Ativo = input.Ativo,
                Inicial = input.Inicial,
                CriadoEmUtc = current.CriadoEmUtc
            };
            _states[existingIndex] = updated;

            for (var i = 0; i < _transitions.Count; i++)
            {
                var transition = _transitions[i];
                if (transition.EstadoOrigemId == updated.Id)
                {
                    _transitions[i] = new TransicaoWorkflow
                    {
                        Id = transition.Id,
                        Label = transition.Label,
                        EstadoOrigemId = transition.EstadoOrigemId,
                        EstadoOrigemNome = updated.Nome,
                        EstadoDestinoId = transition.EstadoDestinoId,
                        EstadoDestinoNome = transition.EstadoDestinoNome,
                        Ativa = transition.Ativa
                    };
                }

                if (transition.EstadoDestinoId == updated.Id)
                {
                    _transitions[i] = new TransicaoWorkflow
                    {
                        Id = transition.Id,
                        Label = transition.Label,
                        EstadoOrigemId = transition.EstadoOrigemId,
                        EstadoOrigemNome = transition.EstadoOrigemNome,
                        EstadoDestinoId = transition.EstadoDestinoId,
                        EstadoDestinoNome = updated.Nome,
                        Ativa = transition.Ativa
                    };
                }
            }
        }
    }

    public void AddTransition(WorkflowTransitionInputModel input)
    {
        lock (_syncRoot)
        {
            if (_transitions.Any(x => x.EstadoOrigemId == input.EstadoOrigemId && x.EstadoDestinoId == input.EstadoDestinoId))
            {
                return;
            }

            var origem = _states.First(x => x.Id == input.EstadoOrigemId);
            var destino = _states.First(x => x.Id == input.EstadoDestinoId);
            _transitions.Add(new TransicaoWorkflow
            {
                Id = _nextTransitionId++,
                Label = input.Label.Trim(),
                EstadoOrigemId = origem.Id,
                EstadoOrigemNome = origem.Nome,
                EstadoDestinoId = destino.Id,
                EstadoDestinoNome = destino.Nome,
                Ativa = true
            });
        }
    }

    public void ToggleTransition(int transitionId)
    {
        lock (_syncRoot)
        {
            var index = _transitions.FindIndex(x => x.Id == transitionId);
            if (index < 0)
            {
                return;
            }

            var current = _transitions[index];
            _transitions[index] = new TransicaoWorkflow
            {
                Id = current.Id,
                Label = current.Label,
                EstadoOrigemId = current.EstadoOrigemId,
                EstadoOrigemNome = current.EstadoOrigemNome,
                EstadoDestinoId = current.EstadoDestinoId,
                EstadoDestinoNome = current.EstadoDestinoNome,
                Ativa = !current.Ativa
            };
        }
    }

    private void AddStateInternal(string nome, string descricao, bool ativo, bool inicial)
    {
        _states.Add(new EstadoPedido
        {
            Id = _nextStateId++,
            Nome = nome,
            Descricao = descricao,
            Ativo = ativo,
            Inicial = inicial,
            CriadoEmUtc = DateTime.UtcNow
        });
    }

    private void AddTransitionByNameInternal(string label, string origemNome, string destinoNome)
    {
        var origem = _states.First(x => x.Nome == origemNome);
        var destino = _states.First(x => x.Nome == destinoNome);
        _transitions.Add(new TransicaoWorkflow
        {
            Id = _nextTransitionId++,
            Label = label,
            EstadoOrigemId = origem.Id,
            EstadoOrigemNome = origem.Nome,
            EstadoDestinoId = destino.Id,
            EstadoDestinoNome = destino.Nome,
            Ativa = true
        });
    }

    private void ResetInitialFlagInternal()
    {
        for (var i = 0; i < _states.Count; i++)
        {
            var state = _states[i];
            _states[i] = new EstadoPedido
            {
                Id = state.Id,
                Nome = state.Nome,
                Descricao = state.Descricao,
                Ativo = state.Ativo,
                Inicial = false,
                CriadoEmUtc = state.CriadoEmUtc
            };
        }
    }

    private static bool IsFinalState(string stateName)
    {
        return stateName is "Entregue" or "Cancelado" or "Devolvido";
    }
}
