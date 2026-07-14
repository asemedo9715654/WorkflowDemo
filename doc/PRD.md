# PRD - Sistema de Gestao de Pedidos com Workflow

## 1. Visao Geral

### 1.1 Objetivo do Produto
Construir um sistema simples de gestao de pedidos em arquitetura MVC com C#, com foco em acompanhar o ciclo de vida dos pedidos por meio de um workflow configuravel, persistido em base de dados SQL Express e com interface moderna, clara e facil de usar.

### 1.2 Problema a Resolver
Empresas que operam pedidos precisam acompanhar cada etapa do processamento, desde a criacao ate a entrega, cancelamento ou devolucao. Sem um fluxo bem definido, ha risco de perda de rastreabilidade, inconsistencias operacionais e dificuldade para adaptar o processo ao negocio.

### 1.3 Meta Principal
Permitir que usuarios cadastrem, consultem e atualizem pedidos com base em estados de workflow configuraveis, mantendo historico das transicoes e administrando os estados por uma tela de configuracao.

## 2. Escopo do Produto

### 2.1 Incluido no MVP
- Aplicacao web em MVC com C#.
- Persistencia de dados em SQL Express.
- Cadastro e consulta de pedidos.
- Workflow de pedidos com estados iniciais predefinidos.
- Alteracao do estado do pedido conforme regras de transicao.
- Tela de configuracao para gestao de workflow.
- Cadastro, edicao, ativacao e desativacao de estados.
- Historico de mudancas de estado por pedido.
- Interface simples, limpa e com boa usabilidade.

### 2.2 Fora do Escopo Inicial
- Integracao com gateways de pagamento.
- Integracao com transportadoras.
- Notificacoes por email, SMS ou WhatsApp.
- Controle de estoque real.
- Multiempresa.
- Aplicativo mobile nativo.

## 3. Usuarios

### 3.1 Administrador
Responsavel por configurar o workflow, gerir estados e garantir que o processo reflita a operacao do negocio.

### 3.2 Operador
Responsavel por criar pedidos, consultar detalhes e atualizar o estado dos pedidos no dia a dia.

### 3.3 Gestor
Responsavel por acompanhar pedidos e validar se o processo esta funcionando conforme esperado.

## 4. Requisitos Funcionais

### 4.1 Gestao de Pedidos
- O sistema deve permitir criar um pedido.
- O sistema deve permitir listar pedidos.
- O sistema deve permitir visualizar detalhes de um pedido.
- O sistema deve permitir alterar o estado de um pedido.
- O sistema deve registrar data e hora das alteracoes de estado.
- O sistema deve manter historico de transicoes de cada pedido.

### 4.2 Workflow de Pedidos
Estados iniciais esperados:
- `AguardandoPagamento`: pedido criado, aguardando confirmacao do pagamento.
- `PagamentoConfirmado`: pagamento aprovado, ainda nao processado.
- `SeparandoEstoque`: pedido em separacao no deposito.
- `Transporte`: pedido enviado para transporte ou entregador.
- `Entregue`: pedido recebido pelo cliente.
- `Cancelado`: pedido cancelado antes ou depois do pagamento.
- `Devolvido`: pedido devolvido pelo cliente.

Regras esperadas:
- Todo pedido deve possuir um estado atual.
- O fluxo deve permitir definir quais transicoes sao validas entre estados.
- O sistema deve impedir transicoes invalidas.
- O sistema deve permitir configuracao futura de novos estados sem necessidade de alterar o codigo principal do fluxo.

### 4.3 Configuracao do Workflow
- O sistema deve ter uma tela de configuracao de workflow.
- O administrador deve poder adicionar novos estados.
- O administrador deve poder editar estados existentes.
- O administrador deve poder ativar ou desativar estados.
- O administrador deve poder configurar transicoes permitidas entre estados.
- O sistema deve preservar integridade dos pedidos ja existentes ao alterar configuracoes do workflow.

## 5. Requisitos Nao Funcionais

### 5.1 Tecnologia
- Backend em C# com padrao MVC.
- Base de dados SQL Express.
- Estrutura preparada para manutencao e evolucao.

### 5.2 Usabilidade
- A interface deve ser simples e intuitiva.
- A navegacao deve priorizar clareza e rapidez nas acoes principais.
- O estado atual do pedido deve ser exibido com destaque visual.
- O historico de transicoes deve ser facil de consultar.

### 5.3 Qualidade
- O sistema deve validar entradas obrigatorias.
- O sistema deve garantir consistencia das transicoes de estado.
- O sistema deve registrar erros operacionais relevantes.

## 6. Modelo Conceitual

### 6.1 Entidades Principais
- `Pedido`
- `EstadoPedido`
- `TransicaoWorkflow`
- `HistoricoPedido`

### 6.2 Relacoes Esperadas
- Um pedido possui um estado atual.
- Um pedido possui varios registros de historico.
- Um estado pode ter varias transicoes de origem e destino.

## 7. Fluxos Principais

### 7.1 Fluxo Basico do Pedido
1. Operador cria o pedido.
2. O pedido inicia em `AguardandoPagamento`.
3. O pagamento e confirmado e o pedido muda para `PagamentoConfirmado`.
4. O pedido segue para `SeparandoEstoque`.
5. O pedido segue para `Transporte`.
6. O pedido segue para `Entregue`.

### 7.2 Fluxos Alternativos
- O pedido pode ser movido para `Cancelado` conforme regra definida.
- O pedido pode ser movido para `Devolvido` apos entrega, conforme regra definida.

## 8. Criterios de Aceitacao do MVP
- Deve ser possivel criar e consultar pedidos.
- Deve ser possivel visualizar o estado atual de cada pedido.
- Deve ser possivel alterar o estado de um pedido apenas para estados permitidos.
- Deve existir tela administrativa para gerir estados e transicoes.
- O historico de alteracoes de estado deve ficar gravado.
- Os dados devem ser persistidos em SQL Express.
- A interface deve ser funcional, simples e visualmente organizada.

## 9. Riscos e Observacoes
- As regras detalhadas de transicao entre todos os estados ainda precisam ser confirmadas.
- Nao ha definicao de campos detalhados do pedido no requisito inicial.
- Nao ha definicao de perfis de acesso, autenticacao ou autorizacao.

## 10. Proximos Passos Recomendados
1. Validar campos obrigatorios do pedido.
2. Definir matriz completa de transicoes entre estados.
3. Definir perfis de usuario e permissoes.
4. Aprovar wireframes da interface.
5. Detalhar modelo de dados para implementacao.
