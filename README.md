# ExpenseHub — Checkpoint 2 (C#)

API REST corporativa de reembolsos com autenticação, autorização por roles e
ownership, fluxo de aprovação/reprovação, pagamento simulado, histórico e
testes unitários.

Especificação oficial: [Racass/checkpoint-csharpracass-expensehub](https://github.com/Racass/checkpoint-csharpracass-expensehub)
(issues I01–I10). Documentos do enunciado em [docs/](docs/).

## Integrantes

| Nome | RM | GitHub |
|---|---|---|
| Pedro Oliveira | 99943 | [@PeOliveira18](https://github.com/PeOliveira18) |
| Debora Ivanowski | 555694 | |
| Diego Cabral | 557817 | |

## Stack

- .NET 10 / ASP.NET Core
- Entity Framework Core 10
- Oracle Database (provider `Oracle.EntityFrameworkCore` 10.23.x)
- MSTest (testes unitários)

## Estrutura

```text
sources/
├── ExpenseHub.slnx
├── ExpenseHub.Api/
│   ├── Domain/          # entidades e enums do domínio
│   ├── Data/            # DbContext, mapeamentos e migrations
│   └── Program.cs
└── ExpenseHub.UnitTests/
```

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

As migrations ficam em `sources/ExpenseHub.Api/Data/Migrations`. A ferramenta
`dotnet-ef` está fixada no manifesto local `dotnet-tools.json`:

```shell
dotnet tool restore
dotnet ef database update --project sources/ExpenseHub.Api
```

Para criar uma nova migration (não exige conexão com o banco):

```shell
dotnet ef migrations add <NomeDaMigration> --project sources/ExpenseHub.Api --output-dir Data/Migrations
```

O build e os testes unitários não dependem de um banco em execução.

## Como executar

```shell
dotnet restore ./sources/ExpenseHub.slnx
dotnet build ./sources/ExpenseHub.slnx
dotnet test ./sources/ExpenseHub.slnx
dotnet run --project ./sources/ExpenseHub.Api/ExpenseHub.Api.csproj
```

O endpoint `GET /health` confirma que a aplicação iniciou.

## Documentação

- [Enunciado](docs/ENUNCIADO.md)
- [Requisitos e contratos](docs/REQUISITOS.md)
- [Rubrica](docs/RUBRICA.md)
- [Matriz de autorização](docs/MATRIZ-AUTORIZACAO.md)
- [Processo no GitHub](docs/PROCESSO-GITHUB.md)
- [Uso de Inteligência Artificial](docs/USO-DE-IA.md)
- [Regras do pipeline de qualidade](docs/code-quality-rules.md)
