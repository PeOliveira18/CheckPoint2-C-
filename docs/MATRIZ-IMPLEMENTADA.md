# Matriz de autorização implementada

Complementa [MATRIZ-AUTORIZACAO.md](MATRIZ-AUTORIZACAO.md) com o comportamento efetivo da API.
A autorização é feita em duas camadas:

1. **Rota** — policies de role em [`Auth/Policies.cs`](../sources/ExpenseHub.Api/Auth/Policies.cs):
   anônimo → `401`; autenticado sem a role exigida → `403`.
2. **Serviço** — [`ExpenseService`](../sources/ExpenseHub.Api/Application/ExpenseService.cs) e
   [`ExpenseAccessPolicy`](../sources/ExpenseHub.Api/Application/ExpenseAccessPolicy.cs) combinam role,
   ownership e estado. O serviço revalida a role, então a regra não depende apenas do atributo da rota.

## Escopo de leitura (listagem, detalhe e histórico)

| Role | Enxerga |
|---|---|
| Employee | somente os próprios reembolsos, em qualquer estado |
| Approver | reembolsos `Submitted` |
| Finance | reembolsos `Approved` e `Paid` |
| Auditor | todos |
| Admin | nenhum (sem acesso funcional implícito) |

Roles se acumulam: o escopo é a **união** dos escopos. O filtro é uma `Expression` aplicada no `IQueryable`
do EF Core (`WHERE` no banco), nunca depois de carregar os dados. Reembolso fora do escopo → `404`.

## Operações

| Operação | Policy da rota | Regra no serviço | Respostas de negação |
|---|---|---|---|
| Criar | Employee | proprietário = usuário do token | `403` sem Employee; `400` entrada inválida |
| Editar | Employee | fora do escopo → `404`; não proprietário → `403`; fora de `Draft` → `409` | `400`, `403`, `404`, `409` |
| Enviar | Employee | igual a editar; `Draft` → `Submitted` | `403`, `404`, `409` |
| Listar | Employee/Approver/Finance/Auditor | união dos escopos | `403` sem role funcional |
| Detalhe | Employee/Approver/Finance/Auditor | fora do escopo → `404` | `403`, `404` |
| Aprovar | Approver | inexistente ou `Draft` de outra pessoa → `404`; proprietário → `403`; fora de `Submitted` → `409` | `403`, `404`, `409` |
| Reprovar | Approver | igual a aprovar + justificativa de 10 a 500 caracteres | `400`, `403`, `404`, `409` |
| Pagar | Finance | inexistente ou `Draft` de outra pessoa → `404`; proprietário → `403`; fora de `Approved` → `409` | `403`, `404`, `409` |
| Histórico | Employee/Approver/Finance/Auditor | mesma visibilidade do reembolso | `403`, `404` |
| Listar usuários | Admin | — | `403` |
| Alterar roles | Admin | roles conhecidas; Admin não remove a própria role Admin | `400`, `403`, `404`, `409` |

Nas decisões (aprovar, reprovar e pagar) o reembolso é carregado sem o filtro de leitura para que uma
repetição (por exemplo, aprovar algo já aprovado) responda `409` em vez de `404`, como exige o contrato.
Rascunhos de outras pessoas continuam respondendo `404`, para não revelar sua existência.

## Casos negativos obrigatórios × evidência

| Caso | Teste unitário |
|---|---|
| Employee não acessa reembolso de outro Employee | `OwnershipIsolationTests.AnotherEmployeeCannotReadOrChangeExpense` |
| Employee não informa/altera proprietário | `ExpenseDraftTests.CreateAsEmployeeStoresDraftOwnedByCaller` (DTO não possui o campo) |
| Approver não decide sobre o próprio reembolso | `ExpenseDecisionTests` (I07) |
| Finance não paga o próprio reembolso | `ExpensePaymentTests` (I08) |
| Auditor não altera dados | `OwnershipIsolationTests.AuditorReadsEverythingButCannotWrite` |
| Admin sem acesso funcional | `OwnershipIsolationTests.AdminWithoutFunctionalRolesHasNoExpenseAccess` |
| Transição fora do estado → `409` | `ExpenseDraftTests.UpdateOutsideDraftReturnsConflict`, `ExpenseSubmitAndQueryTests.SubmitTwiceReturnsConflictWithoutDuplicatingHistory` |
| Fora do escopo de leitura → `404` | `ExpenseSubmitAndQueryTests.GetOutsideScopeReturnsNotFoundAndOwnerSeesOwn` |
| Matriz de visibilidade com roles acumuladas | `ExpenseAccessPolicyTests.VisibilityFollowsRoleMatrix` |
