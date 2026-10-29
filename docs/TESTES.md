# Testes unitários

```shell
dotnet test ./sources/ExpenseHub.slnx
```

Os testes não usam banco, rede, RabbitMQ ou serviço externo. O `ExpenseService` depende de `IExpenseRepository`
e de `TimeProvider`; nos testes eles são substituídos por:

- [`FakeExpenseRepository`](../sources/ExpenseHub.UnitTests/Support/FakeExpenseRepository.cs): imita o banco —
  leituras devolvem cópias, nada é persistido antes de `SaveChangesAsync` e é possível simular conflito de
  concorrência (`FailNextSave`) para verificar a atomicidade de estado, histórico e pagamento;
- [`FixedTimeProvider`](../sources/ExpenseHub.UnitTests/Support/FixedTimeProvider.cs): relógio controlado para
  datas futuras e horários registrados pelo servidor;
- [`FakeReportRepository`](../sources/ExpenseHub.UnitTests/Support/FakeReportRepository.cs) e
  [`FakeReportPublisher`](../sources/ExpenseHub.UnitTests/Support/FakeReportPublisher.cs) (CP3): pedidos de relatório
  com cópia, contagem de consultas aos reembolsos e publicação confirmada ou não.

| Arquivo | Regras cobertas |
|---|---|
| `Domain/ExpenseStateMachineTests` | transições válidas, todas as inválidas, estados finais |
| `Domain/ExpenseRulesTests` | limites de descrição, justificativa, valor e data |
| `Expenses/ExpenseRequestValidationTests` | validação declarativa do DTO |
| `Expenses/ExpenseDraftTests` (I04) | criação em Draft, proprietário do token, validações, edição contextual, falha de gravação |
| `Expenses/ExpenseSubmitAndQueryTests` (I05) | envio, repetição → 409, listagem filtrada, detalhe → 404 |
| `Authorization/ExpenseAccessPolicyTests` (I06) | matriz de visibilidade por role, roles acumuladas, filtro SQL × memória |
| `Authorization/OwnershipIsolationTests` (I06) | isolamento entre Employees, Auditor sem escrita, Admin sem acesso funcional |
| `Expenses/ExpenseDecisionTests` (I07) | aprovação, reprovação, justificativa, autoaprovação, repetição |
| `Expenses/ExpensePaymentTests` (I08) | pagamento, autopagamento, estados inválidos, atomicidade |
| `Expenses/ExpenseHistoryTests` (I08) | histórico completo do fluxo e sua visibilidade |
| `Users/RoleChangeRulesTests` (I03) | roles conhecidas, proteção da própria role Admin |
| `Expenses/ExpenseDescriptionTests` | descrição gravada sem espaços nas pontas na criação e na edição |
| `Expenses/ExpenseRejectionAuditTests` | ator e horário da reprovação derivados do servidor |
| `Reports/ReportCalculatorTests` (CP3) | exemplo da apostila (Mine 3 / R$ 175, Global 4 / R$ 375), limites do período, conjunto vazio |
| `Reports/ReportRequestValidationTests` (CP3) | datas ausentes ou invertidas e escopo inválido no DTO |
| `Reports/ReportServiceTests` (CP3) | Pending persistido e publicado, 503 com id, banco fora sem publicação, resultado salvo sem recálculo |
| `Reports/ReportIsolationTests` (CP3) | matriz role × escopo, escopo negado sem criar trabalho, contrato sem campo de dono, Auditor e Admin sem acesso a pedidos alheios, Mine processado pelo solicitante gravado |
| `Reports/ReportProcessorTests` (CP3) | conclusão com resultado salvo, Global com todos os reembolsos, Processing retomado, concluído não recalcula, período inválido → Failed, conflito de concorrência |
| `Reports/ReportMessageHandlerTests` (CP3) | ack só depois de salvar, duplicata sem efeito (inclusive após reinício), falha transitória e conflito → nack, limite de tentativas e falha definitiva → Failed + ack, mensagem inválida ou pedido inexistente |
| `Reports/ReportMessageContractTests` e `ReportContractTests` (CP3) | formato da mensagem e da resposta |
| `Reports/ReportExcelTests` (CP3) | planilha válida e numérica, resultado salvo, período vazio, 409 e 404 |
| `Http/RequestErrorStatusTests` (CP3) | corpo ilegível responde 400, não 500 |

Para conferir que a suíte detecta regressões, foram feitas mutações manuais (não versionadas):
remover a checagem de proprietário do filtro de visibilidade faz 12 testes falharem; remover a checagem
de proprietário na edição/envio faz 1 teste falhar.

No CP3, com o mesmo método (mutação manual, revertida em seguida):

| Mutação | Testes que falham |
|---|---|
| detalhe e Excel buscando o pedido só pelo `id`, sem o solicitante | 4 |
| worker sem a verificação de pedido já `Completed`/`Failed` (duplicata) | 3 |
| escopo Global liberado para qualquer role | 4 |
