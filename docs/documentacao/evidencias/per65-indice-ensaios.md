# PER-65 · índice de ensaios e gabaritos

Em 21/11/2026, o usuário aprovou retirar o GonkaBroker da cadeia. Os ensaios de quatro provedores continuam sendo evidência histórica. Os resultados de qualificação da cadeia atual consideram Groq, Cohere e OpenRouter. [Contrato da PER-63](../../epicos/cor-01-classificacao-hibrida.md)

## Como confrontar a entrada e a saída

O payload v4 envia o ditado inteiro em uma chamada por provedor acionado. Cada bloco contém a frase escrita e as próprias lacunas, com ID, gabarito, resposta e indicação de avaliação. As definições selecionadas sem regra programada aparecem uma vez em `candidatos`. O esperado é um JSON com todos os IDs avaliados e um grupo por candidato. Cada grupo lista correspondências e inconclusivos. Ausência nas duas listas indica ausência de correspondência.

| Amostra | Correspondências esperadas | Controles |
|---|---|---|
| 10 lacunas, três candidatos | Concordância nas lacunas 4 e 8, inversão na 9, troca de cedilha na 10 | “A ave pousou” e “os músicos caminharam” preservam a concordância apesar da diferença em relação ao gabarito. |
| 24 lacunas, três candidatos | Concordância em 2, 3, 8, 16 e 19. Inversão em 4 e 23. Troca de cedilha em 6 e 17. | 14 “a criança chegou” fica sem achado. 16 “os professores chegou” recebe concordância. |
| 5 lacunas, API e MySQL | Regra por código em `café → cafe` e concordância docente somente no verbo de “os professores chegou” | Nota 20/100, “a criança chegou” sem achado, releitura idêntica. |

As demais lacunas esperam ausência de correspondência. A métrica de lacunas exatas exige o conjunto correto de correspondências e nenhum estado inconclusivo naquela lacuna. Uma falha de transporte ou de validação deixa o lote sem pontuação pedagógica. [Amostras e rótulos](per65-contexto-lote-2026-11-19.md)

## Rodadas e evidências

