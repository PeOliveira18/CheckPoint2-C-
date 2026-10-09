# Guia de contribuição dos integrantes

Cada integrante precisa ter **mais de um commit próprio** no repositório, feito com a **própria conta**
do GitHub. As tarefas abaixo são pequenas, reais e independentes entre si (arquivos diferentes, sem
conflito). Pode usar IA (Claude Code, Copilot etc.), mas revise e entenda o que for enviar.

## Opção A — pelo navegador (sem instalar nada)

1. Entre em <https://github.com> com **a sua conta** e abra `PeOliveira18/CheckPoint2-C-`.
2. Para editar um arquivo existente: abra o arquivo e clique no lápis (**Edit this file**).
   Para criar um arquivo: **Add file → Create new file** e digite o caminho completo.
3. Clique em **Commit changes…**, escreva uma mensagem objetiva (ex.: `test(expenses): cover description trimming`),
   marque **Create a new branch** (ex.: `debora-tests`) e clique em **Propose changes**.
4. Na tela seguinte, abra a pull request. Título no padrão `I09 — <resumo>` e, na descrição, cite
   `Racass/checkpoint-csharpracass-expensehub#9` (sem usar `Closes`/`Fixes`/`Resolves`).
5. A segunda tarefa pode ir na mesma branch (escolha **Commit directly to the `<sua-branch>` branch**)
   ou em outra PR.
6. Aguarde o workflow `code-quality` e avise o Pedro para revisar e fazer o merge.

> Pelo navegador não dá para rodar os testes antes: o CI roda por você. Se o check falhar, abra o
> resumo do job e corrija na mesma branch.

## Opção B — na sua máquina

```shell
git clone https://github.com/PeOliveira18/CheckPoint2-C-.git
cd CheckPoint2-C-
git checkout -b <nome>-tests
dotnet test ./sources/ExpenseHub.slnx
# implemente, depois:
git add <arquivos>
git commit -m "test(expenses): ..."
git push -u origin <nome>-tests
```

Requisitos: .NET 10 SDK e `git config user.name`/`user.email` com os **seus** dados.

## Regras de código (para não perder pontos de qualidade)

- Arquivos `.cs` sem `using` implícitos: declare todos os `using` usados e nenhum a mais.
- Classe e métodos `public` de teste precisam de comentário XML `/// <summary>`.
- Campos privados com `_camelCase` (inclusive `static readonly`); chaves `{ }` sempre; 4 espaços.
- Nada de `#pragma warning disable`, nem mudanças em `.editorconfig`, `Directory.Build.props`, `scripts/` ou workflow.
- Testes não podem usar banco: use `FakeExpenseRepository`, `FixedTimeProvider` e `TestData`
  (pasta `sources/ExpenseHub.UnitTests/Support`). Veja `ExpenseDraftTests.cs` como modelo.

---

## Debora Ivanowski (RM 555694)

### Tarefa 1 — teste: descrição é gravada sem espaços nas pontas

Crie `sources/ExpenseHub.UnitTests/Expenses/ExpenseDescriptionTests.cs` com uma classe de teste que:

- cria um reembolso como `TestData.Ana` usando `TestData.ValidInput(clock.Today) with { Description = "   Táxi até o cliente   " }`;
- verifica que o valor persistido (`repository.Stored(id).Description`) é `"Táxi até o cliente"`;
- (opcional) verifica o mesmo após `UpdateAsync`.

Regra coberta: validações e persistência consistentes (I04).

### Tarefa 2 — documentação: passo a passo do fluxo

No `README.md`, adicione a seção **"Fluxo de demonstração"** (antes de "Como executar") descrevendo,
em lista numerada, como usar o `ExpenseHub.Api.http` para: logar como Admin → cadastrar dois usuários →
atribuir `Employee` a um e `Approver`/`Finance` ao outro → **logar de novo** → criar → enviar → aprovar → pagar
→ consultar o histórico. Cite qual status HTTP cada passo devolve.

---

## Diego Cabral (RM 557817)

### Tarefa 1 — teste: reprovação registra quem decidiu e quando

Crie `sources/ExpenseHub.UnitTests/Expenses/ExpenseRejectionAuditTests.cs` com uma classe de teste que:

- semeia um reembolso `Submitted` da Ana (`TestData.Expense(TestData.AnaId, ExpenseStatus.Submitted)` + `repository.Seed`);
- reprova como `TestData.Carla` com uma justificativa válida;
- verifica que `DecidedById == TestData.CarlaId`, `DecidedAtUtc == clock.GetUtcNow().UtcDateTime` e
  que a entrada de histórico tem `FromStatus == Submitted` e `ToStatus == Rejected`.

Regra coberta: ator e horário derivados do servidor (I07).

### Tarefa 2 — requisições negativas de pagamento e histórico

No final de `sources/ExpenseHub.Api/ExpenseHub.Api.http`, adicione três requisições comentadas
(no mesmo formato das existentes, separadas por `###`):

- `POST /api/expenses/{id}/pay` de um reembolso ainda `Submitted` → esperado `409`;
- `POST /api/expenses/{id}/pay` feito pelo próprio dono com role Finance → esperado `403`;
- `GET /api/expenses/{id}/history` por outro Employee → esperado `404`.

Use apenas variáveis/placeholders (`{{financeToken}}`, `<ID>`); **nunca** coloque tokens ou senhas reais.
