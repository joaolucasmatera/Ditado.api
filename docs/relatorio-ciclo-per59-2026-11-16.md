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

## Revisão de isolamento da alteração de visibilidade

A coordenação encontrou um bloqueio para o QA em `POST /api/ErrosCatalogo/{id}/visibilidade`. A rota anterior validava o ator e o destino, mas permitia republicar um erro institucional de outra escola como público. Em TestServer, na versão `5a9ea61`, os três casos de negativa esperavam 404 e recebiam 204. Os três casos autorizados já passavam.

`VisibilidadeService` usa a mesma regra de administração da edição existente para autorizar a republicação pelo autor com vínculo ativo, pelo Administrador da Escola da autoria ou da publicação vigente e pelo Administrador Geral. A edição continua sujeita às suas próprias regras de imutabilidade. A rota de visibilidade verifica também o destino e encerra a publicação anterior somente após as validações. `ErrosCatalogoController` documenta 404 para registro inexistente ou fora do escopo administrativo.

- Build oficial com SDK 9.0.320 concluído com 0 erros e 2 avisos preexistentes.
- Os 72 testes pertinentes passaram. A alteração entre escolas e por professor sem autoria retorna 404, preserva a versão vigente e mantém o detalhe oculto ao ator externo. Autor e administradores autorizados recebem 204 e criam uma nova versão com o histórico anterior preservado. O destino escolar também foi validado e o Swagger gerado foi conferido.
- A suíte completa teve 251 aprovados de 272. As 21 falhas são as mesmas da compilação anterior.

O gate da coordenação recompilou o commit `3e402fb` e confirmou os 72 testes pertinentes. A suíte completa teve 251 aprovações de 272. A comparação com a referência `5526a4d` confirmou que os seis casos de publicação que falhavam passaram e que as 21 falhas restantes já existiam. [Acompanhamento da suíte na PER-57](https://linear.app/joaomoutinho/issue/PER-57/backend-investigar-e-corrigir-27-falhas-da-suite-de-testes)

## QA independente em MySQL e navegador

O QA aprovou o backend `3e402fb` com o frontend `a59529c`, duas contas sintéticas de escolas diferentes e um schema isolado no MySQL local. As publicações pública e institucional retornaram 201 com `Location` e apareceram na lista ativa. O autor abriu o detalhe de ambas. A outra escola viu somente o registro público e recebeu 404 para o detalhe institucional. No navegador, publicar pelo formulário exibiu o recibo e o erro apareceu ao voltar à lista.

Os pedidos inválidos retornaram 400 e o MySQL confirmou a ausência de gravação parcial. A republicação pelo autor retornou 204, preservou o histórico e registrou a versão 2 vigente. A tentativa por outra escola retornou 404 e manteve o erro institucional. O Swagger respondeu 200 com as duas operações.

O schema isolado precisou de um ajuste manual de cadastro, PIN e chamada. As três migrations correspondentes já existiam em `5526a4d` e o EF não as descobriu. A PER-59 preserva o schema do projeto, e a validação usou apenas dados sintéticos. [Registro do QA na PER-59](https://linear.app/joaomoutinho/issue/PER-59/cat-bug-01-publicacao-nao-aparece-na-listagem)
