# Dossiê de experimentos do CP3

Evidências dos cinco experimentos obrigatórios (issue
[CP3-I06](https://github.com/Racass/checkpoint-csharpracass-expensehub/issues/16), seção 9).
Regras: o mesmo `reportId` do começo ao fim de cada experimento; comandos com os nomes reais dos serviços
(`web`, `worker`, `rabbitmq`); logs e prints **sem** tokens, senhas, cabeçalhos de autenticação ou connection strings.

Modelo de cada seção: objetivo → pré-condições → passos (comandos e requisições) → evidências (status HTTP,
`reportId`, trechos de log, prints) → esperado × obtido → explicação.

## Ambiente

| Item | Valor |
|---|---|
| Data da execução | 28 e 29/10/2026 (horários em UTC nas tabelas) |
| Máquina | macOS (Apple Silicon), Rancher Desktop com `dockerd` 29.5.3 (`linux/arm64`) e Docker Compose 5.3.1 |
| Arquivo do ambiente | [`docker-compose.yml`](../docker-compose.yml) com `web`, `worker` e `rabbitmq`; passo a passo em [EXECUCAO-DOCKER.md](EXECUCAO-DOCKER.md) |
| Imagens (E01, E02, E03, E05 e diagnóstico) | `ghcr.io/peoliveira18/expensehub-api` e `ghcr.io/peoliveira18/expensehub-worker` |
| Tag das imagens (SHA do commit) | `456f2fc582def10d646781ab689d70c6a6144c25` (merge da PR #23), variante `linux/arm64` do manifesto multiplataforma |
| Broker | `rabbitmq:4-management`, fila durável `expensehub.reports`, volume `expensehub_rabbitmq-data` |
| Banco | Oracle da FIAP 19c, externo ao Compose; migrations `InitialExpenseSchema`, `AddIdentity` e `AddReportRequests` aplicadas |
| E04 | executado antes da publicação das imagens, com `dotnet run` (API e worker) e o mesmo RabbitMQ em contêiner; detalhes na seção |

**Massa de dados** (criada pela própria API antes dos experimentos: `POST /register`, roles atribuídas pelo Admin, novo
login, criação, envio, aprovação e pagamento dos reembolsos). É o exemplo da apostila:

| Conta | Role | Reembolsos (data, valor, estado final) |
|---|---|---|
| Ana | Employee | 05/09 R$ 100,00 Approved · 10/09 R$ 50,00 Approved · 20/09 R$ 25,00 Paid · 02/10 R$ 500,00 Paid (fora do período) |
| Bruno | Employee | 15/09 R$ 200,00 Approved |
| Carla | Approver + Finance | aprovou e pagou os reembolsos acima (não é dona de nenhum) |
| Auditor | Auditor | sem reembolsos |

Resultado esperado para setembro/2026 (`2026-09-01` a `2026-09-30`): **Mine da Ana = 3 reembolsos, R$ 175,00**
(Approved 2 / 150,00; Paid 1 / 25,00) e **Global = 4 reembolsos, R$ 375,00** (Approved 3 / 350,00; Paid 1 / 25,00).

## E01 — Fluxo normal

> Responsável: Pedro. Executado em 29/10/2026 no Compose com as imagens publicadas.

**Objetivo.** Autenticar, solicitar, guardar o id, acompanhar até `Completed`, baixar o Excel e conferir resultado e
planilha contra os dados conhecidos da massa acima.

**Pré-condições.** `docker compose up -d`; `docker compose ps` com os três serviços `Up` (`rabbitmq` healthy).

**Passos e evidências.** `reportId` Mine: `dd5e2b20-22ea-4806-991b-c83e66849f2f`.
`reportId` Global: `b0b09d10-9c89-414f-9480-d9f4eaf0c28b`.

| Hora | Quem | Requisição | Resultado |
|---|---|---|---|
| 00:46:01 | Ana | `POST /login` | 200 (token fora do dossiê) |
| 00:46:01 | Ana | `POST /api/reports` Mine, 2026-09-01 a 2026-09-30 | **202**, `Location: /api/reports/dd5e2b20-…` |
| 00:46:02 | Ana | `GET /api/reports/dd5e2b20-…` | 200, `Pending` |
| 00:46:03 | Ana | `GET /api/reports/dd5e2b20-…` | 200, **`Completed`**, `startedAt` 00:46:02.481, `completedAt` 00:46:02.578, 3 / R$ 175,00 |
| 00:46:03 | Ana | `GET /api/reports/dd5e2b20-…/excel` | **200**, `attachment; filename=report-dd5e2b20-….xlsx` |
| 00:46:04 | Auditor | `POST /api/reports` Global | **202**, `Location: /api/reports/b0b09d10-…` |
| 00:46:04 | Auditor | `GET /api/reports/b0b09d10-…` | 200, **`Completed`**, 4 / R$ 375,00 |
| 00:46:04 | Auditor | `GET /api/reports/b0b09d10-…/excel` | **200**, `report-b0b09d10-….xlsx` |
| 00:46:05 | — | `rabbitmqctl list_queues` | `expensehub.reports` Ready 0, Unacked 0, 1 consumidor |

Planilhas baixadas (células lidas do `.xlsx`; B e C numéricas):

| Linha | Mine (`dd5e2b20-…`) | Global (`b0b09d10-…`) |
|---|---|---|
| B2 / B3 / B4 | Mine / 2026-09-01 / 2026-09-30 | Global / 2026-09-01 / 2026-09-30 |
| B5 (gerado em, UTC) | 2026-10-29T00:46:02Z | 2026-10-29T00:46:04Z |
| Draft / Submitted / Rejected | 0 / 0,00 cada | 0 / 0,00 cada |
| Approved | 2 / 150,00 | 3 / 350,00 |
| Paid | 1 / 25,00 | 1 / 25,00 |
| **Total** | **3 / 175,00** | **4 / 375,00** |

Logs correlacionados (`docker compose logs web worker`, filtrados pelo id):

```text
web-1     | Relatório dd5e2b20-22ea-4806-991b-c83e66849f2f publicado com confirmação do broker.
worker-1  | Relatório dd5e2b20-22ea-4806-991b-c83e66849f2f: processamento iniciado (reentrega: False).
worker-1  | Relatório dd5e2b20-22ea-4806-991b-c83e66849f2f: concluído e salvo; mensagem confirmada.
web-1     | Relatório b0b09d10-9c89-414f-9480-d9f4eaf0c28b publicado com confirmação do broker.
worker-1  | Relatório b0b09d10-9c89-414f-9480-d9f4eaf0c28b: processamento iniciado (reentrega: False).
worker-1  | Relatório b0b09d10-9c89-414f-9480-d9f4eaf0c28b: concluído e salvo; mensagem confirmada.
```

**Esperado × obtido.** Mine 3 / R$ 175,00 e Global 4 / R$ 375,00, os cinco estados sempre presentes, o reembolso de
02/10 fora do período e o Excel com os mesmos números do resultado salvo. Obtido: idêntico ao esperado.

**Explicação.** O POST só grava `Pending` e publica `{"reportId"}`; o 202 sai depois da confirmação do broker. O
worker lê período, escopo e solicitante do banco, marca `Processing`, calcula, grava `Completed` com o resultado em
`ResultJson` e só então confirma a mensagem. O Excel é montado a partir desse resultado salvo, nunca de uma nova
consulta aos reembolsos.

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

> Responsável: Pedro. Executado em 29/10/2026 no Compose, logo depois do E02.

**Objetivo.** Depois de concluir, reiniciar o worker e republicar a mensagem com o mesmo `reportId`; mostrar que não
há novo efeito.

**Pré-condições.** Relatório `dd5e2b20-22ea-4806-991b-c83e66849f2f` (do E01) em `Completed`, 3 / R$ 175,00,
`completedAt` 2026-10-29T00:46:02.578Z.

**Passos e evidências.**

| Hora | Ação | Resultado |
|---|---|---|
| 00:46:44 | `GET /api/reports/dd5e2b20-…` | 200, `Completed`, corpo guardado para comparação |
| 00:46:44 | `docker compose restart worker` | worker reiniciado (novo processo, sem memória do que já processou) |
| 00:46:52 | republicação no painel do RabbitMQ (abaixo) | `{"routed":true}` |
| 00:46:58 | `rabbitmqctl list_queues` | Ready 0, Unacked 0, 1 consumidor: a mensagem foi consumida e confirmada |
| 00:46:58 | `GET /api/reports/dd5e2b20-…` | 200, `Completed`, 3 / R$ 175,00 |
| 00:46:58 | comparação dos dois corpos JSON | **idênticos**: mesmo `status`, `startedAt`, `completedAt`, `generatedAt` e totais |

**Como republicar** (ambiente de estudo): painel <http://localhost:15672> → **Queues and Streams** →
`expensehub.reports` → **Publish message** → *Delivery mode* `2 - Persistent`, *Properties* `content_type =
application/json`, *Payload* `{"reportId":"dd5e2b20-22ea-4806-991b-c83e66849f2f"}` → **Publish message**. Neste
experimento usamos a mesma operação pela API HTTP do painel (`POST /api/exchanges/%2F/amq.default/publish` com
`routing_key` `expensehub.reports`). Nenhum endpoint de negócio foi criado e o banco não foi alterado à mão.

Log do worker depois do reinício (mesmo id):

```text
worker-1  | Relatório dd5e2b20-22ea-4806-991b-c83e66849f2f: processamento iniciado (reentrega: False).
worker-1  | Relatório dd5e2b20-22ea-4806-991b-c83e66849f2f: duplicata de solicitação já finalizada; confirmada sem novo efeito.
```

**Esperado × obtido.** Esperado: mensagem confirmada sem recalcular nem regravar. Obtido: log de duplicata, fila vazia
e resultado idêntico, inclusive `completedAt` e `generatedAt` da primeira execução.

**Explicação.** A proteção contra duplicata está no **estado salvo no banco**, não na memória do worker: ao receber
uma mensagem, ele carrega o pedido e, se já está `Completed` ou `Failed`, confirma sem efeito. Por isso funciona depois
do reinício. Em uma corrida entre dois consumidores, o token de concorrência (`ConcurrencyStamp`) impede que duas
conclusões sejam gravadas. Os testes `ReportMessageHandlerTests` e `ReportProcessorTests` cobrem os mesmos casos sem
broker nem banco. "Reentrega: False" aparece porque a mensagem republicada é uma mensagem nova para o broker; quando o
RabbitMQ reenvia uma mensagem não confirmada, o log mostra `reentrega: True` (caso visto no diagnóstico abaixo).

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

> Responsável: Pedro. Execução complementar (não é um sexto experimento obrigatório). Detalhes do fluxo em
> [MENSAGERIA.md](MENSAGERIA.md).

### Publicação sem confirmação → 503 com id

Compose da seção Ambiente; `reportId` `1994cb61-0ec1-4a3d-a233-d845f1c58aab`.

| Hora | Ação | Resultado |
|---|---|---|
| 00:48:11 | `docker compose stop rabbitmq` | broker parado (`web` e `worker` continuam rodando) |
| 00:48:16 | `POST /api/reports` Mine (Ana) | **503** "Publicação não confirmada." com `id` `1994cb61-…`, depois de ~4 s (tempo limite de publicação) |
| 00:48:16 | `GET /api/reports/1994cb61-…` | 200, `Pending`: a solicitação foi gravada antes da tentativa de publicar |
| 00:48:16 | `docker compose start rabbitmq` | broker de volta; worker reconecta sozinho |
| 00:48:38 | fila e `GET` 20 s depois | Ready 0; o pedido **continua `Pending`**: a mensagem nunca existiu |
| 00:48:38 | republicação manual de `{"reportId":"1994cb61-…"}` pelo painel | `{"routed":true}` |
| 00:48:42 | `GET /api/reports/1994cb61-…` | **`Completed`**, 3 / R$ 175,00 |
| 00:48:42 | novo `POST /api/reports` com o broker no ar | 202 (outro id) |

```text
web-1     | Relatório 1994cb61-0ec1-4a3d-a233-d845f1c58aab: publicação sem confirmação do broker.
web-1     |   RabbitMQ.Client.Exceptions.BrokerUnreachableException: None of the specified endpoints were reachable
web-1     | Relatório 1994cb61-0ec1-4a3d-a233-d845f1c58aab registrado, mas a publicação não foi confirmada pelo broker.
worker-1  | Relatório 1994cb61-0ec1-4a3d-a233-d845f1c58aab: processamento iniciado (reentrega: False).
worker-1  | Relatório 1994cb61-0ec1-4a3d-a233-d845f1c58aab: concluído e salvo; mensagem confirmada.
```

### Falha transitória no worker → nack e reentrega

Observado durante a primeira tentativa do E04 (API e worker com `dotnet run`): a conexão do worker com o Oracle ficou
ociosa por cerca de 15 minutos e foi derrubada pela rede. Ao processar o relatório `5d8f9147-1c77-49c3-902d-8558b5c9a5cb`
o worker recebeu `ORA-12570` (`SocketException: Operation timed out`), registrou "falha transitória; mensagem devolvida
à fila", e o RabbitMQ reentregou a mesma mensagem (`processamento iniciado (reentrega: True)`), que foi concluída na
tentativa seguinte. Nenhum dado foi perdido e o relatório não virou `Failed`.

### Conclusões

- **Aceitação:** `202` significa "gravado e publicado com confirmação"; não significa "processado". O processamento é
  assíncrono e acompanhado pelo `GET`.
- **Publicação:** sem confirmação, a API não finge sucesso: responde `503` e devolve o `id` já gravado, para o usuário
  consultar o andamento. Banco fora **antes** de gravar → erro sem `id` e sem publicação. Os dois casos são cobertos
  pelos testes `UnconfirmedPublicationKeepsPersistedRequest` e `DatabaseFailureBeforeSavingDoesNotPublish`.
- **Janela sem outbox:** gravar no Oracle e publicar no RabbitMQ são duas operações separadas, sem transação comum.
  Se a publicação falha (como acima) ou a API cai entre gravar e publicar, o pedido fica `Pending` sem mensagem.
  Reiniciar o worker ou o broker **não** recupera esse pedido, porque não há o que reentregar. A recuperação é manual
  (republicar `{"reportId"}` pelo painel) ou um novo POST, que cria outro id. A correção definitiva seria um
  *Transactional Outbox* (gravar a mensagem numa tabela na mesma transação do pedido e um publicador que a envia depois),
  deixado fora do escopo do CP3.
- **Worker parado ≠ broker indisponível:** com o worker parado (E02) a mensagem existe e espera em Ready; com o broker
  indisponível a mensagem nem chega a existir. Só o segundo cai na janela sem outbox.
