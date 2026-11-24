# Vários exemplos no Catálogo de erros

O professor pode adicionar e remover pares de Escrita esperada e Resposta do aluno na publicação, no modal de edição e na rota de edição. Recibo e detalhe exibem os pares na ordem salva. O formulário omite linhas totalmente vazias e identifica o par incompleto. Foram incorporados a ajuda revisada para exemplos e o placeholder de observação pedagógica fornecidos pela coordenação.

`PublicarErroRequest` e `ErroPublicadoResponse` expõem `exemplos`. O primeiro par usa as colunas existentes e os demais ficam em `ExemplosAdicionaisJson`. A migração `20261124120000_AdicionarExemplosAoCatalogo` acrescenta colunas anuláveis ao catálogo e aos candidatos da LLM. Uma requisição no formato legado atualiza o primeiro par e mantém os adicionais. A validação de carga legada cobre o objeto de definição JSON. O endpoint de importação de arquivos da CAT-07 segue pendente no backend.

`DitadoService` copia todos os pares ao criar os candidatos da tentativa. `LlmAnalysisProcessor` lê a cópia já gravada quando monta o contexto. O Router envia vários exemplos como uma lista compacta de pares `[esperado, resposta]`, sem repetir o primeiro em campos separados. Um único par mantém os campos usados no contrato anterior. O prompt `per69-v6-exemplos` descreve a lista e conserva o limite de entrada configurado. A migração mantém os campos e valores dos candidatos históricos.

## Validação

Os builds da API e do frontend passaram. Os 110 testes focados abrangeram publicação, edição, leitura no formato legado, Swagger, processamento e cliente LLM. O teste do Router simulado confirmou que os dois pares congelados foram enviados mesmo depois de uma alteração simulada no catálogo. A migração ocorreu em um schema MySQL de teste separado e as duas colunas anuláveis foram conferidas. O Router real e o ambiente ativo ficaram fora dessa rodada.

O Playwright usou o frontend na porta 5198, a API na 5076 e o schema separado `ditado_catalogo_exemplos_24nov`. Conferiu o aviso de par incompleto, o descarte de uma linha vazia, a publicação de dois pares e a releitura no recibo e no detalhe. A edição no catálogo preservou o segundo par. A remoção do primeiro pela rota de edição manteve o segundo após recarregar a página. O teste pode ser repetido com `node tests/e2e/catalogo-exemplos.e2e.js` e as variáveis `E2E_API_URL` e `E2E_BASE_URL` de um ambiente sintético.

Evidências locais em `work/logs/catalogo-exemplos-build-api.log`, `work/logs/catalogo-exemplos-build-front.log`, `work/logs/catalogo-exemplos-testes.trx`, `work/logs/catalogo-exemplos-migracao.log` e `work/logs/catalogo-exemplos-playwright.log`.
