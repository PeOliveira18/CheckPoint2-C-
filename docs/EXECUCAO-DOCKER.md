# Execução com Docker Compose (CP3-I05)

O ExpenseHub roda em três serviços: `web` (API), `worker` (consumidor dos relatórios) e `rabbitmq` (broker com
painel). O banco é o **Oracle da FIAP**, externo ao Compose; `web` e `worker` usam a mesma connection string.

## Imagens

Publicadas pelo workflow [`publish`](../.github/workflows/publish.yml) a cada push na `main`, depois dos testes,
para `linux/amd64` e `linux/arm64`, com a tag igual ao SHA completo do commit:

| Serviço | Imagem |
|---|---|
| `web` | `ghcr.io/peoliveira18/expensehub-api:<sha>` |
| `worker` | `ghcr.io/peoliveira18/expensehub-worker:<sha>` |

Os Dockerfiles ficam em `sources/ExpenseHub.Api/Dockerfile` e `sources/ExpenseHub.Worker/Dockerfile` e usam a raiz do
repositório como contexto (o build precisa de `Directory.Build.props` e `.editorconfig`). A compilação roda na
arquitetura do runner e gera a saída para a arquitetura de destino (`-a $TARGETARCH`); a imagem final contém só o
runtime e roda sem privilégios (`USER $APP_UID`).

## Pré-requisitos

- Docker com Compose v2 (Docker Desktop ou Rancher Desktop com o motor `dockerd`).
- Usuário e senha do Oracle da FIAP.
- Para aplicar as migrations: .NET 10 SDK.

## Passo a passo

1. **Configuração.** Copie o exemplo e troque os placeholders (o `.env` não é versionado):

   ```shell
   cp .env.example .env
   ```

   | Variável | Uso |
   |---|---|
   | `IMAGE_TAG` | SHA completo do commit publicado (aba Actions → `publish` → resumo, ou GitHub → Packages) |
   | `EXPENSEHUB_CONNECTION_STRING` | conexão Oracle, entre aspas: `"User Id=<RM>;Password=<SENHA>;Data Source=oracle.fiap.com.br:1521/ORCL"` |
   | `RABBITMQ_USER`, `RABBITMQ_PASSWORD` | usuário criado no primeiro start do volume do RabbitMQ; também usado por `web` e `worker` |
   | `JWT_SIGNING_KEY` | chave do JWT, mínimo de 32 caracteres (`openssl rand -base64 48`) |
   | `SEED_ADMIN_PASSWORD` | senha inicial do Admin (`admin@expensehub.local`) |

   Evite `$` nos valores do `.env`: o Compose interpreta `$` como variável.

2. **Migrations (uma vez por banco).** Com a connection string nos user-secrets
   (`dotnet user-secrets set "ConnectionStrings:ExpenseHub" "..." --project sources/ExpenseHub.Api`):

   ```shell
   dotnet tool restore
   dotnet ef database update --project sources/ExpenseHub.Core --startup-project sources/ExpenseHub.Api
   ```

3. **Baixar e subir.**

   ```shell
   docker compose pull
   docker compose up -d
   docker compose ps
   ```

   O `rabbitmq` sobe primeiro; `web` e `worker` só iniciam depois do healthcheck (`rabbitmq-diagnostics -q ping`).

4. **Conferir.**

   - API: <http://localhost:8080/health> → `{"status":"ok"}`; requisições em
     [`ExpenseHub.Api.http`](../sources/ExpenseHub.Api/ExpenseHub.Api.http) trocando o host para `http://localhost:8080`.
   - Painel do RabbitMQ: <http://localhost:15672> com `RABBITMQ_USER`/`RABBITMQ_PASSWORD`; fila `expensehub.reports`.
   - Logs: `docker compose logs -f web worker`.

## Versão em execução

```shell
docker compose images
docker inspect --format '{{.Config.Image}}' expensehub-web-1 expensehub-worker-1
docker image inspect --format '{{index .Config.Labels "org.opencontainers.image.revision"}}' ghcr.io/peoliveira18/expensehub-api:<sha>
```

## Operações usadas nos experimentos

| Ação | Comando |
|---|---|
| Parar só o worker | `docker compose stop worker` |
| Voltar o worker | `docker compose up -d worker` |
| Reiniciar o worker | `docker compose restart worker` |
| Trocar a versão sem perder dados | atualizar `IMAGE_TAG`, `docker compose pull` e `docker compose up -d --force-recreate web worker` |
| Parar tudo mantendo a fila | `docker compose down` |

O volume `rabbitmq-data` guarda filas duráveis e mensagens persistentes entre reinícios. **Não** use
`docker compose down -v` se quiser manter as mensagens. Os relatórios ficam no Oracle e não dependem dos contêineres.