| Rodada | Enviado e esperado | Efetivamente recebido | Consequência |
|---|---|---|---|
| Economia inicial | Amostra de 12, chamadas individuais e variantes em lote | Lote com texto usou 1.325 tokens ante 8.051 em chamadas individuais, com 11/12 lacunas exatas | O lote reduziu os tokens no cenário medido. [Registros dos quatro provedores](per65-benchmark-llm-2026-11-19.md) |
| Formato de saída | Amostra de 24 no Groq 20B, schema estrito e modo JSON | 2/3 saídas válidas no schema estrito e 0/3 no modo JSON. Dois controles válidos confundiram as concordâncias | Justificou manter validação local e testar agrupamento por frase. [Pedidos e retornos](per65-groq-ab-2026-11-19.md) |
| Contexto na aplicação | 24 e 10 lacunas, texto escrito contínuo nas versões v2 e v3 | O modelo voltou a associar “as crianças” ao verbo da lacuna 14 | A v4 vincula os IDs aos seus trechos. [Corpos e divergências](per65-cliente-dotnet-contextual-2026-11-19.md) |
| Groq 120B v4 | 24 e 10 lacunas, três candidatos | Com teto de 6.144 tokens, 23/24 e 10/10. Com teto de 4.096, 24/24 e 10/10 em outra execução | Referência sintética com 4.096 tokens e 30 s. [Capturas de 6.144](per65-cliente-v4-2026-11-21.md) · [Capturas de 4.096](per65-provedores-v4-2026-11-21.md#groq--24-lacunas--teto-de-4096-tokens) |
| Cohere Command A+ | Mesmo payload v4, resposta esperada em JSON | Bloco `thinking` antes de `text`. Com adaptador corrigido, 10/10 com teto de 8.192. Lote longo truncou a 4.096 ou expirou a 8.192 | `e6f2b8d` lê os blocos textuais. O QA confirmou as classificações esperadas no E2E curto e observou duas lacunas em revisão. [Capturas](per65-provedores-v4-2026-11-21.md#reteste-do-adaptador-cohere-corrigido) |
| OpenRouter | Payload v4 com roteamento dinâmico e modelos gratuitos fixos | Ling e ERNIE deram resultados utilizáveis na primeira amostra curta. Repetições tiveram HTTP 429, truncamento ou timeout | Sem evidência de estabilidade no lote longo. [Modelos fixos](per65-provedores-v4-2026-11-21.md#modelos-adicionais-com-schema-estrito) · [Reteste a 8.192](per65-provedores-v4-2026-11-21.md#openrouter-com-teto-de-8192-tokens) |
| GonkaBroker | Schema v4, variantes em modo JSON, lote reduzido e controle de schema com um campo | Lote de dez expirou. Quatro lacunas retornaram texto malformado. O controle de schema retornou texto puro | A saída reduzida também seria rejeitada pela API. [Diagnóstico e capturas](per65-provedores-v4-2026-11-21.md#diagnóstico-de-geração-no-gonkabroker) |
| Rotação real | Cinco lacunas em uma tentativa pela API, GonkaBroker seguido de Groq | GonkaBroker expirou. Groq HTTP 200, cinco lacunas concluídas, classificações e releitura conforme o esperado | Confirmou troca real após falha técnica. [Trilha e resultado](../../relatorio-ciclo-per65-contextual-2026-11-19.md#rotação-real-após-timeout) |
| Definições docentes inéditas | Oito lacunas e três novas definições em v4. O gabarito prevê quatro correspondências e um inconclusivo na lacuna vazia | Cohere 8/8 estados, Groq 7/8 e GonkaBroker 6/8. OpenRouter HTTP 429 | A comparação confirma a aplicação de novas definições nos lotes recebidos e evidencia divergências por modelo. [Rodada inicial](per65-provedores-v4-2026-11-21.md#generalização-para-três-definições-docentes-inéditas) · [Complemento](per65-provedores-v4-2026-11-21.md#openrouter-e-gonkabroker-com-as-mesmas-definições-docentes-inéditas) |
| Reteste com o critério aprovado | v4 original, amostras de 24 e 10, controle negativo 14 no lote longo e controles 2 e 6 no curto | Groq 22/24 e 10/10, Cohere 21/24 e 9/10. Os quatro lotes passaram nos controles e na validação estrutural | As divergências gerais e os resultados anteriores continuam no histórico. [Corpos e gabarito](per65-provedores-v4-2026-11-21.md#repetição-com-o-critério-de-controles-negativos-aprovado) |
| OpenRouter em lote longo | ERNIE, v4 de 24 lacunas, teto de 8.192, janela de 60 s | HTTP 200 e 21/24. Sem falso positivo no controle de concordância da lacuna 14. Falsos positivos de inversão em 12 e 18 | Uma execução completa. [Corpos e gabarito](per65-provedores-v4-2026-11-21.md#openrouter-ernie-com-teto-de-8192-e-janela-de-60-segundos) |
| GonkaBroker em lote longo | GLM, v4 de 24 lacunas, teto de 8.192, janela de 60 s | Timeout, sem resposta classificável | Sem avaliação nos controles. [Corpo e desfecho](per65-provedores-v4-2026-11-21.md#gonkabroker-glm-com-teto-de-8192-e-janela-de-60-segundos) |
| Free-LLM Router em modo guest | Payload v4 de 8, 10 e 24 lacunas, três repetições por amostra, sem chaves de provedores | Nove retornos HTTP 503 `no_providers`. Catálogo com zero rotas guest | 0/9 saídas utilizáveis. Precisão e controles sem avaliação. [Relatório para o TCC](per65-free-llm-guest-2026-11-21.md) |

## Decisões e evidência exigida para avançar

- A v4 mantém o contexto do ditado e usa um único lote por tentativa e provedor. A nota objetiva e as regras programadas operam na submissão.
- A rotação ocorre por falha técnica. Saída incompleta ou fora do contrato vai para revisão. A decisão preserva o significado das lacunas ausentes na resposta.
- O perfil Groq 120B com 4.096 tokens e timeout de 30 segundos é referência para reprodução sintética. Os valores observados são resultados das amostras estudadas.
- A escolha dos modelos de backup para produção segue pendente. Os resultados mostram timeouts, limites, inconclusivos e falsos positivos. Um retorno HTTP bem-sucedido precisa ser acompanhado de saída validada e confronto com o gabarito.
- A decisão de ativação requer aprovar os modelos e a ordem, os limites de tempo e uso, os dados permitidos e o critério pedagógico. A especificação e a PER-65 registram essas condições. Os ensaios sintéticos demonstram o funcionamento do contrato, a correção híbrida e o fallback após timeout. Cada rodada registra o desempenho observado e suas limitações. [Contrato da correção híbrida](../../epicos/cor-01-classificacao-hibrida.md) · [PER-65](https://linear.app/joaomoutinho/issue/PER-65/configurar-e-validar-os-quatro-provedores-de-classificacao-llm)
- Em novas rodadas, registrar provedor e modelo, alterações da entrada, gabarito, resposta sanitizada, diferenças observadas, contagem disponível de tokens e decisão resultante.

## Critério de qualificação aprovado em 21/11/2026

O usuário aprovou exigir zero falsos positivos nos controles negativos definidos antes do ensaio. Para as demais lacunas, registrar separadamente falsos positivos, omissões, inconclusivos esperados e inesperados, e timeouts. Uma execução sem resposta utilizável deixa o controle sem avaliação. O critério exige observar também os achados positivos esperados, para distinguir uma classificação correta de uma resposta sem achados.

| Modelo e rodada | Controles negativos observados | Outros resultados relevantes |
|---|---|---|
| Groq 120B, v4, amostra de 24, teto 4.096 | Zero. O controle 14 ficou sem marcação. | 24/24 e concordância esperada em 16. |
| Groq 120B, v4, amostra de 8 | Zero nos controles da amostra. | Omissão de concordância na lacuna 7. |
| Cohere Command A+, v4, amostra de 24, teto 8.192 e janela de 60 s | Um. Marcou concordância em “a criança chegou”, lacuna 14. | 22/24, incluindo omissão de inversão em 23. |
| Cohere Command A+, v4, amostras de 8 e 10 com resultados válidos | Zero nessas execuções. | 8/8 e 10/10 nos respectivos gabaritos. Outras execuções da amostra de 10 tiveram saída incompleta. |
| GonkaBroker DeepSeek V4 Flash, v4, amostra de 8 | Um no controle de frase correta, lacuna 2. | Na lacuna 8, devolveu correspondência quando o esperado era inconclusivo. |
| OpenRouter Ling 3.2 Flash, v4 | Zero na primeira execução válida da amostra de 10. Repetições sem avaliação por timeout ou HTTP 429. | 10/10 na execução válida. A amostra de 24 segue sem resultado classificável. |

Os resultados acima aplicam o critério por execução. Cohere e GonkaBroker já apresentaram falsos positivos em controles da v4. O OpenRouter tem evidência válida em um lote curto e verificação pendente nos demais lotes. A decisão não estabelece um percentual global de aceitação nem autoriza uso de respostas reais.

## Protocolo de ensaio para qualificar os backups

Congelar antes do ensaio as amostras de 8, 10 e 24 lacunas, os candidatos e os rótulos por par de lacuna e definição. Registrar quais casos são controles negativos de concordância e quais esperam estado inconclusivo. Rodar os mesmos insumos em cada modelo com o payload v4 e registrar o teto e o timeout de cada ambiente. Repetir as execuções para observar variação. Anotar limites do provedor, incluindo HTTP 429, sem pontuar uma classificação que não chegou.

Para cada modelo, publicar o total de chamadas, as concluídas no contrato, os tempos observados e o total de erros por tipo. Nas respostas válidas, contar falsos positivos, omissões, concordâncias preservadas e inconclusivos previstos e inesperados. Comparar separadamente a ocorrência de falso positivo nos controles negativos e a taxa observada no conjunto da amostra. Registrar aprovação do critério pedagógico e dos limites antes de classificar a evidência como suficiente para respostas reais.

As repetições ficam pendentes. Os resultados existentes foram obtidos em configurações e rodadas distintas e servem como evidência exploratória. A tabela de rodadas aponta para os corpos originais e seus limites.
