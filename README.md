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
│   ├── Auth/            # Identity, JWT, seed do Admin
│   ├── Contracts/       # DTOs de resposta
│   ├── Data/            # DbContext, mapeamentos e migrations
│   ├── Domain/          # entidades e enums do domínio
│   ├── Dtos/            # DTOs de entrada com validação declarativa
│   ├── Endpoints/       # minimal APIs
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
