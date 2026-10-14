# Fluxo de demonstração

Roteiro para validar a API ponta a ponta com o arquivo
[`ExpenseHub.Api.http`](../sources/ExpenseHub.Api/ExpenseHub.Api.http).
Pré-requisitos: segredos configurados e banco atualizado (veja o [README](../README.md)), API rodando com
`dotnet run --project ./sources/ExpenseHub.Api/ExpenseHub.Api.csproj`.

## Preparação dos usuários

1. `POST /login` com o Admin do seed → `200` com `accessToken` (preencha `@adminToken`).
2. `POST /register` para **Ana**, **Bruno** e **Carla** → `201`, todos com `roles: []`.
3. Como Admin, `PUT /api/admin/users/{id}/roles`:
   - Ana → `["Employee"]`;
   - Bruno → `["Approver"]`;
   - Carla → `["Finance"]`.
   Cada chamada → `200`.
4. `POST /login` de Ana, Bruno e Carla → `200`. É obrigatório logar **depois** da troca de roles:
   tokens emitidos antes dela recebem `401`.

## Caminho feliz

5. Ana: `POST /api/expenses` → `201`, estado `Draft`, `ownerId` = Ana.
6. Ana: `PUT /api/expenses/{id}` alterando o valor → `200`.
7. Ana: `POST /api/expenses/{id}/submit` → `200`, estado `Submitted`.
8. Bruno: `GET /api/expenses` → `200`, o reembolso aparece porque está `Submitted`.
9. Bruno: `POST /api/expenses/{id}/approve` → `200`, estado `Approved`.
10. Carla: `POST /api/expenses/{id}/pay` → `200`, estado `Paid` com `payment` preenchido.
11. Ana: `GET /api/expenses/{id}/history` → `200` com 5 entradas:
    `Created`, `Updated`, `Submitted`, `Approved`, `Paid`.

## Casos negativos

| Passo | Requisição | Esperado |
|---|---|---|
| Sem token | `GET /api/expenses` | `401` |
| Admin sem role funcional | `GET /api/expenses` com `adminToken` | `403` |
| Reenvio | Ana repete o `submit` | `409` |
| Autoaprovação | Ana (só Employee) chama `approve` | `403` |
| Repetição de decisão | Bruno repete o `approve` | `409` |
| Reprovação sem justificativa válida | Bruno chama `reject` com `"justification": "não"` | `400` |
| Pagamento repetido | Carla repete o `pay` | `409` |
| Escopo de leitura | Bruno consulta o detalhe depois de `Approved` | `404` |
