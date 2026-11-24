# Avaliação do modo guest do Free-LLM Router na correção do Ditado

Coleta em 21/11/2026, às 22h31, horário de Brasília. Experimento de bancada com dados sintéticos da PER-65.

## Resultado

O Free-LLM Router devolveu **HTTP 503 `no_providers` nas nove chamadas** com o payload contextual v4 do Ditado. Nenhum pedido chegou a um modelo. A proporção de chamadas que produziram uma classificação utilizável foi **0/9 (0%)**. A **precisão das marcações** e a **taxa de lacunas corrigidas exatamente** ficaram **sem estimativa**, pois nenhuma resposta pôde ser confrontada com o gabarito.

## Método

O ensaio usou o Free-LLM Router `1.0.1`, revisão `08d536e91f3c19c77ddbf5723c7e582efa5ccabe`, com Node.js `24.11.1` no Windows. O roteador foi iniciado em `127.0.0.1:45465`, com diretório de configuração novo, cache desativado, sem sondagem inicial e sem chaves de provedores. A autenticação HTTP usou somente uma chave gerada para o roteador local. [Documentação do Free-LLM Router na revisão testada](https://github.com/nejib1/Free-LLM/blob/08d536e91f3c19c77ddbf5723c7e582efa5ccabe/router/README.md)

Foram reutilizados três corpos HTTP registrados pelo cliente do Ditado para a versão `per18-v4-contextual`. Cada corpo contém as instruções de sistema, o texto escrito agrupado em blocos, as lacunas com seus IDs e gabaritos, as três definições selecionadas e o schema estrito de saída. O único campo alterado foi `model`, de `openai/gpt-oss-120b` para `auto`, para que o roteador pudesse escolher uma rota guest. O teto solicitado permaneceu em 4.096 tokens. O cliente do ensaio aguardou até 60 s por resposta. Cada amostra foi enviada três vezes em sequência, com uma chamada por ditado e por rodada. [Corpo de 8 lacunas](per65-provedores-v4-2026-11-21.md#generalização-para-três-definições-docentes-inéditas) · [Corpo de 10 lacunas](per65-provedores-v4-2026-11-21.md#groq--10-lacunas--teto-de-4096-tokens) · [Corpo de 24 lacunas](per65-provedores-v4-2026-11-21.md#groq--24-lacunas--teto-de-4096-tokens)

| Amostra | Pares esperados como correspondentes (lacuna:candidato) | Inconclusivo esperado | Controles negativos de concordância |
|---|---|---|---|
| 8 lacunas | `1:17`, `7:17`, `3:29`, `5:43` | `8:17` | `2:17` |
| 10 lacunas | `4:1`, `8:1`, `9:2`, `10:3` | Nenhum | `2:1`, `6:1` |
| 24 lacunas | `2:1`, `3:1`, `8:1`, `16:1`, `19:1`, `4:2`, `23:2`, `6:3`, `17:3` | Nenhum | `14:1` |

Os demais pares de lacuna e candidato esperam ausência de correspondência. Os controles incluem casos como “a criança chegou”, na lacuna 14, em que a forma escrita diverge do gabarito, mas preserva a concordância entre sujeito e verbo. [Gabaritos da PER-65](per65-indice-ensaios.md#como-confrontar-a-entrada-e-a-saída)

As métricas previstas distinguem a produção de uma saída utilizável da qualidade da classificação. Uma resposta entra na avaliação pedagógica quando termina normalmente e o JSON contém todos os IDs avaliados uma vez, um grupo por candidato, somente pares autorizados e valores compatíveis com o contrato. A precisão corresponde a `VP / (VP + FP)`, em que VP são pares marcados corretamente e FP são pares marcados indevidamente. A revocação corresponde a `VP / (VP + FN)`, em que FN são correspondências esperadas que ficaram sem marcação positiva. A taxa por lacuna exige que todos os estados da lacuna coincidam com o gabarito, incluindo o inconclusivo previsto na amostra de oito lacunas. Requisições com erro entram no denominador da disponibilidade observada. [Validador do Ditado](../../../Ditado.Aplicacao/Llm/LlmAnalysisProcessor.cs)

## Observações por chamada

| Lacunas | Rodada 1 | Rodada 2 | Rodada 3 | Saídas utilizáveis | Precisão e acerto por lacuna |
|---:|---|---|---|---:|---|
| 8 | 503 · 53 ms | 503 · 28 ms | 503 · 9 ms | 0/3 | Sem estimativa |
| 10 | 503 · 28 ms | 503 · 32 ms | 503 · 44 ms | 0/3 | Sem estimativa |
| 24 | 503 · 40 ms | 503 · 14 ms | 503 · 30 ms | 0/3 | Sem estimativa |

Em todas as chamadas, o código do erro foi `no_providers` e os cabeçalhos de provedor e modelo estavam ausentes. Os tempos medem a devolução do erro em localhost. Sem saída de modelo, também ficaram sem avaliação a revocação, os falsos positivos nos controles, as omissões e os inconclusivos.

## Causa observada

O roteador carregou o catálogo ao vivo de `free-llm.com` sem erro. O catálogo reportou 88 provedores e 438 modelos no total. O status local registrou `providers_with_keys: 0`, `targets: 0` e `guest_mode: false`. No código, `guest_mode` só aparece como verdadeiro quando existe ao menos uma rota e todas as rotas usam acesso anônimo. Nesse ensaio, a tentativa de operar sem chaves não encontrou nenhuma rota elegível. [Implementação do status](https://github.com/nejib1/Free-LLM/blob/08d536e91f3c19c77ddbf5723c7e582efa5ccabe/router/src/server.js#L132-L144) · [Seleção de rotas e HTTP 503](https://github.com/nejib1/Free-LLM/blob/08d536e91f3c19c77ddbf5723c7e582efa5ccabe/router/src/router.js#L232-L249)

O catálogo ao vivo e o snapshot da revisão testada tinham **zero provedores com `anonymous_key` não nulo**. O roteador usa esse campo para habilitar a rota anônima de um provedor quando opera sem chaves. O snapshot apresentado na tabela foi inspecionado separadamente. A execução HTTP usou o catálogo ao vivo. [Leitura do catálogo](https://github.com/nejib1/Free-LLM/blob/08d536e91f3c19c77ddbf5723c7e582efa5ccabe/router/src/catalog.js#L93-L109) · [Regra para acesso anônimo](https://github.com/nejib1/Free-LLM/blob/08d536e91f3c19c77ddbf5723c7e582efa5ccabe/router/src/router.js#L73-L95) · [Snapshot da revisão](https://github.com/nejib1/Free-LLM/blob/08d536e91f3c19c77ddbf5723c7e582efa5ccabe/router/catalog.snapshot.json)

| Catálogo | Provedores | Entradas com acesso anônimo | SHA-256 do arquivo inspecionado |
|---|---:|---:|---|
| Ao vivo, armazenado pelo roteador | 88 | 0 | `1ab743d7726f46c13a386849389b00b61212c43cb473af63e50d37a074dca284` |
| Snapshot do repositório | 88 | 0 | `6dfac7519ace48f943b027dc1e8b90a2b048bd105e19c666c970236a88b1c1e9` |

## Interpretação para o TCC

Na configuração e no horário observados, o Free-LLM Router sem credenciais de provedores não conseguiu executar a classificação de nenhuma das três amostras. O resultado caracteriza indisponibilidade para esse cenário de acesso. Uma comparação de qualidade com os resultados do Groq, Cohere ou OpenRouter exigiria saídas válidas do modo guest sobre as mesmas amostras. Como o catálogo é atualizado, a evidência se restringe à revisão e aos catálogos inspecionados em 21/11/2026. O ensaio cobre o componente de classificação por LLM. A nota objetiva, as regras programadas e o fluxo da API com persistência ficaram fora da medição. [Comparativo histórico da PER-65](per65-provedores-v4-2026-11-21.md)

## Reprodução

Na raiz do backend, com Node.js e o [script de bancada](../../../work/per65-free-llm-guest.mjs), os comandos abaixo reproduzem as três rodadas. O script confere os corpos e o gabarito antes de iniciar o roteador e registra status, provedor, modelo, tempo e resultado em JSON Lines. A pasta `work/` contém o material local do ensaio. O catálogo consultado na reprodução pode mudar.

```powershell
git clone https://github.com/nejib1/Free-LLM.git work/free-llm-guest-router
git -C work/free-llm-guest-router checkout --detach 08d536e91f3c19c77ddbf5723c7e582efa5ccabe
node work/per65-free-llm-guest.mjs --check
node work/per65-free-llm-guest.mjs 3 | Tee-Object -FilePath work/per65-free-llm-guest-resultados.jsonl
```
