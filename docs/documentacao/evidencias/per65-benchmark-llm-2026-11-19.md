# Evidências do benchmark de classificação de ditados por LLM

Coleta em 19/11/2026, horário de Brasília. Experimento exploratório associado à [PER-65](https://linear.app/joaomoutinho/issue/PER-65/configurar-e-validar-os-quatro-provedores-de-classificacao-llm) e ao [relatório de ciclo](../../relatorio-ciclo-per65-2026-11-18.md).

## Perguntas e método

1. Quantos tokens uma chamada por tentativa usa em comparação com uma chamada por lacuna?
2. Quanto custa incluir o texto marcado do ditado e o que muda quando a frase determina a classificação?
3. Os modelos configurados retornam cobertura das lacunas e classificações utilizáveis dentro do contrato?

Os insumos são dois ditados sintéticos aninhados de 12 e 24 lacunas, três erros hipotéticos de catálogo e rótulos definidos antes das chamadas. Os IDs são locais. O lote de 24 acrescenta 12 lacunas ao primeiro. Todas as medições usam temperatura 0 e teto de 2.048 tokens de saída. No Groq foi usado `reasoning_effort=low`. A aplicação mantém seu fluxo por lacuna e suas próprias configurações. A comparação quantitativa com a linha de base individual foi executada no Groq para 12 lacunas. Cohere, OpenRouter e GonkaBroker foram avaliados com o texto completo.

Cada resposta foi capturada como uma linha com status, motivo sanitizado, finalização, contadores retornados pelo provedor, duração local e pares de IDs e estados extraídos. Os contadores de saída são os valores do provedor. Para Cohere, `usage.tokens` mede uso e `usage.billed_units` é preservado separadamente. Bytes representam o corpo HTTP serializado. Tokens nulos ou ausentes indicam ausência de medição. A latência é descritiva de cada chamada e inclui a rede local.

Uma lacuna tem conjunto exato quando os IDs marcados `correspondente` coincidem com os rótulos esperados. Inconclusivos são anotados separadamente e não equivalem a uma classificação negativa. A validação do contrato exige finalização normal, todas as lacunas previstas uma vez e somente IDs de erro válidos sem duplicação por lacuna. Respostas truncadas ou inválidas ficam sem nota de classificação. As saídas compactas registradas abaixo preservam os pares associados aos IDs retornados em `avaliadas`.

## Insumos sintéticos

Texto de 12 lacunas, com numeração local.

```text
Os [1] [2] cedo. A professora [3] a porta e colocou o [4] sobre a mesa. No recreio, as crianças [5] perto da [6]. A diretora pediu [7] para a atividade. Depois, eles [8] [9] da sala. No fim, o aluno [10] o [11] e fechou a [12].
```

Trecho acrescentado para 24 lacunas.

```text
No portão, [13] [14] no horário. Mais tarde, [15] [16] para a reunião. A inspetora guardou a [17] e o [18]. No pátio, as meninas [19] em silêncio. Antes da aula, os [20] ficaram [21]. A turma ouviu uma [22] e anotou a [23] no [24].
```

| ID | Definição | Descrição enviada |
|---:|---|---|
| 1 | Concordância verbal em número | Marque quando o verbo digitado não concorda em número com o sujeito da frase. Aplique somente ao verbo, não a substantivos ou adjetivos. |
| 2 | Inversão de r em encontro consonantal inicial | Marque quando a grafia esperada começa com consoante + r + vogal e, na resposta, o r e essa vogal trocam de posição. Exemplo: prato → parto. |
| 3 | Troca de ç por s antes de a | Marque quando a resposta troca apenas a letra ç por s imediatamente antes de a. Exemplo: praça → prasa. |

| Lacuna | Gabarito | Digitado | IDs de erros esperados |
|---:|---|---|---|
| 1 | alunos | alunos | Nenhum |
| 2 | chegaram | chegou | 1 |
| 3 | abriu | abriram | 1 |
| 4 | prato | parto | 2 |
| 5 | brincaram | brincaram | Nenhum |
| 6 | praça | prasa | 3 |
| 7 | papéis | papel | Nenhum |
| 8 | saíram | saiu | 1 |
| 9 | juntos | junto | Nenhum |
| 10 | guardou | guardou | Nenhum |
| 11 | caderno | Ausente (`null`) | Nenhum |
| 12 | porta | potra | Nenhum |
| 13 | as crianças | a criança | Nenhum |
| 14 | chegaram | chegou | Nenhum |
| 15 | os professores | os professores | Nenhum |
| 16 | chegaram | chegou | 1 |
| 17 | taça | tasa | 3 |
| 18 | prato | pato | Nenhum |
| 19 | entraram | entrou | 1 |
| 20 | alunos | alunos | Nenhum |
| 21 | quietos | quieto | Nenhum |
| 22 | história | historia | Nenhum |
| 23 | frase | farse | 2 |
| 24 | caderno | caderno | Nenhum |

Na lacuna 14, `chegou` concorda com `a criança`, que é o sujeito efetivamente digitado na lacuna 13. Na lacuna 16, a mesma troca de `chegaram` por `chegou` viola a concordância com `os professores`. O rótulo mede apenas a regra de concordância verbal deste ensaio.

## Formatos comparados

- **Por lacuna.** Mensagem JSON com `respostaEsperada`, `respostaFornecida` e `candidatos` com ID, código sintético, nome e descrição. Uma lacuna ausente é enviada como string vazia. A resposta estruturada contém `classificacoes` com `candidatoId`, `estado`, `confianca` e `evidencia`.
- **Lote padrão.** Mensagem JSON com `lacunas` (`id`, `esperado`, `resposta`), `erros` (`id`, `descricao`) e opcionalmente `texto`. A resposta exige um item por lacuna com a lista de erros aplicáveis ou inconclusivos. Uma lacuna ausente é enviada como `null`.
- **Lote com saída compacta.** Mesmos dados de entrada com texto. A resposta separa os IDs em `avaliadas` dos pares em `achados` (`lacunaId`, `erroId`, `estado`, `evidencia`).

Os formatos estruturados reproduzem o estilo do adaptador de cada provedor. Cohere usa `json_object` com schema. Os outros usam `json_schema` com `strict: true`. OpenRouter usa `require_parameters: true` e `allow_fallbacks: false`. O modelo efetivo do roteador foi registrado quando retornado.

A instrução do lote padrão foi:

> Classifique cada lacuna apenas pelos erros fornecidos. Se houver texto, use os marcadores [id] para reconstruir o contexto com as respostas. Resposta nula significa lacuna sem resposta. Devolva uma entrada por lacuna, com os IDs dos erros correspondentes ou inconclusivos e evidência breve. Use erros vazios se nenhum se aplicar.

Reprodução local na raiz do backend com PowerShell 7 e modelos/credenciais configurados em `.env.production`. O diagnóstico carrega as credenciais na memória e não as inclui nos resultados. Estes comandos consomem a cota do provedor escolhido.

```powershell
& '.\work\per65-token-benchmark-20261119.ps1' -Provedor Groq
& '.\work\per65-token-benchmark-20261119.ps1' -Provedor Cohere -LoteLongo -SomenteComTexto
& '.\work\per65-token-benchmark-20261119.ps1' -Provedor GonkaBroker -LoteLongo -SomenteComTexto -SaidaCompacta
```


## Resultados

### Groq — comparação direta de 12 lacunas

| Envio | Chamadas aceitas | Tokens de entrada | Tokens de saída | Tokens totais | Conjuntos exatos | Lacunas inconclusivas |
|---|---:|---:|---:|---:|---:|---:|
| Uma chamada por lacuna | 12 | 5.868 | 2.183 | 8.051 | 10/12 | 6 |
| Lote sem texto | 1 | 625 | 843 | 1.468 | 12/12 | 0 |
| Lote com texto | 1 | 710 | 615 | 1.325 | 11/12 | 0 |

O lote com texto reduziu os tokens de entrada em 87,9% e os tokens totais em 83,5%. A inclusão do texto acrescentou 85 tokens de entrada em relação ao lote sem texto. Houve um HTTP 429 extra durante a linha de base individual. O texto levou a um falso positivo na lacuna 9, `juntos → junto`, que não contém verbo.

### Lotes com o texto completo

| Provedor | Lacunas | Formato de saída | Tokens de entrada | Tokens de saída | Total | Diagnóstico |
|---|---:|---|---:|---:|---:|---|
| Groq | 12 | Padrão | 710 | 615 | 1.325 | 11/12 conjuntos exatos |
| Groq | 24 | Padrão e compacta | — | — | — | HTTP 400 `json_validate_failed` nas três chamadas |
| Cohere | 12 | Padrão | 1.179 | 371 | 1.550 | 9/12 conjuntos exatos, dez lacunas com inconclusivos, duas das cinco ocorrências identificadas |
| Cohere | 24 | Padrão | 1.480 | 1.036 | 2.516 | 13 lacunas com ID de erro desconhecido. Saída inválida |
| Cohere | 24 | Compacta | 1.487 | 653 | 2.140 | 8/24 conjuntos exatos, seis lacunas com inconclusivos, quatro ocorrências corretas entre 18 marcações |
| OpenRouter | 12 | Padrão | 535 | 2.048 | 2.583 | Truncado. Modelo efetivo `liquid/lfm-3-1.7b:free` |
| OpenRouter | 24 | Padrão | 861 | 2.048 | 2.909 | Truncado. Modelo efetivo `nvidia/nemotron-3.5-super-vl:free` |
| OpenRouter | 24 | Compacta | 845 | 2.048 | 2.893 | Truncado. Modelo efetivo `liquid/lfm-3-1.7b:free` |
| GonkaBroker | 12 | Padrão | 547 | 525 | 1.072 | Todas as 12 lacunas inconclusivas, nenhuma ocorrência identificada |
| GonkaBroker | 24 | Padrão, repetição após HTTP 429 | 854 | 796 | 1.650 | ID de erro 0 na lacuna 22. Saída inválida |
| GonkaBroker | 24 | Compacta | — | — | — | Falha de conexão, `SocketException` |

O lote Groq de 24 lacunas sem texto retornou 821 tokens de entrada, 1.015 de saída e 20/24 conjuntos exatos. Marcou a mesma falha verbal nas lacunas 14 e 16, embora ela só exista na 16 por causa do sujeito efetivamente escrito. A comparação com texto falhou nesse tamanho e, portanto, o experimento não mede ganho de qualidade com contexto nas 24 lacunas.

O consumo foi medido para os formatos e modelos desta sessão. As amostras são pequenas e sintéticas. O OpenRouter escolheu modelos diferentes entre as chamadas, o que impede atribuir a diferença apenas ao tamanho do lote. Faltam repetições independentes, comparação por lacuna nos demais provedores e validação com casos rotulados mais representativos. Nenhum dos modelos testados entregou classificação confiável para o lote de 24 com texto nesta rodada. O [ensaio de seguimento do Groq](per65-groq-ab-2026-11-19.md) registra a comparação direta dos modos de saída e os corpos completos das chamadas.

## Registros por chamada

Abaixo estão os registros sanitizados preservados na ordem de execução. Os 29 registros incluem respostas aceitas, truncadas e falhas de protocolo, limite e conexão. A projeção registra lacuna, ID de erro e estado. Nenhuma credencial, cabeçalho, frase escolar real ou texto de raciocínio do modelo foi coletado nesta evidência.

```jsonl
{"fase":"groq-12","tamanho":12,"formato":"padrao","provedor":"Groq","modelo":"openai/gpt-oss-20b","cenario":"LoteComTexto","lacuna":null,"http":200,"fim":"stop","bytes":2594,"entrada":710,"saida":615,"total":1325,"ms":1594,"classificacoes":[{"id":1,"erros":[]},{"id":2,"erros":[{"id":1,"estado":"correspondente"}]},{"id":3,"erros":[{"id":1,"estado":"correspondente"}]},{"id":4,"erros":[{"id":2,"estado":"correspondente"}]},{"id":5,"erros":[]},{"id":6,"erros":[{"id":3,"estado":"correspondente"}]},{"id":7,"erros":[]},{"id":8,"erros":[{"id":1,"estado":"correspondente"}]},{"id":9,"erros":[{"id":1,"estado":"correspondente"}]},{"id":10,"erros":[]},{"id":11,"erros":[]},{"id":12,"erros":[]}]}
{"fase":"groq-12","tamanho":12,"formato":"padrao","provedor":"Groq","modelo":"openai/gpt-oss-20b","cenario":"LoteSemTexto","lacuna":null,"http":200,"fim":"stop","bytes":2351,"entrada":625,"saida":843,"total":1468,"ms":1315,"classificacoes":[{"id":1,"erros":[]},{"id":2,"erros":[{"id":1,"estado":"correspondente"}]},{"id":3,"erros":[{"id":1,"estado":"correspondente"}]},{"id":4,"erros":[{"id":2,"estado":"correspondente"}]},{"id":5,"erros":[]},{"id":6,"erros":[{"id":3,"estado":"correspondente"}]},{"id":7,"erros":[]},{"id":8,"erros":[{"id":1,"estado":"correspondente"}]},{"id":9,"erros":[]},{"id":10,"erros":[]},{"id":11,"erros":[]},{"id":12,"erros":[]}]}
{"fase":"groq-12","tamanho":12,"formato":"padrao","provedor":"Groq","modelo":"openai/gpt-oss-20b","cenario":"PorLacuna","lacuna":1,"http":200,"fim":"stop","bytes":1760,"entrada":489,"saida":153,"total":642,"ms":542,"classificacoes":[{"id":1,"erros":[{"id":1,"estado":"correspondente"},{"id":2,"estado":"correspondente"},{"id":3,"estado":"correspondente"}]}]}
{"fase":"groq-12","tamanho":12,"formato":"padrao","provedor":"Groq","modelo":"openai/gpt-oss-20b","cenario":"PorLacuna","lacuna":2,"http":200,"fim":"stop","bytes":1762,"entrada":491,"saida":236,"total":727,"ms":639,"classificacoes":[{"id":2,"erros":[{"id":1,"estado":"correspondente"},{"id":2,"estado":"inconclusivo"},{"id":3,"estado":"inconclusivo"}]}]}
{"fase":"groq-12","tamanho":12,"formato":"padrao","provedor":"Groq","modelo":"openai/gpt-oss-20b","cenario":"PorLacuna","lacuna":3,"http":200,"fim":"stop","bytes":1760,"entrada":489,"saida":166,"total":655,"ms":698,"classificacoes":[{"id":3,"erros":[{"id":1,"estado":"naoCorrespondente"}]}]}
{"fase":"groq-12","tamanho":12,"formato":"padrao","provedor":"Groq","modelo":"openai/gpt-oss-20b","cenario":"PorLacuna","lacuna":4,"http":200,"fim":"stop","bytes":1758,"entrada":489,"saida":112,"total":601,"ms":458,"classificacoes":[{"id":4,"erros":[{"id":2,"estado":"correspondente"}]}]}
{"fase":"groq-12","tamanho":12,"formato":"padrao","provedor":"Groq","modelo":"openai/gpt-oss-20b","cenario":"PorLacuna","lacuna":5,"http":200,"fim":"stop","bytes":1766,"entrada":491,"saida":153,"total":644,"ms":621,"classificacoes":[{"id":5,"erros":[{"id":1,"estado":"naoCorrespondente"},{"id":2,"estado":"naoCorrespondente"},{"id":3,"estado":"naoCorrespondente"}]}]}
{"fase":"groq-12","tamanho":12,"formato":"padrao","provedor":"Groq","modelo":"openai/gpt-oss-20b","cenario":"PorLacuna","lacuna":6,"http":200,"fim":"stop","bytes":1759,"entrada":489,"saida":119,"total":608,"ms":657,"classificacoes":[{"id":6,"erros":[{"id":3,"estado":"correspondente"}]}]}
{"fase":"groq-12","tamanho":12,"formato":"padrao","provedor":"Groq","modelo":"openai/gpt-oss-20b","cenario":"PorLacuna","lacuna":7,"http":200,"fim":"stop","bytes":1760,"entrada":489,"saida":187,"total":676,"ms":594,"classificacoes":[{"id":7,"erros":[{"id":1,"estado":"inconclusivo"},{"id":2,"estado":"inconclusivo"},{"id":3,"estado":"inconclusivo"}]}]}
{"fase":"groq-12","tamanho":12,"formato":"padrao","provedor":"Groq","modelo":"openai/gpt-oss-20b","cenario":"PorLacuna","lacuna":8,"http":429,"codigo":"rate_limit_exceeded","retryAfter":"3"}
{"fase":"groq-12","tamanho":12,"formato":"padrao","provedor":"Groq","modelo":"openai/gpt-oss-20b","cenario":"PorLacuna","lacuna":8,"http":200,"fim":"stop","bytes":1759,"entrada":490,"saida":266,"total":756,"ms":737,"classificacoes":[{"erros":[{"estado":"correspondente","id":1},{"estado":"inconclusivo","id":2},{"estado":"inconclusivo","id":3}],"id":8}]}
{"fase":"groq-12","tamanho":12,"formato":"padrao","provedor":"Groq","modelo":"openai/gpt-oss-20b","cenario":"PorLacuna","lacuna":9,"http":200,"fim":"stop","bytes":1759,"entrada":488,"saida":184,"total":672,"ms":536,"classificacoes":[{"erros":[{"estado":"inconclusivo","id":1},{"estado":"inconclusivo","id":2},{"estado":"inconclusivo","id":3}],"id":9}]}
{"fase":"groq-12","tamanho":12,"formato":"padrao","provedor":"Groq","modelo":"openai/gpt-oss-20b","cenario":"PorLacuna","lacuna":10,"http":200,"fim":"stop","bytes":1762,"entrada":489,"saida":102,"total":591,"ms":408,"classificacoes":[{"erros":[],"id":10}]}
{"fase":"groq-12","tamanho":12,"formato":"padrao","provedor":"Groq","modelo":"openai/gpt-oss-20b","cenario":"PorLacuna","lacuna":11,"http":200,"fim":"stop","bytes":1755,"entrada":486,"saida":143,"total":629,"ms":641,"classificacoes":[{"erros":[{"estado":"inconclusivo","id":1},{"estado":"inconclusivo","id":2},{"estado":"inconclusivo","id":3}],"id":11}]}
{"fase":"groq-12","tamanho":12,"formato":"padrao","provedor":"Groq","modelo":"openai/gpt-oss-20b","cenario":"PorLacuna","lacuna":12,"http":200,"fim":"stop","bytes":1758,"entrada":488,"saida":362,"total":850,"ms":699,"classificacoes":[{"erros":[{"estado":"inconclusivo","id":1},{"estado":"inconclusivo","id":2},{"estado":"inconclusivo","id":3}],"id":12}]}
{"fase":"groq-24","tamanho":24,"formato":"padrao","provedor":"Groq","modelo":"openai/gpt-oss-20b","cenario":"LoteComTexto","lacuna":null,"http":400,"codigo":"json_validate_failed","retryAfter":""}
{"fase":"groq-24","tamanho":24,"formato":"padrao","provedor":"Groq","modelo":"openai/gpt-oss-20b","cenario":"LoteSemTexto","lacuna":null,"http":200,"fim":"stop","bytes":3103,"entrada":821,"saida":1015,"total":1836,"ms":1514,"classificacoes":[{"erros":[],"id":1},{"erros":[{"estado":"correspondente","id":1}],"id":2},{"erros":[{"estado":"correspondente","id":1}],"id":3},{"erros":[{"estado":"correspondente","id":2}],"id":4},{"erros":[],"id":5},{"erros":[{"estado":"correspondente","id":3}],"id":6},{"erros":[],"id":7},{"erros":[{"estado":"correspondente","id":1}],"id":8},{"erros":[],"id":9},{"erros":[],"id":10},{"erros":[],"id":11},{"erros":[],"id":12},{"erros":[],"id":13},{"erros":[{"estado":"correspondente","id":1}],"id":14},{"erros":[],"id":15},{"erros":[{"estado":"correspondente","id":1}],"id":16},{"erros":[],"id":17},{"erros":[],"id":18},{"erros":[{"estado":"correspondente","id":1}],"id":19},{"erros":[],"id":20},{"erros":[{"estado":"correspondente","id":1}],"id":21},{"erros":[],"id":22},{"erros":[],"id":23},{"erros":[],"id":24}]}
{"fase":"groq-24","tamanho":24,"formato":"padrao","provedor":"Groq","modelo":"openai/gpt-oss-20b","cenario":"LoteComTexto","lacuna":null,"http":400,"codigo":"json_validate_failed","retryAfter":""}
{"fase":"groq-24-compacto","tamanho":24,"formato":"compacto","provedor":"Groq","modelo":"openai/gpt-oss-20b","cenario":"LoteComTexto","lacuna":null,"http":400,"codigo":"json_validate_failed","retryAfter":""}
{"fase":"outros-padrao","tamanho":12,"formato":"padrao","provedor":"Cohere","modelo":"command-r7b-12-2024","modeloEfetivo":null,"cenario":"LoteComTexto","lacuna":null,"http":200,"fim":"COMPLETE","bytes":2494,"entrada":1179,"saida":371,"total":1550,"cobrados":{"input_tokens":487,"output_tokens":368},"ms":5965,"classificacoes":[{"id":1,"erros":[{"id":1,"estado":"inconclusivo"}]},{"id":2,"erros":[{"id":2,"estado":"inconclusivo"}]},{"id":3,"erros":[{"id":3,"estado":"inconclusivo"}]},{"id":4,"erros":[{"id":2,"estado":"correspondente"}]},{"id":5,"erros":[{"id":1,"estado":"inconclusivo"}]},{"id":6,"erros":[{"id":3,"estado":"correspondente"}]},{"id":7,"erros":[{"id":1,"estado":"inconclusivo"}]},{"id":8,"erros":[{"id":1,"estado":"inconclusivo"}]},{"id":9,"erros":[{"id":1,"estado":"inconclusivo"}]},{"id":10,"erros":[{"id":1,"estado":"inconclusivo"}]},{"id":11,"erros":[{"id":1,"estado":"inconclusivo"}]},{"id":12,"erros":[{"id":1,"estado":"inconclusivo"}]}]}
{"fase":"outros-padrao","tamanho":24,"formato":"padrao","provedor":"Cohere","modelo":"command-r7b-12-2024","modeloEfetivo":null,"cenario":"LoteComTexto","lacuna":null,"http":200,"fim":"COMPLETE","bytes":3483,"entrada":1480,"saida":1036,"total":2516,"cobrados":{"input_tokens":788,"output_tokens":1033},"ms":15811,"classificacoes":[{"id":1,"erros":[{"id":1,"estado":"inconclusivo"}]},{"id":2,"erros":[{"id":2,"estado":"inconclusivo"}]},{"id":3,"erros":[{"id":3,"estado":"inconclusivo"}]},{"id":4,"erros":[{"id":2,"estado":"inconclusivo"}]},{"id":5,"erros":[{"id":5,"estado":"inconclusivo"}]},{"id":6,"erros":[{"id":3,"estado":"inconclusivo"}]},{"id":7,"erros":[{"id":7,"estado":"inconclusivo"}]},{"id":8,"erros":[{"id":8,"estado":"inconclusivo"}]},{"id":9,"erros":[{"id":9,"estado":"inconclusivo"}]},{"id":10,"erros":[{"id":10,"estado":"inconclusivo"}]},{"id":11,"erros":[{"id":11,"estado":"inconclusivo"}]},{"id":12,"erros":[{"id":12,"estado":"inconclusivo"}]},{"id":13,"erros":[{"id":13,"estado":"inconclusivo"}]},{"id":14,"erros":[{"id":14,"estado":"inconclusivo"}]},{"id":15,"erros":[{"id":15,"estado":"inconclusivo"}]},{"id":16,"erros":[{"id":16,"estado":"inconclusivo"}]},{"id":17,"erros":[{"id":2,"estado":"inconclusivo"}]},{"id":18,"erros":[{"id":2,"estado":"inconclusivo"}]},{"id":19,"erros":[{"id":5,"estado":"inconclusivo"}]},{"id":20,"erros":[{"id":1,"estado":"inconclusivo"}]},{"id":21,"erros":[{"id":1,"estado":"inconclusivo"}]},{"id":22,"erros":[{"id":1,"estado":"inconclusivo"}]},{"id":23,"erros":[{"id":2,"estado":"inconclusivo"}]},{"id":24,"erros":[{"id":11,"estado":"inconclusivo"}]}]}
{"fase":"outros-padrao","tamanho":12,"formato":"padrao","provedor":"OpenRouter","modelo":"openrouter/free","modeloEfetivo":"liquid/lfm-3-1.7b:free","cenario":"LoteComTexto","lacuna":null,"http":200,"fim":"length","bytes":2618,"entrada":535,"saida":2048,"total":2583,"cobrados":null,"ms":15363,"classificacoes":null}
{"fase":"outros-padrao","tamanho":24,"formato":"padrao","provedor":"OpenRouter","modelo":"openrouter/free","modeloEfetivo":"nvidia/nemotron-3.5-super-vl:free","cenario":"LoteComTexto","lacuna":null,"http":200,"fim":"length","bytes":3607,"entrada":861,"saida":2048,"total":2909,"cobrados":null,"ms":15789,"classificacoes":null}
{"fase":"outros-padrao","tamanho":12,"formato":"padrao","provedor":"GonkaBroker","modelo":"deepseek-ai/DeepSeek-V4-Flash-0731","modeloEfetivo":"deepseek-ai/DeepSeek-V4-Flash-0731","cenario":"LoteComTexto","lacuna":null,"http":200,"fim":"stop","bytes":2574,"entrada":547,"saida":525,"total":1072,"cobrados":null,"ms":6141,"classificacoes":[{"id":1,"erros":[{"id":1,"estado":"inconclusivo"}]},{"id":2,"erros":[{"id":1,"estado":"inconclusivo"}]},{"id":3,"erros":[{"id":1,"estado":"inconclusivo"}]},{"id":4,"erros":[{"id":2,"estado":"inconclusivo"}]},{"id":5,"erros":[{"id":1,"estado":"inconclusivo"}]},{"id":6,"erros":[{"id":3,"estado":"inconclusivo"}]},{"id":7,"erros":[{"id":1,"estado":"inconclusivo"}]},{"id":8,"erros":[{"id":1,"estado":"inconclusivo"}]},{"id":9,"erros":[{"id":1,"estado":"inconclusivo"}]},{"id":10,"erros":[{"id":1,"estado":"inconclusivo"}]},{"id":11,"erros":[{"id":1,"estado":"inconclusivo"}]},{"id":12,"erros":[{"id":2,"estado":"inconclusivo"}]}]}
{"fase":"outros-padrao","tamanho":24,"formato":"padrao","provedor":"GonkaBroker","modelo":"deepseek-ai/DeepSeek-V4-Flash-0731","cenario":"LoteComTexto","lacuna":null,"http":429,"codigo":"rate_limit_exceeded","motivo":"limite","retryAfter":"5"}
{"fase":"gonka-reteste","tamanho":24,"formato":"padrao","provedor":"GonkaBroker","modelo":"deepseek-ai/DeepSeek-V4-Flash-0731","modeloEfetivo":"deepseek-ai/DeepSeek-V4-Flash-0731","cenario":"LoteComTexto","lacuna":null,"http":200,"fim":"stop","bytes":3563,"entrada":854,"saida":796,"total":1650,"cobrados":null,"ms":20031,"classificacoes":[{"id":1,"erros":[]},{"id":2,"erros":[{"id":1,"estado":"inconclusivo"}]},{"id":3,"erros":[{"id":1,"estado":"inconclusivo"}]},{"id":4,"erros":[{"id":2,"estado":"inconclusivo"}]},{"id":5,"erros":[]},{"id":6,"erros":[{"id":3,"estado":"inconclusivo"}]},{"id":7,"erros":[{"id":1,"estado":"inconclusivo"}]},{"id":8,"erros":[{"id":1,"estado":"inconclusivo"}]},{"id":9,"erros":[{"id":1,"estado":"inconclusivo"}]},{"id":10,"erros":[]},{"id":11,"erros":[]},{"id":12,"erros":[{"id":2,"estado":"inconclusivo"}]},{"id":13,"erros":[{"id":1,"estado":"inconclusivo"}]},{"id":14,"erros":[{"id":1,"estado":"inconclusivo"}]},{"id":15,"erros":[]},{"id":16,"erros":[{"id":1,"estado":"inconclusivo"}]},{"id":17,"erros":[{"id":3,"estado":"inconclusivo"}]},{"id":18,"erros":[{"id":2,"estado":"inconclusivo"}]},{"id":19,"erros":[{"id":1,"estado":"inconclusivo"}]},{"id":20,"erros":[]},{"id":21,"erros":[{"id":1,"estado":"inconclusivo"}]},{"id":22,"erros":[{"id":0,"estado":"inconclusivo"}]},{"id":23,"erros":[{"id":2,"estado":"inconclusivo"}]},{"id":24,"erros":[]}]}
{"fase":"outros-compacto","tamanho":24,"formato":"compacto","provedor":"Cohere","modelo":"command-r7b-12-2024","modeloEfetivo":null,"cenario":"LoteComTexto","lacuna":null,"http":200,"fim":"COMPLETE","bytes":3481,"entrada":1487,"saida":653,"total":2140,"cobrados":{"input_tokens":802,"output_tokens":650},"ms":10143,"classificacoes":[{"id":1,"erros":[{"id":1,"estado":"correspondente"}]},{"id":2,"erros":[{"id":2,"estado":"correspondente"}]},{"id":3,"erros":[{"id":3,"estado":"correspondente"}]},{"id":4,"erros":[{"id":2,"estado":"correspondente"}]},{"id":5,"erros":[{"id":1,"estado":"correspondente"}]},{"id":6,"erros":[{"id":3,"estado":"correspondente"}]},{"id":7,"erros":[{"id":1,"estado":"correspondente"}]},{"id":8,"erros":[{"id":1,"estado":"correspondente"}]},{"id":9,"erros":[{"id":2,"estado":"correspondente"}]},{"id":10,"erros":[{"id":1,"estado":"correspondente"}]},{"id":11,"erros":[{"id":2,"estado":"inconclusivo"}]},{"id":12,"erros":[{"id":2,"estado":"correspondente"}]},{"id":13,"erros":[{"id":1,"estado":"inconclusivo"}]},{"id":14,"erros":[{"id":2,"estado":"correspondente"}]},{"id":15,"erros":[{"id":1,"estado":"correspondente"}]},{"id":16,"erros":[{"id":2,"estado":"correspondente"}]},{"id":17,"erros":[{"id":2,"estado":"inconclusivo"}]},{"id":18,"erros":[{"id":2,"estado":"inconclusivo"}]},{"id":19,"erros":[{"id":1,"estado":"correspondente"}]},{"id":20,"erros":[{"id":1,"estado":"correspondente"}]},{"id":21,"erros":[{"id":2,"estado":"correspondente"}]},{"id":22,"erros":[{"id":1,"estado":"inconclusivo"}]},{"id":23,"erros":[{"id":2,"estado":"inconclusivo"}]},{"id":24,"erros":[{"id":1,"estado":"correspondente"}]}]}
{"fase":"outros-compacto","tamanho":24,"formato":"compacto","provedor":"OpenRouter","modelo":"openrouter/free","modeloEfetivo":"liquid/lfm-3-1.7b:free","cenario":"LoteComTexto","lacuna":null,"http":200,"fim":"length","bytes":3605,"entrada":845,"saida":2048,"total":2893,"cobrados":null,"ms":8579,"classificacoes":null}
{"fase":"outros-compacto","tamanho":24,"formato":"compacto","provedor":"GonkaBroker","modelo":"deepseek-ai/DeepSeek-V4-Flash-0731","cenario":"LoteComTexto","lacuna":null,"falha":"SocketException"}
```

## Referências técnicas

- [Groq API, objeto de uso](https://console.groq.com/docs/api-reference)
- [Cohere Chat v2, uso de tokens e unidades cobradas](https://docs.cohere.com/v2/reference/chat)
- [OpenRouter Chat Completions](https://openrouter.ai/docs/api/api-reference/chat/create-a-chat-completion)
- [GonkaBroker, compatibilidade com Chat Completions](https://docs.gonkabroker.com/reference/api-compatibility/)
