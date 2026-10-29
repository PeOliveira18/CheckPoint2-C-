# ExpenseHub — Checkpoints 2 e 3 (C#)

API REST corporativa de reembolsos com autenticação, autorização por roles e
ownership, fluxo de aprovação/reprovação, pagamento simulado, histórico e
testes unitários (CP2), evoluída no CP3 com **relatórios em segundo plano**:
solicitação pela API, processamento por um worker via **RabbitMQ**, resultado salvo,
download em **Excel**, duas imagens publicadas e execução com **Docker Compose**.

Especificação oficial: [Racass/checkpoint-csharpracass-expensehub](https://github.com/Racass/checkpoint-csharpracass-expensehub)
(CP2: issues I01–I10; CP3: issues #11–#18). Documentos do enunciado em [docs/](docs/).

| Entrega | Commit |
|---|---|
| CP2 | `f2c53ceceaa0d7a28bde67bd19a7eae8329f7b9e` |
| CP3 | último commit da `main` (o SHA entregue no Teams é também a tag das imagens) |

## Integrantes

| Nome | RM | GitHub |
|---|---|---|
| Pedro Oliveira | 99943 | [@PeOliveira18](https://github.com/PeOliveira18) |
| Debora Ivanowski | 555694 | [@Debora-Iv](https://github.com/Debora-Iv) |
| Diego Cabral | 557817 | [@diegoc85](https://github.com/diegoc85) |

## Stack

- .NET 10 / ASP.NET Core e Worker Service
- Entity Framework Core 10
- Oracle Database (provider `Oracle.EntityFrameworkCore` 10.23.x, modo de compatibilidade 19c)
- RabbitMQ 4 (`RabbitMQ.Client` 7.2.2, publisher confirms e ack manual)
- ClosedXML 0.105.1 (planilha `.xlsx`)
- Docker, Docker Compose e GitHub Container Registry
- MSTest (testes unitários)

## Estrutura

```text
sources/
├── ExpenseHub.slnx
├── ExpenseHub.Api/      # web: API REST (imagem expensehub-api)
│   ├── Application/     # serviços, políticas de acesso, exportação Excel
│   ├── Auth/            # Identity, JWT, seed do Admin
│   ├── Contracts/       # DTOs de resposta
│   ├── Data/            # repositório EF Core usado pelos serviços
│   ├── Dtos/            # DTOs de entrada com validação declarativa
│   ├── Endpoints/       # minimal APIs
│   ├── Messaging/       # publicação no RabbitMQ com confirmação
│   ├── Dockerfile
│   └── Program.cs
├── ExpenseHub.Core/     # compartilhado entre API e worker
│   ├── Data/            # DbContext, mapeamentos e migrations
│   ├── Domain/          # entidades e enums do domínio
│   ├── Messaging/       # contrato da mensagem e declaração da fila
│   └── Reports/         # cálculo e processamento idempotente do relatório
├── ExpenseHub.Worker/   # worker: consumidor da fila (imagem expensehub-worker)
│   ├── Messaging/
│   └── Dockerfile
└── ExpenseHub.UnitTests/
```

Na raiz: [`docker-compose.yml`](docker-compose.yml), [`.env.example`](.env.example) e o workflow
[`publish`](.github/workflows/publish.yml), que roda os testes e publica as imagens.

## Banco de dados (Oracle)

| Item | Valor |
|---|---|
| Provider | Oracle Database (testado no Oracle da FIAP) |
| Pacote | `Oracle.EntityFrameworkCore` 10.23.26301 |
| Connection string | `ConnectionStrings:ExpenseHub` |
| Tabelas | prefixo `EH_` para não colidir com outras tabelas do schema |

A connection string **não é versionada**: o `appsettings.json` traz apenas um
valor vazio. Configure-a com User Secrets (desenvolvimento local):

```shell
cd sources/ExpenseHub.Api
dotnet user-secrets set "ConnectionStrings:ExpenseHub" "User Id=<RM>;Password=<SENHA>;Data Source=oracle.fiap.com.br:1521/ORCL"
```

Alternativamente, use a variável de ambiente `ConnectionStrings__ExpenseHub`.

### Criar ou atualizar o banco

As migrations ficam em `sources/ExpenseHub.Core/Data/Migrations`. A ferramenta
`dotnet-ef` está fixada no manifesto local `dotnet-tools.json`:

```shell
dotnet tool restore
dotnet ef database update --project sources/ExpenseHub.Core --startup-project sources/ExpenseHub.Api
```

Para criar uma nova migration (não exige conexão com o banco):

```shell
dotnet ef migrations add <NomeDaMigration> --project sources/ExpenseHub.Core --startup-project sources/ExpenseHub.Api --output-dir Data/Migrations
```

O build e os testes unitários não dependem de um banco em execução.

## Segredos de configuração

Nenhum segredo é versionado. Além da connection string, configure (uma vez por máquina):

```shell
cd sources/ExpenseHub.Api
dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 48)"
dotnet user-secrets set "SeedAdmin:Password" "<SENHA_FORTE_DO_ADMIN>"
```

| Chave | Onde fica | Observação |
|---|---|---|
| `ConnectionStrings:ExpenseHub` | User Secrets / variável de ambiente | conexão Oracle |
| `Jwt:SigningKey` | User Secrets / variável de ambiente | mínimo de 32 caracteres; a aplicação não inicia sem ela |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpirationMinutes` | `appsettings.json` | valores não sensíveis |
| `SeedAdmin:Email` | `appsettings.json` | padrão `admin@expensehub.local` |
| `SeedAdmin:Password` | User Secrets / variável de ambiente | senha inicial do Admin (mín. 8 caracteres, com maiúscula, minúscula, dígito e símbolo) |

Para consultar os valores configurados: `dotnet user-secrets list`.

## Autenticação e seed

- Usuários e roles são persistidos pelo ASP.NET Core Identity nas tabelas `EH_USERS`, `EH_ROLES` etc.
- Na inicialização, o seed (idempotente) cria as roles `Admin`, `Employee`, `Approver`, `Finance` e `Auditor`
  e, **somente se ainda não existir nenhum Admin**, a conta configurada em `SeedAdmin`. Nenhum outro usuário é criado.
- `POST /login` recebe `{ "email", "password" }` e devolve `{ "accessToken", "tokenType": "Bearer", "expiresIn" }`.
  Envie o token no cabeçalho `Authorization: Bearer <token>`.
- O token carrega o *security stamp* do usuário. Quando o stamp muda (por exemplo, ao alterar roles), tokens
  antigos passam a receber `401` e o usuário precisa **fazer login novamente**.
- Respostas de erro seguem `ProblemDetails`: `401` sem credencial válida, `403` autenticado sem permissão.

## Cadastro e administração de roles

| Método e rota | Acesso | Respostas |
|---|---|---|
| `POST /register` | público | `201` criado **sem roles**; `400` entrada inválida; `409` e-mail já cadastrado |
| `POST /login` | público | `200` token; `400` entrada inválida; `401` credenciais inválidas |
| `GET /api/me` | autenticado | `200`; `401` |
| `GET /api/admin/users` | Admin | `200`; `401`; `403` |
| `PUT /api/admin/users/{id}/roles` | Admin | `200`; `400` role desconhecida; `401`; `403`; `404` usuário inexistente; `409` Admin removendo a própria role Admin |

- O cadastro nunca aceita roles: qualquer campo `roles` enviado é ignorado e o usuário nasce sem nenhuma role.
- `PUT /api/admin/users/{id}/roles` recebe o **conjunto completo** de roles (`{ "roles": ["Employee", "Approver"] }`);
  roles ausentes da lista são removidas. Apenas `Admin`, `Employee`, `Approver`, `Finance` e `Auditor` são aceitas.
- **Após qualquer alteração de roles o usuário deve fazer login novamente**: o security stamp é renovado
  e os tokens emitidos antes da alteração passam a receber `401`.
- Exemplos prontos em [`ExpenseHub.Api.http`](sources/ExpenseHub.Api/ExpenseHub.Api.http).

## Reembolsos

Categorias iniciais (seed via migration): `1` Alimentação, `2` Transporte, `3` Hospedagem,
`4` Material de escritório, `5` Outros.

| Método e rota | Role | Regra contextual (serviço) | Respostas |
|---|---|---|---|
| `POST /api/expenses` | Employee | proprietário = usuário do token; estado `Draft` | `201`, `400`, `401`, `403` |
| `PUT /api/expenses/{id}` | Employee | somente o proprietário e somente em `Draft` | `200`, `400`, `401`, `403`, `404`, `409` |
| `POST /api/expenses/{id}/submit` | Employee | somente o proprietário; `Draft` → `Submitted` | `200`, `401`, `403`, `404`, `409` |
| `GET /api/expenses` | Employee, Approver, Finance, Auditor | união dos escopos das roles, filtrada no banco | `200`, `401`, `403` |
| `GET /api/expenses/{id}` | Employee, Approver, Finance, Auditor | fora do escopo de leitura → `404` | `200`, `401`, `403`, `404` |
| `POST /api/expenses/{id}/approve` | Approver | não proprietário; `Submitted` → `Approved` | `200`, `401`, `403`, `404`, `409` |
| `POST /api/expenses/{id}/reject` | Approver | não proprietário; justificativa de 10 a 500 caracteres; `Submitted` → `Rejected` | `200`, `400`, `401`, `403`, `404`, `409` |
| `POST /api/expenses/{id}/pay` | Finance | não proprietário; `Approved` → `Paid`; cria `PaymentRecord` | `200`, `401`, `403`, `404`, `409` |
| `GET /api/expenses/{id}/history` | Employee, Approver, Finance, Auditor | mesma visibilidade do reembolso | `200`, `401`, `403`, `404` |

`Rejected` e `Paid` são finais. Não existem reabertura, cancelamento, exclusão ou reenvio; repetir uma
transição responde `409` sem gerar histórico. A matriz completa está em
[docs/MATRIZ-IMPLEMENTADA.md](docs/MATRIZ-IMPLEMENTADA.md).

### Histórico e atomicidade

Cada criação, edição em `Draft` (com a descrição das alterações), envio, aprovação, reprovação (com a
justificativa) e pagamento gera uma entrada em `EH_EXPENSE_HISTORY` com ação, ator, instante UTC e estados
anterior/posterior. A alteração do reembolso, o histórico e o `PaymentRecord` são gravados no **mesmo**
`SaveChangesAsync` (uma transação). O reembolso possui token de concorrência: se duas operações
disputarem a mesma transição, a segunda recebe `409` e nada dela é persistido.

Validações: descrição com 10 a 500 caracteres, valor entre R$ 0,01 e R$ 2.147.483.647,00 (`decimal`),
data válida e não futura, categoria existente. `id`, proprietário, estado, atores e horários nunca são
lidos do corpo da requisição.

## Relatórios em segundo plano (CP3)

| Método e rota | Acesso | Respostas |
|---|---|---|
| `POST /api/reports` | Employee (`scope: Mine`) ou Auditor (`scope: Global`) | `202` + `Location`; `400` datas ausentes/invertidas ou escopo inválido; `401`; `403` role incompatível com o escopo; `503` publicação não confirmada (com o `id` já gravado) |
| `GET /api/reports` | autenticado | `200`, somente os pedidos do próprio usuário, mais recentes primeiro |
| `GET /api/reports/{id}` | dono do pedido | `200` (`Pending`, `Processing`, `Completed` com resultado ou `Failed` com erro); `404` para pedido de outra pessoa, inclusive Auditor |
| `GET /api/reports/{id}/excel` | dono do pedido | `200` arquivo `report-{id}.xlsx`; `404` pedido de outra pessoa; `409` pedido não concluído |

Corpo do POST: `{ "fromDate": "2026-09-01", "toDate": "2026-09-30", "scope": "Mine" }`.

- **Mine** resume os reembolsos do próprio solicitante; **Global**, de todos. O período usa `ExpenseDate`, com as duas
  datas inclusivas. O resultado traz total de reembolsos, valor total e os cinco estados (sempre presentes, com zero).
- O POST grava `Pending` e só responde `202` depois que o RabbitMQ confirma a mensagem `{"reportId"}`. O worker lê
  período, escopo e solicitante **do banco**, marca `Processing`, calcula, grava `Completed` com o resultado e só então
  confirma a mensagem (*ack*).
- O resultado fica salvo (`ResultJson`): consultas e Excel não recalculam, mesmo que um reembolso mude depois.
- Mensagem repetida para pedido já `Completed`/`Failed` é confirmada sem novo efeito (proteção persistente no banco);
  falhas transitórias voltam para a fila; falhas definitivas viram `Failed` com código e mensagem.
- Detalhes da fila, das decisões do worker e da janela sem outbox em [docs/MENSAGERIA.md](docs/MENSAGERIA.md).

## Como executar

### Docker Compose (imagens publicadas)

Serviços `web` (API em <http://localhost:8080>), `worker` e `rabbitmq` (painel em <http://localhost:15672>). O banco é o
Oracle da FIAP, externo ao Compose.

```shell
cp .env.example .env    # preencha IMAGE_TAG, connection string, RabbitMQ, JWT e senha do Admin
docker compose pull
docker compose up -d
docker compose ps
```

As imagens são publicadas pelo workflow `publish` a cada push na `main`, para `linux/amd64` e `linux/arm64`, com a tag
igual ao SHA completo do commit:

| Serviço | Imagem |
|---|---|
| `web` | `ghcr.io/peoliveira18/expensehub-api` |
| `worker` | `ghcr.io/peoliveira18/expensehub-worker` |

Passo a passo completo (migrations, variáveis, conferência da versão) em [docs/EXECUCAO-DOCKER.md](docs/EXECUCAO-DOCKER.md).

### Local (desenvolvimento)

```shell
dotnet restore ./sources/ExpenseHub.slnx
dotnet build ./sources/ExpenseHub.slnx
dotnet test ./sources/ExpenseHub.slnx
dotnet run --project ./sources/ExpenseHub.Api/ExpenseHub.Api.csproj
dotnet run --project ./sources/ExpenseHub.Worker/ExpenseHub.Worker.csproj
```

API e worker compartilham os User Secrets (`ConnectionStrings:ExpenseHub` e, se o RabbitMQ exigir usuário,
`RabbitMq:UserName`/`RabbitMq:Password`); o RabbitMQ local pode ser o do Compose (`docker compose up -d rabbitmq`).
O endpoint `GET /health` confirma que a API iniciou. Para exercitar o fluxo completo, siga
[docs/FLUXO-DEMONSTRACAO.md](docs/FLUXO-DEMONSTRACAO.md) com as requisições de
[`ExpenseHub.Api.http`](sources/ExpenseHub.Api/ExpenseHub.Api.http) (seção "CP3 — Relatórios" para os relatórios e o Excel).

## Experimentos operacionais (CP3)

O dossiê [docs/CP3-EXPERIMENTOS.md](docs/CP3-EXPERIMENTOS.md) registra, com `reportId`, comandos, status e logs reais
executados no Compose com as imagens publicadas: **E01** fluxo normal com Excel, **E02** worker parado, **E03**
duplicata após reinício, **E04** isolamento, **E05** persistência e versão, mais o diagnóstico de publicação sem
confirmação e da janela sem outbox.

## Qualidade de código

O workflow `code-quality` roda em cada push e pull request. As PRs do CP2 e do CP3 foram mergeadas com score
**100/100** (build sem erros e sem warnings, sem segredos, sem arquivos gerados); 207 testes unitários. Para rodar localmente (requer PowerShell 7 e Gitleaks):

```shell
pwsh ./scripts/Invoke-CodeQuality.ps1
```

## Documentação

- [Enunciado](docs/ENUNCIADO.md)
- [Requisitos e contratos](docs/REQUISITOS.md)
- [Rubrica](docs/RUBRICA.md)
- [Matriz de autorização](docs/MATRIZ-AUTORIZACAO.md)
- [Matriz de autorização implementada](docs/MATRIZ-IMPLEMENTADA.md)
- [Processo no GitHub](docs/PROCESSO-GITHUB.md)
- [Fluxo de demonstração](docs/FLUXO-DEMONSTRACAO.md)
- [Testes unitários](docs/TESTES.md)
- [Mensageria dos relatórios](docs/MENSAGERIA.md)
- [Execução com Docker Compose](docs/EXECUCAO-DOCKER.md)
- [Dossiê de experimentos do CP3](docs/CP3-EXPERIMENTOS.md)
- [Guia de contribuição dos integrantes](docs/GUIA-INTEGRANTES.md) (CP2) e [guia do CP3](docs/GUIA-CP3.md)
- [Uso de Inteligência Artificial](docs/USO-DE-IA.md)
- [Regras do pipeline de qualidade](docs/code-quality-rules.md)
