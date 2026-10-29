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

> Responsável: Diego. Executado em 29/10/2026 no Docker Compose com as imagens publicadas
> `ghcr.io/peoliveira18/expensehub-api` e `ghcr.io/peoliveira18/expensehub-worker`.
> Tag das duas (SHA do commit da `main`): `456f2fc582def10d646781ab689d70c6a6144c25`.

**Objetivo.** Mostrar que a API aceita o pedido mesmo sem worker, que a mensagem fica guardada no broker e que o
mesmo `reportId` é concluído quando o worker volta.

**Pré-condições.** `docker compose up -d` com `web`, `worker` e `rabbitmq` rodando; conta Employee (Ana) com os
reembolsos de setembro/2026 usados no E01 (3 reembolsos, R$ 175,00). `reportId` do experimento:
`ad184b6d-f196-4a33-be9d-26cfcebfca31`.

**Passos e evidências** (horários em UTC):

| Hora | Ação | Resultado |
|---|---|---|
| 00:46:14 | `docker compose stop worker` | `worker` parado; `web` e `rabbitmq` continuam `Up` |
| 00:46:15 | `POST /api/reports` (Mine, 2026-09-01 a 2026-09-30) | **202**, `Location: /api/reports/ad184b6d-…` |
| 00:46:18 | `GET /api/reports/ad184b6d-…` | **200**, `status: Pending`, sem `startedAt` |
| 00:46:18 | `rabbitmqctl list_queues` | `expensehub.reports` — **Ready 1**, Unacked 0, consumidores 0 |
| 00:46:34 | `GET /api/reports/ad184b6d-…` (16 s depois) | continua `Pending` |
| 00:46:34 | painel (API de gerenciamento, `/api/queues/%2F/expensehub.reports`) | `durable: true`, `messages_ready: 1`, `messages_unacknowledged: 0`, `consumers: 0` |
| 00:46:34 | `docker compose up -d worker` | `worker` iniciado |
| 00:46:36 | `GET /api/reports/ad184b6d-…` | **`Completed`**, `startedAt` 00:46:36.31, `completedAt` 00:46:36.42, 3 reembolsos, R$ 175,00 |
| 00:46:37 | `rabbitmqctl list_queues` | Ready 0, Unacked 0, consumidores 1 |

Trecho de `docker compose logs --tail 80 worker` e `web` para o mesmo id (sem dados sensíveis):

```text
web-1     | Relatório ad184b6d-f196-4a33-be9d-26cfcebfca31 publicado com confirmação do broker.
worker-1  | Relatório ad184b6d-f196-4a33-be9d-26cfcebfca31: processamento iniciado (reentrega: False).
worker-1  | Relatório ad184b6d-f196-4a33-be9d-26cfcebfca31: concluído e salvo; mensagem confirmada.
```

**Esperado × obtido.** Esperado: 202 com o worker parado, pedido `Pending`, mensagem em Ready e conclusão do mesmo id
ao religar o worker. Obtido: exatamente isso.

**Explicação.**

- A API só responde 202 depois da confirmação do broker (*publisher confirm*); o worker não participa do aceite.
- A fila é durável e a mensagem é persistente, então ela espera no broker enquanto não há consumidor.
- **Ready** e **Unacked** são métricas do RabbitMQ: Ready = mensagem disponível para entrega; Unacked = entregue a um
  consumidor e ainda sem *ack*. Elas não são os estados da aplicação: `Pending` e `Processing` ficam gravados no Oracle.
  Aqui, Ready 1 coincidiu com `Pending` porque não havia consumidor; ao religar, o worker marcou `Processing`, gravou
  `Completed` e só então confirmou a mensagem (Ready 0, Unacked 0).

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

> Responsável: Diego. Executado em 29/10/2026 no mesmo ambiente do E02, sem `down -v` e sem remover volumes.

**Objetivo.** Recriar só `web` e `worker` a partir das imagens publicadas e provar que a versão em execução é a
esperada e que resultados já gerados continuam iguais.

**Pré-condições.** Dois relatórios concluídos no E01:

| Relatório | `reportId` | Resultado antes |
|---|---|---|
| Mine (Ana) | `dd5e2b20-22ea-4806-991b-c83e66849f2f` | `Completed`, 3 reembolsos, R$ 175,00, `completedAt` 2026-10-29T00:46:02.578Z |
| Global (Auditor) | `b0b09d10-9c89-414f-9480-d9f4eaf0c28b` | `Completed`, 4 reembolsos, R$ 375,00, `completedAt` 2026-10-29T00:46:04.155Z |

**Passos e evidências.**

1. Contêineres antes: `web` `c5bae6b38883`, `worker` `6f282ef4ed35`.
2. `docker compose pull web worker` — imagens `…:456f2fc582def10d646781ab689d70c6a6144c25` baixadas do GHCR.
3. `docker compose up -d --force-recreate web worker` — `rabbitmq` não foi tocado.
4. Contêineres depois: `web` `82e0da889b95`, `worker` `16aef59bedb7` (novos contêineres).
5. Versão em execução:

   ```text
   $ docker compose images
   CONTAINER               REPOSITORY                               TAG
   expensehub-rabbitmq-1   rabbitmq                                 4-management
   expensehub-web-1        ghcr.io/peoliveira18/expensehub-api      456f2fc582def10d646781ab689d70c6a6144c25
   expensehub-worker-1     ghcr.io/peoliveira18/expensehub-worker   456f2fc582def10d646781ab689d70c6a6144c25

   $ docker inspect --format '{{.Config.Image}}' expensehub-web-1   (e o mesmo para o worker)
   label org.opencontainers.image.revision = 456f2fc582def10d646781ab689d70c6a6144c25 nas duas imagens
   ```

   O SHA da tag e o rótulo `revision` são o commit `456f2fc` da `main` (merge da PR #23), publicado pelo workflow
   `publish`. Volume do broker preservado: `expensehub_rabbitmq-data`.
6. Novo login (tokens fora do dossiê) e nova consulta dos dois ids:

| Relatório | Depois da recriação | Comparação |
|---|---|---|
| `dd5e2b20-…` | **200**, `Completed`, 3 / R$ 175,00, mesmo `startedAt`, `completedAt` e `generatedAt` | corpo JSON **idêntico** ao de antes |
| `b0b09d10-…` | **200**, `Completed`, 4 / R$ 375,00 | corpo JSON **idêntico** ao de antes |

   `GET /api/reports/dd5e2b20-…/excel` depois da recriação → **200**, planilha com Approved 2 / 150, Paid 1 / 25,
   Total 3 / 175 e `Gerado em (UTC)` 2026-10-29T00:46:02Z (o mesmo horário da geração original).

**Esperado × obtido.** Esperado: novos contêineres na versão publicada e resultados preservados. Obtido: contêineres
novos com a tag e a `revision` `456f2fc…`, e os dois relatórios idênticos aos de antes.

**Explicação.** `web` e `worker` não guardam estado: pedidos e resultados ficam no Oracle (externo ao Compose) e a
fila no volume nomeado do RabbitMQ. Por isso recriar os contêineres não perde nada. Como o resultado é salvo em
`ResultJson` no momento da conclusão, a consulta e o Excel devolvem o mesmo conteúdo sem recalcular. A tag igual ao
SHA do commit, mais o rótulo `org.opencontainers.image.revision`, ligam o que está rodando a um commit exato.

## Diagnóstico: aceitação, publicação e janela sem outbox

> Responsável: Pedro.
