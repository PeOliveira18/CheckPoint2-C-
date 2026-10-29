# Dossiê de experimentos do CP3

Evidências dos cinco experimentos obrigatórios (issue
[CP3-I06](https://github.com/Racass/checkpoint-csharpracass-expensehub/issues/16), seção 9).
Regras: o mesmo `reportId` do começo ao fim de cada experimento; comandos com os nomes reais dos serviços
(`web`, `worker`, `rabbitmq`); logs e prints **sem** tokens, senhas, cabeçalhos de autenticação ou connection strings.

Modelo de cada seção: objetivo → pré-condições → passos (comandos e requisições) → evidências (status HTTP,
`reportId`, trechos de log, prints) → esperado × obtido → explicação.

## Ambiente

> Preenchido na consolidação (versão das imagens, SHA do commit, data da execução).

## E01 — Fluxo normal

> Responsável: Pedro.

## E02 — Worker parado

> Responsável: Diego.

## E03 — Duplicata

> Responsável: Pedro.

## E04 — Isolamento

> Responsável: Debora. Executado em 28/10/2026 contra a API local (`dotnet run`, ambiente Development) com o
> Oracle da FIAP e RabbitMQ `rabbitmq:4-management` em contêiner; `main` em `70ef0dc` mais a correção da PR #21.

**Objetivo.** Mostrar que role e solicitante são verificados no servidor: Employee não pede Global, ninguém consulta ou
baixa relatório de outra pessoa (nem o Auditor) e a listagem só traz os próprios pedidos.

**Pré-condições.** Três contas criadas por `POST /register`, com papéis atribuídos pelo Admin
(`PUT /api/admin/users/{id}/roles`) e **novo login** depois da atribuição:

| Conta | Role | Papel no experimento |
|---|---|---|
| Ana | Employee | dona do relatório Mine |
| Bruno | Employee | outra Employee |
| Auditor | Auditor | pede Global e tenta acessar o relatório da Ana |

Período usado em todos os pedidos: `fromDate` `2026-09-01`, `toDate` `2026-09-30`. Requisições equivalentes estão no
final de [`ExpenseHub.Api.http`](../sources/ExpenseHub.Api/ExpenseHub.Api.http) (seção "CP3 — Relatórios").

**Passos e evidências.** `reportId` da Ana: `4f719f88-191d-4880-bbb4-059ba9d89378`.
`reportId` do Auditor: `c3ae01da-df06-4aef-8b5f-8661732d597c`.

| # | Quem | Requisição | Esperado | Obtido |
|---|---|---|---|---|
| 1 | Ana (Employee) | `POST /api/reports` com `scope: Global` | 403 | **403** — "Somente Auditor solicita relatório Global." |
| 2 | Ana | `POST /api/reports` com `scope: Mine` | 202 + `Location` | **202**, `Location: /api/reports/4f719f88-…`; worker concluiu (`Completed`, 3 reembolsos, R$ 175,00) |
| 3 | Ana | `GET /api/reports/4f719f88-…` | 200 | **200** |
| 4 | Ana | `GET /api/reports/4f719f88-…/excel` | 200 | **200**, `.xlsx` válido |
| 5 | Bruno (Employee) | `GET /api/reports/4f719f88-…` | 404 | **404** — "Relatório não encontrado." |
| 6 | Bruno | `GET /api/reports/4f719f88-…/excel` | 404 | **404** |
| 7 | Auditor | `POST /api/reports` com `scope: Global` | 202 | **202**, `c3ae01da-…` concluído |
| 8 | Auditor | `GET /api/reports/4f719f88-…` | 404 | **404** — "Relatório não encontrado." |
| 9 | Auditor | `GET /api/reports/4f719f88-…/excel` | 404 | **404** |
| 10 | Ana | `GET /api/reports/c3ae01da-…` (Global do Auditor) | 404 | **404** |
| 11 | Bruno | `GET /api/reports` | 200 sem o pedido da Ana | **200**, lista não contém `4f719f88-…` |
| 12 | Auditor | `GET /api/reports` | 200 só com os próprios | **200**, somente pedidos Global do Auditor; não contém `4f719f88-…` |

Casos complementares do contrato, na mesma execução: período invertido → **400**
(`toDate`: "A data final deve ser igual ou posterior à data inicial."); `scope: "Team"` → **400**; sem token → **401**;
id inexistente → **404**. Excel de relatório ainda `Pending` → **409** ("O Excel só fica disponível para relatório
concluído; o estado atual é Pending.").

**Esperado × obtido.** Todos os 12 passos e os casos complementares responderam o status esperado.

**Explicação.**

- O escopo é decidido pela role do token: `Mine` exige Employee e `Global` exige Auditor (`ReportAccessPolicy`); o 403
  sai antes de gravar ou publicar qualquer coisa.
- Consulta, listagem e Excel buscam sempre por `id` **e** `RequesterId` do token (`FindOwnAsync` / `ListOwnAsync`).
  Pedido de outra pessoa é indistinguível de pedido inexistente: 404, inclusive para o Auditor, que pode **gerar** um
  relatório Global mas não ganha acesso aos pedidos dos outros.
- O solicitante nunca vem do corpo nem da mensagem: a API grava o `RequesterId` do token e o worker lê escopo, período e
  solicitante do banco.
- Na primeira tentativa, um `scope` inválido respondeu 500 em Development; o defeito foi corrigido na PR #21 (corpo
  ilegível agora responde 400) e o experimento foi repetido do início com os ids acima.

## E05 — Persistência e versão

> Responsável: Diego.

## Diagnóstico: aceitação, publicação e janela sem outbox

> Responsável: Pedro.
