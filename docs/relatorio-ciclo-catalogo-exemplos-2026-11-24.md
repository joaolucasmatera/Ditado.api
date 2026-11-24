# Vários exemplos no Catálogo de erros

O professor pode adicionar e remover pares de Escrita esperada e Resposta do aluno na publicação, no modal de edição e na rota de edição. Recibo e detalhe exibem os pares na ordem salva. O formulário omite linhas totalmente vazias e identifica o par incompleto. Foram incorporados a ajuda revisada para exemplos e o placeholder de observação pedagógica fornecidos pela coordenação.

`PublicarErroRequest` e `ErroPublicadoResponse` expõem `exemplos`. O primeiro par usa as colunas existentes e os demais ficam em `ExemplosAdicionaisJson`. A migração `20261124120000_AdicionarExemplosAoCatalogo` acrescenta colunas anuláveis ao catálogo e aos candidatos da LLM. Uma requisição no formato legado atualiza o primeiro par e mantém os adicionais. A validação de carga legada cobre o objeto de definição JSON. O endpoint de importação de arquivos da CAT-07 segue pendente no backend.

`DitadoService` copia todos os pares ao criar os candidatos da tentativa. `LlmAnalysisProcessor` lê a cópia já gravada quando monta o contexto. O Router envia vários exemplos como uma lista compacta de pares `[esperado, resposta]`, sem repetir o primeiro em campos separados. Um único par mantém os campos usados no contrato anterior. O prompt `per69-v6-exemplos` descreve a lista e conserva o limite de entrada configurado. A migração mantém os campos e valores dos candidatos históricos.

## Validação

Os builds da API e do frontend passaram. Os 110 testes focados abrangeram publicação, edição, leitura no formato legado, Swagger, processamento e cliente LLM. O teste do Router simulado confirmou que os dois pares congelados foram enviados mesmo depois de uma alteração simulada no catálogo. A migração ocorreu em um schema MySQL de teste separado e as duas colunas anuláveis foram conferidas. O Router real e o ambiente ativo ficaram fora dessa rodada.

O Playwright usou o frontend na porta 5198, a API na 5076 e o schema separado `ditado_catalogo_exemplos_24nov`. Conferiu o aviso de par incompleto, o descarte de uma linha vazia, a publicação de dois pares e a releitura no recibo e no detalhe. A edição no catálogo preservou o segundo par. A remoção do primeiro pela rota de edição manteve o segundo após recarregar a página. O teste pode ser repetido com `node tests/e2e/catalogo-exemplos.e2e.js` e as variáveis `E2E_API_URL` e `E2E_BASE_URL` de um ambiente sintético.

Evidências locais em `work/logs/catalogo-exemplos-build-api.log`, `work/logs/catalogo-exemplos-build-front.log`, `work/logs/catalogo-exemplos-testes.trx`, `work/logs/catalogo-exemplos-migracao.log` e `work/logs/catalogo-exemplos-playwright.log`.

## Limite de cinco pares e integração com o histórico

A publicação e as duas formas de edição abrem com uma linha opcional quando ainda não há exemplos. A interface permite adicionar até cinco linhas e reabilita o botão quando uma é removida. O Swagger declara `maxItems: 5`. POST e PUT rejeitam o sexto par com HTTP 400. O PUT rejeitado preserva os dados previamente salvos.

A branch `codex/local-catalogo-exemplos` consolidou os arquivos do histórico que já estavam no localhost antes de aplicar os commits do catálogo. Os builds das imagens passaram e 115 testes focados aprovaram a integração. O Playwright com a versão integrada validou o limite visual, a gravação, a remoção e a reabertura de uma definição sem exemplos. Uma nova aplicação da migração em schema isolado confirmou que o par legado manteve os dois valores.

Evidências em `work/logs/catalogo-exemplos-integracao.trx`, `work/logs/catalogo-exemplos-integracao-playwright.log`, `work/logs/catalogo-exemplos-limite-migracao.log` e nos logs de build das imagens.

O Playwright verificou no histórico integrado duas tentativas em tabelas verticais, a troca de ordem, as origens da correção, a melhor nota e HTTP 404 para outra escola. A validação usou exclusivamente um schema sintético. Evidência em `work/logs/catalogo-exemplos-integracao-historico.log`.

As imagens locais `ditado-backend1-api:catalogo-exemplos-limite-nov24` (revisão `0e0683e`) e `ditado-backend1-frontend:catalogo-exemplos-limite-nov24` (revisão `b60cfbf`) foram ativadas. O localhost serviu os dois fluxos e o Swagger com limite de cinco. Foram conferidos o ID do mesmo contêiner MySQL, a configuração da API, a migração registrada e as contagens de 12 sessões ativas, sete tentativas da aplicação conferida e 14 definições antes e depois. As imagens anteriores continuam disponíveis para reversão. Evidência em `work/logs/catalogo-exemplos-publicacao.log`.
