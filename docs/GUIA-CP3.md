# Guia do CP3 para os integrantes

Cada integrante precisa de **commits próprios no CP3**, feitos com **a própria conta** do GitHub; sem isso a
nota individual é zero. O Pedro não commita por ninguém: ele revisa, roda a validação e faz o merge.
Pode usar IA (Claude Code, Copilot etc.), mas revise e entenda o que for enviar.

Especificação: issues CP3 do repositório original
([#11](https://github.com/Racass/checkpoint-csharpracass-expensehub/issues/11) a
[#18](https://github.com/Racass/checkpoint-csharpracass-expensehub/issues/18)).

## Quem faz o quê

| Item | Branch | Quem | Pode começar | Issue citada na PR |
|---|---|---|---|---|
| Excel do relatório | `cp3-excel` | Debora | **já** (o resultado salvo está na `main`) | `#11` |
| Docker, Compose e publicação | `cp3-i05` | Diego | depois do projeto Worker (item 4 do Pedro) — dá para estudar e rascunhar antes | `#15` |
| Requisições `.http` + experimento E04 | `cp3-http-e04` | Debora | com o ambiente Docker rodando | `#16` |
| Experimentos E02 e E05 | `cp3-e02-e05` | Diego | com o ambiente Docker rodando | `#16` |

O Pedro faz o resto: regras Mine/Global (I02), RabbitMQ e worker (I03), E01 e E03, consolidação do dossiê e
entrega (I06/I07).

## Fluxo de trabalho (igual ao CP2)

```shell
git clone https://github.com/PeOliveira18/CheckPoint2-C-.git   # ou git pull na main, se já tiver
cd CheckPoint2-C-
git config user.name "<seu nome>"
git config user.email "<seu e-mail do GitHub>"
git checkout -b <branch-da-tabela>
dotnet test ./sources/ExpenseHub.slnx
# implemente, rode os testes de novo e depois:
git add <arquivos>
git commit -m "feat(reports): ..."
git push -u origin <branch-da-tabela>
```

1. Abra a PR para `main`. Título no padrão `CP3-Ixx — <resumo>` e, na descrição, cite
   `Racass/checkpoint-csharpracass-expensehub#N` **sem** `Closes`/`Fixes`/`Resolves`.
2. Espere o workflow `code-quality` e avise o Pedro: ele baixa o `report.json` e só faz o merge com **100/100**.
3. Se puder, rode antes `pwsh ./scripts/Invoke-CodeQuality.ps1` (PowerShell 7) — ele gera o mesmo relatório
   em `artifacts/code-quality/report.md`.

Requisitos: .NET 10 SDK, Git; para Docker, Rancher Desktop ou Docker Desktop.

## Regras de código (as mesmas que garantiram 100 no CP2)

- Sem `using` implícitos: declare todos os `using` usados e nenhum a mais, em ordem (`System` primeiro).
- Classes e métodos `public` de teste precisam de `/// <summary>`; tipos da API e do Core são `internal` e não precisam.
- Sem linhas em branco dentro de `/// <summary>` (`CS1570`) — cuidado ao colar código do chat.
- Campos privados com `_camelCase`, inclusive `static readonly`; chaves `{ }` sempre; 4 espaços.
- Arrays constantes em asserts viram campo `private static readonly` (`CA1861`).
- Nada de `#pragma warning disable` nem mudanças em `.editorconfig`, `Directory.Build.props`, `scripts/`, `tests/`
  ou `.github/workflows/build.yml`.
- Testes unitários sem banco, rede ou RabbitMQ: use os fakes de `sources/ExpenseHub.UnitTests/Support`
  (`FakeReportRepository`, `FakeReportPublisher`, `FixedTimeProvider`, `TestData`, `ReportScenario`).
- **Nunca** versione tokens, senhas ou connection strings — nem em `.http`, nem em prints, nem no dossiê.

---

## Debora Ivanowski (RM 555694)

### Tarefa 1 — download Excel do relatório (`cp3-excel`)

Contrato (issue #11, seção “Download Excel obrigatório”): `GET /api/reports/{id}/excel` devolve um `.xlsx`
real montado **a partir do resultado salvo** (`ResultJson`), nunca de uma nova consulta aos reembolsos.

**Pacote** — ClosedXML 0.105.1 (licença MIT; já testado neste repositório: 0 warnings, sem vulnerabilidades):

```shell
dotnet add sources/ExpenseHub.Api package ClosedXML --version 0.105.1
```

**Arquivos a criar ou alterar**

1. `sources/ExpenseHub.Api/Application/ReportExcelExporter.cs` — `internal static class` com
   `public static byte[] Export(ReportResponse report)`. Usa somente `report` e `report.Result`. Layout fixo
   (os testes conferem estas células):

   | Célula | Conteúdo |
   |---|---|
   | A1 / B1 | `Identificador` / id do relatório (texto) |
   | A2 / B2 | `Escopo` / `Mine` ou `Global` |
   | A3 / B3 | `Data inicial` / `yyyy-MM-dd` |
   | A4 / B4 | `Data final` / `yyyy-MM-dd` |
   | A5 / B5 | `Gerado em (UTC)` / `generatedAt` no formato ISO 8601 com `Z` |
   | A7 / B7 / C7 | `Estado` / `Quantidade` / `Valor` |
   | linhas 8–12 | um estado por linha, na ordem de `report.Result.ByStatus`; B = quantidade (número), C = valor (número, formato `#,##0.00`) |
   | linha 13 | `Total` / `TotalCount` / `TotalAmount` (números) |

   Contagens e valores **numéricos** (`int` e `decimal`), não texto. Grave num `MemoryStream` e devolva os bytes.
   Datas e números formatados como texto devem usar `CultureInfo.InvariantCulture` (`CA1305`).

2. `sources/ExpenseHub.Api/Application/ReportExcelFile.cs` —
   `internal sealed record ReportExcelFile(string FileName, byte[] Content);`

3. `ReportService` — novo método
   `Task<ServiceResult<ReportExcelFile>> GetExcelAsync(UserContext user, Guid id, CancellationToken cancellationToken)`:
   - busca com `_repository.FindOwnAsync(id, user.UserId, ...)`; `null` → `ServiceError.NotFound` (404, inclusive para Auditor);
   - status diferente de `Completed` (Pending, Processing **ou** Failed) → `ServiceError.Conflict` (409);
   - senão → `ReportExcelExporter.Export(ReportResponse.From(report))` com nome `report-{id}.xlsx`.

4. `ReportEndpoints` — mapear `GET /{id:guid}/excel` (nome `GetReportExcel`) e devolver
   `TypedResults.File(file.Content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", file.FileName)`.
   Passar o nome do arquivo já gera o `Content-Disposition: attachment`.

**Testes** — `sources/ExpenseHub.UnitTests/Reports/ReportExcelTests.cs`. Use como modelo
`ReportServiceTests.GetCompletedReportReturnsSavedResultWithoutRecalculating` (semeia `ReportScenario.ApostilaExpenses()`,
solicita, processa com `ReportProcessor` e consulta). Abra os bytes com `new XLWorkbook(new MemoryStream(bytes))` e confira:

- relatório concluído: arquivo abre, cabeçalhos da linha 7 corretos, Approved = 2 / 150,00, Paid = 1 / 25,00,
  total 3 / 175,00, e as células B/C são numéricas (`cell.DataType == XLDataType.Number`);
- alterar um reembolso **depois** de concluir não muda a planilha (continua 175,00);
- conjunto vazio: cinco estados com zero e total zero;
- relatório Pending → `ServiceError.Conflict`; relatório Failed → `ServiceError.Conflict`;
- relatório de outro usuário → `ServiceError.NotFound`, mesmo se quem pede for Auditor;
- nome do arquivo `report-{id}.xlsx`.

Commit sugerido: `feat(reports): excel download from saved result`. PR: `CP3-I01 — Download Excel do relatório`,
citando `Racass/checkpoint-csharpracass-expensehub#11`.

### Tarefa 2 — requisições `.http` e experimento E04 (`cp3-http-e04`)

Começa quando o ambiente Docker do Diego estiver rodando e o POST devolver 202 (depois do item 4 do Pedro).

**Parte A — `sources/ExpenseHub.Api/ExpenseHub.Api.http`.** No final do arquivo, no mesmo formato das
existentes (`###` + título com o status esperado), adicione:

- variáveis só com placeholders: `@employeeToken`, `@employee2Token`, `@auditorToken`, `@reportId`;
- `POST /api/reports` Mine como Employee → 202 com `Location`;
- `POST /api/reports` Global como Auditor → 202;
- `POST /api/reports` Global como Employee → 403;
- `POST /api/reports` com `fromDate` maior que `toDate` → 400; com `scope` inválido → 400; sem token → 401;
- `GET /api/reports` → 200 só com os próprios;
- `GET /api/reports/{{reportId}}` como dono → 200; como outro Employee → 404; como Auditor que não criou → 404;
- `GET /api/reports/{{reportId}}/excel` concluído → 200; de um relatório ainda Pending → 409; de outro usuário → 404.

Corpo de exemplo:

```json
{ "fromDate": "2026-09-01", "toDate": "2026-09-30", "scope": "Mine" }
```

**Parte B — dossiê, seção E04 de `docs/CP3-EXPERIMENTOS.md`.** Use duas contas Employee e uma Auditor
(papéis atribuídos pelo Admin em `PUT /api/admin/users/{id}/roles`; depois **faça login de novo**). Registre:
Employee pedindo Global (403), Employee consultando e baixando relatório da outra Employee (404/404), Auditor
pedindo Global (202) e Auditor consultando e baixando relatório criado por outra pessoa (404/404). Para cada
passo: requisição, `reportId`, status esperado e obtido. **Sem tokens** no texto ou nos prints.

---

## Diego Cabral (RM 557817)

### Tarefa 1 — Docker, Compose e publicação das imagens (`cp3-i05`)

Contrato: issue #15. O projeto `sources/ExpenseHub.Worker` chega com o item 4 do Pedro; até lá dá para
montar o da API e o Compose com o RabbitMQ.

**Nomes combinados (não mude sem falar com o Pedro):**

| Item | Valor |
|---|---|
| Imagens | `ghcr.io/peoliveira18/expensehub-api:<sha>` e `ghcr.io/peoliveira18/expensehub-worker:<sha>` |
| Serviços no Compose | `web`, `worker`, `rabbitmq` |
| Porta da API no contêiner | `8080` (padrão das imagens .NET) |
| Fila | `expensehub.reports` |
| Configuração da API e do worker | `ConnectionStrings__ExpenseHub`, `RabbitMq__HostName`, `RabbitMq__Port`, `RabbitMq__UserName`, `RabbitMq__Password`, `RabbitMq__QueueName` |
| Só a API | `Jwt__SigningKey`, `SeedAdmin__Password` |

**Arquivos**

1. `sources/ExpenseHub.Api/Dockerfile` e `sources/ExpenseHub.Worker/Dockerfile` — multi-stage, com **contexto
   na raiz do repositório** (o build precisa de `Directory.Build.props` e `.editorconfig`). Esqueleto da API:

   ```dockerfile
   FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
   WORKDIR /src
   COPY Directory.Build.props .editorconfig ./
   COPY sources/ExpenseHub.Core/ExpenseHub.Core.csproj sources/ExpenseHub.Core/
   COPY sources/ExpenseHub.Api/ExpenseHub.Api.csproj sources/ExpenseHub.Api/
   RUN dotnet restore sources/ExpenseHub.Api/ExpenseHub.Api.csproj
   COPY sources/ sources/
   RUN dotnet publish sources/ExpenseHub.Api/ExpenseHub.Api.csproj -c Release -o /app --no-restore

   FROM mcr.microsoft.com/dotnet/aspnet:10.0
   WORKDIR /app
   COPY --from=build /app .
   USER $APP_UID
   ENTRYPOINT ["dotnet", "ExpenseHub.Api.dll"]
   ```

   O do worker é igual, trocando o projeto e usando `mcr.microsoft.com/dotnet/runtime:10.0`.

2. `.dockerignore` na raiz — no mínimo `**/bin/`, `**/obj/`, `artifacts/`, `.git/`, `.vs/`, `*.env`.

3. `docker-compose.yml` na raiz:
   - `rabbitmq`: `rabbitmq:4-management`, portas `5672` e `15672`, **volume nomeado** em `/var/lib/rabbitmq`,
     `healthcheck` com `rabbitmq-diagnostics -q ping`, usuário e senha vindos do `.env`;
   - `web` e `worker`: `image:` com as referências do GHCR e a tag `${IMAGE_TAG}` (nada de `latest` e nada de
     só `build:` — a correção exige as imagens publicadas), variáveis vindas do `.env`, `depends_on` do
     `rabbitmq` com `condition: service_healthy`, `restart: unless-stopped`; `web` publica `8080:8080`;
   - o banco é o **Oracle da FIAP, externo ao Compose**: `web` e `worker` recebem a **mesma**
     `ConnectionStrings__ExpenseHub`.

4. `.env.example` na raiz — todas as variáveis acima com placeholders (`<RM>`, `<SENHA>`, `<SHA_DO_COMMIT>`).
   O `.env` real já é ignorado pelo `.gitignore` (`*.env`); confira com `git status` antes de commitar.

5. `.github/workflows/publish.yml` (arquivo **novo**; não mexa no `build.yml`):
   - dispara em `push` na `main` e em `workflow_dispatch`;
   - job `test`: `actions/setup-dotnet` 10.0.x e `dotnet test ./sources/ExpenseHub.slnx`;
   - job `publish` com `needs: test` e `permissions: packages: write, contents: read`: login no `ghcr.io` com
     `GITHUB_TOKEN`, `docker/build-push-action` para as duas imagens, `context: .`, `file:` apontando para cada
     Dockerfile e `tags:` com `${{ github.sha }}`.

6. `docs/EXECUCAO-DOCKER.md` — passo a passo reproduzível: copiar `.env.example` para `.env` e preencher;
   aplicar as migrations uma vez no Oracle
   (`dotnet ef database update --project sources/ExpenseHub.Core --startup-project sources/ExpenseHub.Api`, com a
   connection string nos user-secrets); `docker compose pull`; `docker compose up -d`; `docker compose ps`;
   painel do RabbitMQ em <http://localhost:15672>; como conferir a versão em execução
   (`docker compose images`, `docker inspect`).

Depois da primeira publicação, o Pedro torna os dois pacotes **públicos** (GitHub → Packages → Settings).

Commit sugerido: `build(docker): dockerfiles, compose and image publishing`. PR: `CP3-I05 — Publicação e execução
reproduzível`, citando `Racass/checkpoint-csharpracass-expensehub#15`.

### Tarefa 2 — experimentos E02 e E05 (`cp3-e02-e05`)

Contrato: issue #16, seção 9. Preencha as seções E02 e E05 de `docs/CP3-EXPERIMENTOS.md` com comandos reais,
o **mesmo `reportId`** do começo ao fim e trechos de log **sem** tokens, senhas ou connection strings.

**E02 — Worker parado**

1. `docker compose stop worker`.
2. Solicite um relatório (`POST /api/reports` → 202) e anote o `reportId`.
3. Mostre que ele continua `Pending` (`GET /api/reports/{id}`) e que a fila `expensehub.reports` tem a mensagem em
   **Ready** no painel do RabbitMQ (print sem dados sensíveis).
4. `docker compose up -d worker` e acompanhe o **mesmo** id até `Completed`.
5. `docker compose logs --tail 80 worker` com as linhas desse `reportId`.

Explique que Ready/Unacked são métricas do broker, não os estados Pending/Processing da aplicação.

**E05 — Persistência e versão**

1. Com um relatório já `Completed` (anote o `reportId` e o resultado).
2. `docker compose pull` e `docker compose up -d --force-recreate web worker` — **sem** `down -v` e sem apagar volumes.
3. Mostre a versão em execução (`docker compose images` e/ou `docker inspect` com a tag `<sha>`).
4. Consulte o mesmo `reportId` (faça login de novo se precisar) e mostre que o resultado é idêntico ao anterior.
