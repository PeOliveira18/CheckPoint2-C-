# Mensageria dos relatórios (CP3-I03)

## Fluxo

1. `POST /api/reports` valida datas, escopo e perfil, grava a solicitação como `Pending` e publica `{"reportId": "..."}`.
2. A API só responde **202** depois que o broker confirma a publicação (*publisher confirm*). Sem confirmação
   responde **503** com o `id` já persistido.
3. O worker (`sources/ExpenseHub.Worker`) consome a fila, carrega período, escopo e solicitante **do banco** (nunca da
   mensagem), calcula, grava `Completed` com o resultado e **só então** confirma o consumo (*ack*).

## Configuração

| Item | Valor |
|---|---|
| Fila | `expensehub.reports` na exchange padrão (routing key = nome da fila) |
| Fila durável | `durable: true`, `exclusive: false`, `autoDelete: false`, declarada pela API e pelo worker |
| Mensagem | persistente (`DeliveryMode = 2`), `application/json`, `MessageId` e `CorrelationId` = `reportId` |
| Publicação | canal com confirmação e rastreamento (`publisherConfirmationsEnabled` + `publisherConfirmationTrackingEnabled`), `mandatory: true`, tempo limite de 5 s |
| Consumo | `autoAck: false`, `prefetchCount: 1`, ack após salvar; nack com `requeue: true` após 5 s nas falhas transitórias |
| Cliente | `RabbitMQ.Client` 7.2.2, recuperação automática de conexão |

Chaves (seção `RabbitMq`, ou variáveis `RabbitMq__*` no Compose): `HostName`, `Port`, `UserName`, `Password`,
`QueueName`, `PublishTimeoutSeconds` (API) e `RetryDelaySeconds` (worker). Usuário e senha ficam fora do Git
(user-secrets ou `.env`).

## Decisão do worker por mensagem

| Situação | Estado gravado | Mensagem |
|---|---|---|
| `Pending` ou `Processing` | `Processing` (com `startedAt`) → `Completed` com resultado e `generatedAt` | ack |
| Já `Completed` ou `Failed` (duplicata, reentrega, republicação) | nenhum | ack, log "duplicata" |
| Período inválido gravado | `Failed` (`invalid-period`) | ack |
| `Attempts` chegou a 5 | `Failed` (`max-attempts`) | ack |
| Solicitação inexistente ou mensagem sem `reportId` válido | nenhum | ack (descartada) |
| Banco indisponível ou outra exceção | o que já tiver sido salvo | nack + requeue após 5 s |
| Conflito de concorrência | nenhum | nack + requeue |

- **Idempotência persistente:** a proteção está no estado salvo no banco (`Completed`/`Failed`) e no token de
  concorrência; por isso sobrevive ao reinício do worker. O cálculo pode ser repetido; o efeito persistido não.
- **Retomada de `Processing`:** se o worker cai depois de marcar `Processing` e antes do ack, o RabbitMQ reentrega a
  mensagem e o worker processa de novo (mantendo o `startedAt` original e somando uma tentativa).
- **Sem reencaminhamento infinito:** falhas definitivas viram `Failed` e são confirmadas; falhas que se repetem
  esgotam as 5 tentativas e também viram `Failed`. Se o banco estiver totalmente fora, a tentativa nem chega a ser
  contada e a mensagem espera na fila — é indisponibilidade de infraestrutura, não falha de negócio.

## Limite sem Transactional Outbox

Gravar no banco e publicar são operações separadas:

- se a API cair **depois de gravar e antes de publicar**, a solicitação fica `Pending` sem mensagem na fila;
- se a confirmação se perder, a publicação fica incerta e a API responde 503 com o `id`.

Nesses casos o usuário consulta o `id` (continua `Pending`) e pode fazer um novo POST, que cria outro `id`. Reiniciar o
worker **não** recupera uma mensagem que nunca foi publicada. Republicar manualmente `{"reportId": "<id>"}` na fila
(painel do RabbitMQ) processa a solicitação. Outbox, republicação automática e entrega exatamente uma vez ficaram
fora do escopo.

Se o banco falhar **antes** de gravar, a API devolve erro (500) sem `id` e sem publicar.

## Logs correlacionados

Todos os eventos levam o `reportId`: publicação confirmada ou não (API), início com indicação de reentrega,
conclusão, duplicata, falha definitiva, conflito e falha transitória (worker). Tokens, senhas e connection strings
não são registrados.
