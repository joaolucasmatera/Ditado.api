# Base da PER-77 · 25/11/2026

PER-78 e PER-79 implementadas em `codex/per-77`, com base no backend da PER-70 em `dfafe1f`. A API inclui a categoria pedagógica gravada por lacuna na submissão, no resultado do aluno e no histórico do professor.

## Contrato e persistência

- Cada classificação expõe `categoriaPedagogica` com `codigo`, `nome` e `familia`, além dos campos já usados para tipo de erro e origem. Categorias não ortográficas guardam o nome informado pelo professor e família nula.
- A correção por Código grava a categoria no registro do achado. A análise LLM congela a categoria da definição no candidato ao enfileirar a análise e a devolve quando a correspondência é validada.
- A migração `20261125120000_AdicionarCategoriaPedagogicaAosAchados` acrescenta seis colunas opcionais. O histórico devolve categoria nula quando os valores estão ausentes e preserva os estados pendente, em revisão e falha. O estado da análise fica no nível da lacuna.

## Mapeamento da PER-79

A categoria explícita da definição tem prioridade. Para novos achados de regras programadas sem categoria explícita, o mapeamento usa a regra executada. A categoria legada do ditado fica fora da resolução.

| Regra programada | Categoria pedagógica | Justificativa |
| --- | --- | --- |
| Acentuação | Híbrida / Acentuação (`HIBRIDO_ACENTUACAO`) | O executor identifica diferenças nos diacríticos da palavra. A categoria representa o achado emitido pela regra de acentuação. |
| Troca de grafemas surda/sonora | Fonologia da língua / Segmento (`FONOLOGIA_SEGMENTO`) | O executor identifica trocas entre pares de consoantes que se distinguem pela sonoridade. |
| Complexidade silábica | Fonologia da língua / Sílaba (`FONOLOGIA_SILABA`) | A regra identifica alteração em encontros consonantais seguidos de vogal. |
| Regra contextual | Sistema ortográfico / Regras contextuais (`ORTOGRAFIA_CONTEXTUAL`) | A escolha entre `r` e `rr` depende da posição entre vogais. |
| Grafia arbitrária | Sistema ortográfico / Arbitrariedade (`ORTOGRAFIA_ARBITRARIA`) | A regra cobre grafias como o `h` inicial e escolhas de grafemas que o som, por si só, não resolve. |

A regra ampla de nasalização abrange til, inserção ou omissão de marcadores e trocas contextuais entre `m` e `n`. Omissão, acréscimo e substituição descrevem operações que podem ocorrer em várias categorias. Juntura/segmentação, concordância/flexão e maiúsculas/pontuação ficam sem categoria padrão nesta fatia. Definições docentes legadas sem categoria continuam com valor nulo. A migração conserva as categorias do catálogo e os achados históricos.

