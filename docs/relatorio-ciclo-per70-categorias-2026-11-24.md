# PER-70 — Categoria pedagógica no catálogo e retirada da associação na interface de ditados

## Entrega

A publicação, a edição, o recibo, o detalhe e a lista do catálogo exibem a categoria pedagógica. As opções ortográficas vêm das três famílias e seis subdivisões do Quadro 16 de Saggiomo. O formulário permite informar um nome próprio para a categoria de um erro não ortográfico. A API entrega o código, o nome e, quando aplicável, a família. [PER-70](https://linear.app/joaomoutinho/issue/PER-70/aplicar-a-taxonomia-do-geale-a-todos-os-erros) · [Saggiomo (2018), pp. 49–54](referencias/Tipo_Erros_Ortograficos_2_251127_114445.pdf#page=49)

O menu, as páginas antigas de categorias, os filtros e os rótulos de categoria de ditado saíram da navegação do frontend. Na criação e na edição de ditado, a interface omite `categoriaIds`. No contrato legado, omissão ou `null` preserva as associações, e `[]` continua sendo uma remoção explícita. As rotas e os dados legados de ditado permanecem disponíveis para seus consumidores.

## API e migração

- `GET /api/ErrosCatalogo/categorias-pedagogicas` retorna as opções para o formulário de professor ou administrador.
- `POST /api/ErrosCatalogo` exige `categoriaPedagogica`. Exemplo ortográfico em JSON é `{"codigo":"ORTOGRAFIA_CONTEXTUAL"}`. Exemplo não ortográfico é `{"codigo":"NAO_ORTOGRAFICO","nome":"Concordância verbal"}`. Clientes de publicação anteriores precisam incluir o campo ao atualizar.
- `PUT /api/ErrosCatalogo/{id}` preserva a categoria se o campo for omitido ou nulo. A edição continua respeitando o bloqueio após a primeira associação a ditado. O recibo, o detalhe e a lista retornam categoria nula para registros legados ainda sem mapeamento.
- A migração `20261124233000_AdicionarCategoriaPedagogicaAoErro` acrescenta duas colunas anuláveis à tabela `ErrosCatalogo`. Os vínculos `DitadoCategorias` e as definições existentes conservam seus valores.

## Bases e arquivos

Os checkouts da branch `codex/per70-categorias-pedagogicas` partiram de `8990f74` no frontend, que corresponde a `origin/develop`, e de `0e0683e` no backend. O remoto do backend usa `development`. A revisão validada do backend já inclui o histórico servido e a publicação com até cinco exemplos. As alterações de outras worktrees ficaram nos seus respectivos checkouts.

- Backend em `ErrosCatalogoController.cs`, DTOs de `Marketplace`, `CategoriasPedagogicas.cs`, `MarketplaceService.cs`, entidade, configuração e migração de `ErroCatalogo`.
- Frontend em `CategoriaPedagogicaCampos.vue`, `PublicarErro.vue`, `CatalogoErros.vue`, `catalogoErrosService.js`, menu, rotas e telas de ditados do aluno e do professor.
- Testes em `MarketplaceControllerTests.cs`, fixtures de catálogo usadas por COR-01, PER-53 e PER-69 e Playwright do catálogo e da edição de ditado.

## Validação

O build da API com `scripts/build-api.ps1` e o build do frontend com `npm.cmd run build` passaram. Os 137 testes focados passaram. Na seleção ampliada, 139 passaram e quatro de exclusão de ditado falharam. As mesmas quatro falhas foram reproduzidas por nome na base `0e0683e` em checkout separado. O schema sintético `ditado_catalogo_exemplos_per70nov24` recebeu a migração e preservou um erro legado com categoria nula. O Swagger da API sintética expôs a nova rota e a categoria nas definições de requisição e resposta.

No Playwright, a publicação com categoria do GEALE mostrou a categoria no recibo. A edição para uma categoria não ortográfica foi relida e os exemplos permaneceram disponíveis. Um ditado com vínculo legado foi editado antes e depois da primeira resposta. Os dois PUT omitiram `categoriaIds`, e a leitura da API devolveu o mesmo vínculo. Também passaram a primeira entrada do aluno, a submissão e a abertura de Resultados, além do teste isolado do cartão do aluno.

Para reproduzir o navegador, compile e sirva o frontend com `VITE_API_BASE_URL` apontando para a API sintética. Use `E2E_API_URL` e `E2E_BASE_URL` do ambiente de teste com `node tests/e2e/catalogo-exemplos.e2e.js`, `node tests/e2e/cor01-fluxos.e2e.js` e `node tests/e2e/resultados-cartao-aluno.e2e.js`. Os logs correspondentes são `per70-catalogo-playwright.log`, `per70-ditado-legado-playwright.log` e `per70-cartao-aluno-playwright.log`.

Os registros do ciclo ficam em `work/logs/per70-categorias-build-api.log`, `per70-categorias-build-frontend.log`, `per70-foco.trx`, `per70-categorias-testes.trx`, `per70-base-controle.trx` e `per70-categorias-migracao.log`.

## Escopo restante da PER-70

O Router continua recebendo os nomes antigos em `ditado.categorias` como contexto legado. Esta fatia conserva esse contrato e não usa o campo para atribuir categorias pedagógicas. A classificação e a exportação ainda não recebem a categoria pedagógica.

Seguem pendentes a revisão pedagógica das regras programadas e das definições antigas, a categoria nos achados de Código e LLM, a estratégia de preservação histórica em reclassificações, as agregações e a exportação para pesquisa. A fatia foi preparada em ambiente isolado para revisão da coordenação e QA.

## Publicação local em 25/11/2026

O frontend `a387dcd` e a API `4cc1fb0` passaram a atender o catálogo no localhost. A migração aditiva da categoria pedagógica foi registrada no MySQL local. A conferência obteve as mesmas 24 sessões ativas, sete tentativas do cenário acompanhado e 15 definições antes e depois da troca. A configuração da API e o contêiner MySQL foram preservados.

Os builds das duas imagens passaram. O Playwright confirmou no formulário servido em `http://localhost:5173/catalogo-erros/publicar` a exigência de categoria, o envio e a releitura de uma categoria GEALE e a edição para uma categoria não ortográfica. O formulário servido no localhost usou a API e o banco sintéticos para as gravações do teste. Os exemplos continuaram disponíveis até o limite de cinco. Evidências em `work/logs/per70-publicacao-localhost.log` e `work/logs/per70-publicacao-localhost-playwright.log`.
