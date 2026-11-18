# Correção objetiva e classificação híbrida

## COR-01 — Primeira entrega concluída

A nota e o gabarito continuam calculados pelo comparador atual. CAT-05 persiste `erroCatalogoIds` na criação e na edição autorizada e devolve `errosCatalogo` e `possuiRespostas` no detalhe administrativo. Novos ditados exigem seleção. Na edição, omitir a lista preserva a configuração. A primeira resposta congela a seleção. Novas associações exigem registro ativo e publicação vigente compatível com a escola e as turmas autorizadas. A atribuição valida seu destino e registra as referências autorizadas na aplicação. Mudanças antes da primeira resposta validam e sincronizam as aplicações ativas.

Em ditado com seleção persistida, somente definições selecionadas com o vínculo interno a uma regra programada geram classificação por código. Regras independentes acumulam resultados. A ocorrência incorreta sem correspondência permanece revisável. O resultado mantém `tiposErro` e acrescenta `classificacoes` com `erroCatalogoId`, `codigo`, `nome` e `tipoErro` numérico. O cliente usa os nomes da classificação do catálogo e recorre aos tipos legados quando ela não existe. Registros anteriores conservam somente sua identidade histórica.

Ditados legados sem seleção continuam disponíveis para realização e submissão com nota objetiva. Nas novas tentativas, respostas incorretas ficam sem classificação por regra e podem ser revisadas. As tentativas anteriores conservam suas classificações nos relatórios.

Decisão confirmada em 17/11/2026. Um erro programado arquivado continua gerando classificação nas novas tentativas autorizadas de aplicações já atribuídas que conservem o erro na seleção. O arquivamento impede novas seleções e novas atribuições que incluam o erro. As classificações anteriores continuam vinculadas à definição original. [Registro da decisão na PER-22](https://linear.app/joaomoutinho/issue/PER-22/cor-01-integrar-erros-mapeados-a-correcao-existente#comment-53a92c17-012e-4497-aa78-456000b35282)

Fontes [PER-22](https://linear.app/joaomoutinho/issue/PER-22/cor-01-integrar-erros-mapeados-a-correcao-existente), [PER-10](https://linear.app/joaomoutinho/issue/PER-10/cat-05-selecionar-erros-aplicaveis-a-um-ditado) e [contrato do Catálogo de erros](catalogo-contrato.md).

## Entrega híbrida planejada — PER-63

Plano aprovado em 18/11/2026. Cada lacuna apresentada integra a coleta da tentativa, incluindo acertos e lacunas sem resposta. A submissão preserva a resposta original, o gabarito, o cálculo de nota e os achados por código. A gravação da tentativa inclui a tarefa LLM e as referências versionadas dos candidatos sem executor, congeladas a partir da seleção autorizada do ditado ou da aplicação. Um achado por código pode coexistir com uma sugestão LLM para a mesma lacuna.

Um worker processa a fila persistida no MySQL, com retomada após reinício e processamento idempotente. A API retorna a nota sem aguardar o worker. A consulta autorizada do resultado exibe a origem dos achados e o estado da análise, inclusive após recarregar a página. A API valida a saída inteira antes de gravar sugestões, aceita apenas candidatos selecionados e rejeita IDs desconhecidos ou duplicados e valores fora do contrato.

Groq, Cohere, Gonka Broker e OpenRouter compõem a cadeia. A configuração de cada ambiente define ordem, modelos e limites de tempo e uso. O próximo provedor é acionado em caso de timeout, falha de rede, indisponibilidade ou limite informado pelo provedor, como HTTP 429. Saída válida sem correspondência encerra a análise sem sugestão. Saída inválida, erro de autenticação ou erro de contrato são registrados separadamente e seguem para falha ou revisão. A trilha registra cada tentativa com provedor, modelo, versão das instruções, desfecho e motivo sanitizado da troca. As credenciais ficam nas variáveis de ambiente ou no gerenciador de segredos.

### Sequência da entrega

1. [PER-64 — Fluxo completo com provedor simulado](https://linear.app/joaomoutinho/issue/PER-64/classificacao-hibrida-ponta-a-ponta-com-provedor-simulado). Completar a coleta das lacunas, a migration, a fila, a validação integral e a consulta do resultado. Demonstrar em MySQL isolado, com dados sintéticos, nota preservada, achados das duas origens, retomada da fila, resultado após recarga e isolamento entre escolas. Atualizar Swagger se o contrato da API mudar.
2. [PER-65 — Validação dos quatro provedores](https://linear.app/joaomoutinho/issue/PER-65/configurar-e-validar-os-quatro-provedores-de-classificacao-llm). Verificar a saída de cada modelo configurado com dados sintéticos e comparar os quatro na mesma amostra rotulada, em avaliação separada da rotação operacional. Cobrir ordem, timeout, limite, ausência de correspondência, saída inválida e indisponibilidade de todos. O desenvolvedor apresenta build, testes e demonstração funcional antes do QA independente.
3. [PER-66 — Revisão humana e exportação para pesquisa](https://linear.app/joaomoutinho/issue/PER-66/revisao-humana-e-exportacao-auditada-para-pesquisa). Registrar decisões humanas por amostra ou exceção preservando sugestões, evidências e origem. Exportar, com autorização e auditoria, dois CSVs pseudonimizados ligados pelo ID da lacuna. O arquivo de respostas inclui acertos e lacunas vazias. O arquivo de classificações registra origem e versão aplicável.

Antes da ativação com respostas reais, definir modelos e ordem por ambiente, limites de tempo e uso, critério de aceite da amostra e dados permitidos para envio externo. A liberação para pesquisa também requer definição de perfis, permissões e escopo de acesso. [PER-63](https://linear.app/joaomoutinho/issue/PER-63/per-18-a-integracao-llm-hibrida-com-rotacao-de-provedores) · [PER-18](https://linear.app/joaomoutinho/issue/PER-18/epico-2-correcao-objetiva-e-classificacao-hibrida)

Veja a configuração dos quatro provedores, a trilha e a reprodução das validações no [relatório da PER-65](../relatorio-ciclo-per65-2026-11-18.md).