Fontes do mapeamento na [taxonomia da PER-70](../Ditado/docs/ESPECIFICACAO_PG2.md#101-taxonomia-com-foco-no-português-brasileiro), no [Quadro 16 de Saggiomo (2018)](referencias/Tipo_Erros_Ortograficos_2_251127_114445.pdf#page=54) e nos [executores do classificador](../Ditado.Dominio/Services/ClassificadorTiposErro.cs).

## Arquivos e validação

- `DitadoService.cs` grava a categoria e reutiliza `ResultadoTentativa.cs` na submissão e na releitura. `CategoriasPedagogicas.cs` concentra o mapeamento. As entidades `RespostaSegmentoTipoErro` e `LlmAnaliseCandidato`, suas configurações e a migração guardam os valores históricos. Os controllers de ditado, aluno e professor documentam o campo no Swagger.
- Build da API via `scripts/build-api.ps1` com zero erros e dois avisos existentes do EF Relational e de variável de exceção não usada.
- `dotnet test Ditado.Testes/Ditado.Testes.csproj --filter "FullyQualifiedName~CategoriasPedagogicasTests|FullyQualifiedName~Cor01ControllerTests|FullyQualifiedName~Per69ControllerTests|FullyQualifiedName~ProfessorHistoricoControllerTests"` aprovou 33 testes. O fluxo HTTP sintético submeteu o mesmo ditado com Acentuação por Código e Concordância verbal por LLM, mudou as duas definições depois da submissão e confirmou as categorias originais no resultado do aluno e no histórico do professor. O Router foi simulado por fixture HTTP. Os testes também conferiram o Swagger, o acesso de outra escola, os achados antigos e os estados LLM.
- A suíte completa registrou 350 aprovados, um ignorado e as mesmas 17 falhas do commit-base, comparadas pelo nome no TRX.
- No MySQL 8, a migração foi aplicada ao schema isolado `ditado_per77_validacao_20261125` com um achado legado de Código e um de LLM. As seis colunas opcionais ficaram nulas nos dois achados e as duas categorias correntes do catálogo foram preservadas.

## Resultados e CSV

As PER-80 a PER-83 usam o mesmo contrato de categoria histórica nas branches `codex/per-77` do backend e do frontend. A branch frontend parte de `a387dcd`.

- PER-80. O endpoint `GET /api/exportacoes/respostas?escolas={ids}` acrescenta a coluna `classificacoes` ao CSV operacional. A coluna contém um array JSON com `lacunaId`, `origem`, `tipoErro`, `erroCatalogoId`, `categoriaPedagogicaCodigo`, `categoriaPedagogicaNome` e `categoriaPedagogicaFamilia` por achado. Valores de categoria ausentes são strings vazias. O CSV mantém a escola autorizada, a auditoria, as seis colunas existentes, uma linha por tentativa e o cursor por ID. Os dois CSVs pseudonimizados de pesquisa, a revisão humana e a liberação de dados ficam na PER-66.
- PER-81. `ProfessorService.cs` preenche `errosPorCategoria` no DTO do resumo e `ResultadoDitado.vue` exibe a contagem da melhor tentativa de cada aluno. O grupo `Categoria não registrada` reúne achados sem categoria. As lacunas apenas pendentes ou inconclusivas continuam representadas pelo estado da análise.
- PER-82 e PER-83. `ResultadoAluno.vue` e `ResultadoDitado.vue` mostram a categoria ao lado do erro e da origem, inclusive a partir do botão **Resultados**. O modelo efetivo do achado de IA fica disponível apenas no histórico do professor. Os resultados legados distinguem achado sem categoria de lacuna com análise pendente ou em revisão.

### Verificação da entrega

- Build oficial da API e `npm run build` do frontend aprovados. Testes focados de categoria, COR-01, análise híbrida, histórico e exportação passaram em 38 casos. A suíte completa teve as mesmas 17 falhas do commit-base, 350 aprovações e um teste ignorado.
- Playwright validou `resultados-cartao-aluno.e2e.js` e `historico-professor.e2e.js` com o frontend compilado. No cenário com MySQL isolado e IA simulada, duas tentativas exibiram Acentuação por Código e Concordância verbal por IA. O melhor resultado alimentou os grupos do professor. O CSV incluiu as duas categorias e outra escola teve a exportação recusada. Fixtures de tela cobriram categoria ausente, análise pendente e revisão.

A publicação e a validação independente de QA seguem pendentes.

## Correção da exibição de categorias

Em 25/11/2026, a revisão encontrou que `ResultadoTentativa.cs` descartava achados de Código sem vínculo com o catálogo antes de ler a categoria gravada. O resumo do professor recebia apenas o tipo de erro e o agrupava como categoria ausente. Agora a leitura mantém a categoria do achado no resumo, no histórico e no resultado do aluno. Achados antigos sem categoria gravada mantêm a indicação **Categoria não registrada**, inclusive se a definição atual já tiver uma categoria.

`MarketplaceService.cs` usa a mesma resolução que a correção para apresentar categorias das regras programadas com mapeamento da PER-79. Nasalização e outras regras sem mapeamento padrão dependem de categoria explícita da definição. `ResultadoDitado.vue` chama a lista de classificações específicas de **Erros identificados**. O agrupamento pedagógico mantém o título **Erros por categoria**. A API mantém as chaves JSON e a categoria continua associada ao erro do catálogo e ao achado gravado.

O build da API e o build Vite passaram. Os 94 testes focados exercitaram catálogo, correção híbrida, resultado e histórico via API com EF InMemory. O Playwright validou o frontend compilado com dados sintéticos em `categorias-resultados.e2e.js` e `resultados-cartao-aluno.e2e.js`, incluindo o cadastro e a edição de ditado sem categoria. Esta rodada usou preview isolado porque o Docker local estava indisponível. A publicação no serviço compartilhado segue pendente.

Para o QA, com os dois builds de correção em um ambiente de validação, abra o resumo de uma turma com uma tentativa recente que tenha achados de Código e IA categorizados e uma tentativa legada sem categoria. Confira o agrupamento da melhor tentativa, abra o histórico e consulte o resultado do mesmo aluno pelo botão **Resultados**. Confira que a categoria gravada aparece em ambos, que o valor ausente permanece explícito e que o cadastro e a edição de ditado apresentam o seletor de erros.
