# Correção objetiva e classificação híbrida

## COR-01 — Primeira entrega

A nota e o gabarito continuam calculados pelo comparador atual. CAT-05 persiste `erroCatalogoIds` na criação e na edição autorizada e devolve `errosCatalogo` e `possuiRespostas` no detalhe administrativo. Novos ditados exigem seleção. Na edição, omitir a lista preserva a configuração. A primeira resposta congela a seleção. Novas associações exigem registro ativo e publicação vigente compatível com a escola e as turmas autorizadas. A atribuição valida seu destino e registra as referências autorizadas na aplicação. Mudanças antes da primeira resposta validam e sincronizam as aplicações ativas.

Em ditado com seleção persistida, somente definições selecionadas com o vínculo interno a uma regra programada geram classificação por código. Regras independentes acumulam resultados. A ocorrência incorreta sem correspondência permanece revisável. O resultado mantém `tiposErro` e acrescenta `classificacoes` com `erroCatalogoId`, `codigo`, `nome` e `tipoErro` numérico. O cliente usa os nomes da classificação do catálogo e recorre aos tipos legados quando ela não existe. Registros anteriores conservam somente sua identidade histórica.

Ditados legados sem seleção continuam disponíveis para realização e submissão com nota objetiva. Nas novas tentativas, respostas incorretas ficam sem classificação por regra e podem ser revisadas. As tentativas anteriores conservam suas classificações nos relatórios.

Decisão confirmada em 17/11/2026. Um erro programado arquivado continua gerando classificação nas novas tentativas autorizadas de aplicações já atribuídas que conservem o erro na seleção. O arquivamento impede novas seleções e novas atribuições que incluam o erro. As classificações anteriores continuam vinculadas à definição original. [Registro da decisão na PER-22](https://linear.app/joaomoutinho/issue/PER-22/cor-01-integrar-erros-mapeados-a-correcao-existente#comment-53a92c17-012e-4497-aa78-456000b35282)

Fontes [PER-22](https://linear.app/joaomoutinho/issue/PER-22/cor-01-integrar-erros-mapeados-a-correcao-existente), [PER-10](https://linear.app/joaomoutinho/issue/PER-10/cat-05-selecionar-erros-aplicaveis-a-um-ditado) e [contrato do Catálogo de erros](catalogo-contrato.md).

## Planejamento aprovado para pesquisa em 17/11/2026

- A unidade de coleta é a lacuna apresentada em cada tentativa, inclusive acertos e lacunas sem resposta. O protocolo prevê fixar a configuração e as referências da coleta e preservar a resposta original.
- O LLM primário avalia as definições selecionadas sem executor em segundo plano, inclusive se uma regra independente já classificou a resposta. O modelo de contingência assume em caso de falha do primário. Uma saída válida sem correspondência permanece sem classificação por LLM. A API valida os candidatos e preserva gabarito, nota e achados de regras.
- A trilha de revisão distingue a decisão humana das sugestões automáticas e registra origem, evidência e versão da regra, do modelo ou das instruções, quando aplicável. A exportação planejada é auditada e usa dois CSVs ligados pelo ID da lacuna, com respostas pseudonimizadas e classificações.
- Comparar os dois modelos em uma mesma amostra rotulada. Provedores, critérios de aceitação, limites e regras para envio e acesso aos dados seguem pendentes. Chamadas a modelos, envio de dados externos, autorização de pesquisadores e exportação de pesquisa pertencem a etapas posteriores.

Fonte [PER-18](https://linear.app/joaomoutinho/issue/PER-18/epico-2-correcao-objetiva-e-classificacao-hibrida).
