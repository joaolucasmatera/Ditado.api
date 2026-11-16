# PER-59 — Publicação de erro e consulta no catálogo

O backend da branch `codex/per-59-publicacao-catalogo`, derivada de `development` em `5526a4d`, foi validado em API de teste em memória. A PER-59 integra a PER-58. [Issue PER-59](https://linear.app/joaomoutinho/issue/PER-59/cat-bug-01-publicacao-nao-aparece-na-listagem)

## Causa e correção

- O POST público do formulário envia `nivel=1`, `turmaIds=[]` e omite `escolaId`. O serviço gravava primeiro a definição ativa, inferia a escola do autor e então tentava publicar. A validação recusava a combinação de Público geral com escola-alvo. O uso de `Forbid(string)` fazia o ASP.NET Core tentar usar a mensagem da exceção como esquema de autenticação.
- O worktree `codex/catalogo-erros` contém a correção histórica da PER-50 junto de outras alterações locais. A PER-59 usa a mesma gravação conjunta e conserva a escolha de visibilidade, com Instituição como padrão. As referências remotas consultadas não tinham uma branch para a PER-59. [Histórico da PER-50](https://linear.app/joaomoutinho/issue/PER-50/cat-cadastro-de-erro-nao-aparece-na-lista-ativa)
- `Ditado.Aplicacao/Services/MarketplaceService.cs` verifica o vínculo ativo do ator, resolve a escola apenas para níveis escolares e grava a definição e a publicação de versão 1 em um único `SaveChangesAsync`. `Ditado.Aplicacao/Services/VisibilidadeService.cs` valida o alcance e as turmas-alvo antes da gravação.
- `Ditado.API/Controllers/ErrosCatalogoController.cs` documenta e retorna 201 com `Location`, 400 para dados ou escopo inválidos e 403 para falta de permissão. A autenticação continua exigida. O contrato da publicação com escopo está no [contrato CAT-02](epicos/marketplace-cat02.md#publicação-com-visibilidade-per-59).

## Evidência

| Verificação | Resultado |
| --- | --- |
| Reprodução na base `5526a4d` | 8 de 9 casos falharam. A publicação pública foi recusada, a mensagem foi interpretada como esquema de autenticação e nível 99 recebeu 201. |
| Build com `scripts/build-api.ps1` e SDK .NET 9.0.320 | 0 erros e 2 avisos preexistentes de versões de EF Relational e variável sem uso. |
| `MarketplaceControllerTests`, `Per53TenantScopeTests` e `VisibilidadeServiceTests` | 65 aprovados. Cobertura de POST público e institucional com 201, publicação vigente persistida e GET da lista ativa e do detalhe por um segundo professor. O professor de outra escola recebe o registro público e não recebe o institucional. Também foram verificados o alvo por turma, as respostas 400/401/403, a ausência de gravação parcial nas recusas e o Swagger gerado. |
| Suíte completa | 244 aprovados e 21 falhas em 265 testes. A comparação dos nomes nos resultados TRX confirmou que as 21 falhas já ocorriam na base. |

O SDK já instalado em `%USERPROFILE%\.dotnet9` foi vinculado a `%LOCALAPPDATA%\dotnet-sdk-9`, caminho que o script oficial procura. Os testes usaram runtime .NET 9.0.22, TestServer e EF InMemory.

O Docker Engine local e as portas 5031 e 5173 estavam indisponíveis. A persistência no MySQL e o fluxo no navegador aguardam validação no ambiente local. Nenhuma operação foi feita no MySQL compartilhado. A validação independente de QA segue o gate de coordenação.
