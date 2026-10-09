# Testes unitários

```shell
dotnet test ./sources/ExpenseHub.slnx
```

Os testes não usam banco, rede ou serviço externo. O `ExpenseService` depende de `IExpenseRepository`
e de `TimeProvider`; nos testes eles são substituídos por:

- [`FakeExpenseRepository`](../sources/ExpenseHub.UnitTests/Support/FakeExpenseRepository.cs): imita o banco —
  leituras devolvem cópias, nada é persistido antes de `SaveChangesAsync` e é possível simular conflito de
  concorrência (`FailNextSave`) para verificar a atomicidade de estado, histórico e pagamento;
- [`FixedTimeProvider`](../sources/ExpenseHub.UnitTests/Support/FixedTimeProvider.cs): relógio controlado para
  datas futuras e horários registrados pelo servidor.

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

Para conferir que a suíte detecta regressões, foram feitas mutações manuais (não versionadas):
remover a checagem de proprietário do filtro de visibilidade faz 12 testes falharem; remover a checagem
de proprietário na edição/envio faz 1 teste falhar.
