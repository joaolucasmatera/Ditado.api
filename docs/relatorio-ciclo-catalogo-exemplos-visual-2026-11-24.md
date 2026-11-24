# Numeração dos exemplos no formulário do catálogo

O componente `src/components/ExemplosErroCampos.vue` apresenta “Exemplo 1”, “Exemplo 2” e assim por diante na publicação e na edição. Os blocos têm mais espaço vertical, e a numeração acompanha a remoção de linhas. A mudança de interface foi registrada no commit de frontend `f3df871`, na branch `codex/local-catalogo-exemplos`.

O build passou. O teste `tests/e2e/catalogo-exemplos.e2e.js`, com API e MySQL sintéticos, confirmou os títulos do primeiro ao quinto bloco, a renumeração ao remover o terceiro, o espaçamento em desktop e em 390 px, o limite de cinco e a persistência dos demais pares.

A imagem `ditado-backend1-frontend:catalogo-exemplos-visual-nov24` foi publicada em `localhost:5173`. A página devolveu HTTP 200 e serviu o bundle `ExemplosErroLista-D8Tj_5tn.js`. A conferência após a troca encontrou os mesmos contêineres da API e do MySQL.

Evidências em `work/logs/catalogo-exemplos-visual-build.log`, `work/logs/catalogo-exemplos-visual-playwright.log` e `work/logs/catalogo-exemplos-visual-publicacao.log`.
