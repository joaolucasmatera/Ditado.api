# PER-65 · Provedores e trilha da classificação LLM

Implementação de 18/11/2026 na branch `codex/per-65-configurar-e-validar-os-quatro`, baseada na PER-64. A API conserva a nota, o gabarito e os achados por código. A fila assíncrona registra provedor, modelo, versão das instruções, desfecho, motivo sanitizado, status HTTP e horários em `LlmTentativasProvedor`. A migration `20261118225924_RegistrarTentativasLlmPer65` cria a tabela sem preencher tentativas históricas. Uma chamada interrompida permanece na trilha e é marcada como interrompida na retomada da tarefa.

O cliente avança na cadeia por timeout, rede, indisponibilidade ou limite confirmado pelo fornecedor. O HTTP 403 do OpenRouter só avança se o metadado indicar orçamento esgotado. Erros de autenticação, contrato ou saída inválida param a cadeia e sinalizam revisão. Um JSON válido sem correspondência encerra a análise. A trilha usa códigos internos e nunca grava corpo de erro, cabeçalhos ou credenciais.

## Configuração por ambiente

Em 21/11/2026, a cadeia passou a considerar Groq, Cohere e OpenRouter por decisão do usuário. Na configuração Docker, ordene os três provedores nos índices 0, 1 e 2 de `LLM_CHAIN_*` e injete as respectivas credenciais. [Decisão e contrato atual](epicos/cor-01-classificacao-hibrida.md)

Use `Llm__Enabled=true` para habilitar a fila real. Defina `Llm__Chain__0__Provider` e `Llm__Chain__0__Model` para a primeira posição e repita para os índices 1 e 2 na ordem desejada. Use cada provedor uma vez e selecione o identificador exato do modelo na conta correspondente. OpenRouter requer um slug `fornecedor/modelo`. A API continua operando com correção objetiva e a fila fica desabilitada quando faltam modelos ou a cadeia é inválida.

| Provedor | Base HTTPS | Variável de credencial |
| --- | --- | --- |
| Groq | `https://api.groq.com/openai/v1` | `GROQ_API_KEY` |
| Cohere | `https://api.cohere.com/v2` | `COHERE_API_KEY` |
| OpenRouter | `https://openrouter.ai/api/v1` | `OPENROUTER_API_KEY` |

Provisione as credenciais por ambiente ou gerenciador de segredos. Use credenciais novas quando uma anterior tiver sido exposta. Os destinos HTTPS são definidos por provedor no código. O cliente usa `/chat` com schema nativo do Cohere e `/chat/completions` com JSON Schema nos demais. O cliente OpenRouter exige suporte aos parâmetros e desabilita fallback interno. Confirme o suporte do modelo específico com a comparação sintética antes de habilitar a fila com dados escolares.

| Variável | Padrão | Faixa |
| --- | ---: | ---: |
| `Llm__TimeoutSeconds` por chamada | 15 | 1–60 |
| `Llm__MaxInputCharacters` do JSON com as lacunas e os candidatos | 8000 | 128–64000 |
| `Llm__MaxOutputTokens` por chamada | 768 | 16–8192 |

Cada tentativa percorre até três provedores, com uma chamada por provedor acionado. A resposta HTTP tem teto de 100.000 bytes. Configure o orçamento acumulado e a taxa de consumo na conta do fornecedor quando disponíveis. O limite local de entrada encerra a tarefa em revisão sem descartar candidatos nem enviar o texto.

## Comparação separada da operação

Com os três modelos configurados e as credenciais provisionadas, execute no backend usando o SDK .NET 9.

```powershell
$env:DITADO_LLM_COMPARAR_SINTETICO = '1'
dotnet test Ditado.Testes/Ditado.Testes.csproj --filter Categoria=LlmReal --logger 'console;verbosity=detailed'
Remove-Item Env:DITADO_LLM_COMPARAR_SINTETICO
```

O teste usa sete casos sintéticos rotulados com acentuação, segmentação, acerto, ausência e casos sem correspondência. Cada modelo recebe chamadas diretas, sem fila ou fallback. O teste relata a proporção de saídas válidas, correspondência exata e mediana da latência. A amostra serve para comparação inicial. [Critério pedagógico e evidências](documentacao/evidencias/per65-indice-ensaios.md)

Referências de protocolo. [Groq Structured Outputs](https://console.groq.com/docs/structured-outputs) · [Cohere Chat v2](https://docs.cohere.com/v2/reference/chat) · [Gonka Broker API Compatibility](https://docs.gonkabroker.com/reference/api-compatibility/) · [OpenRouter Errors and Debugging](https://openrouter.ai/docs/api_reference/errors-and-debugging).

## Verificação local

O build oficial da API foi executado por `powershell -ExecutionPolicy Bypass -File .\scripts\build-api.ps1` com saída isolada. O frontend foi compilado com `npm.cmd run build`. O recorte de testes usa handlers HTTP sintéticos para verificar os contratos, a ordem, 429, timeout, rede, indisponibilidade dos quatro serviços, autenticação, saída inválida e ausência de correspondência. O teste de comparação real fica ignorado por padrão.

Para a demonstração local, use a [fixture da PER-64](relatorio-ciclo-per64-2026-11-18.md) com `DITADO_LLM_DEMO_PORT=5065`, schema novo `ditado_per65_<sufixo-unico>` e frontend em 5195. Ajuste as URLs em `VITE_API_BASE_URL`, `E2E_API_URL` e `E2E_BASE_URL`. A fixture usa o ambiente `Testing` com provedor simulado e verifica a retomada após reinício. Os arquivos de evidência ficam em `work/logs/`.

Em `ditado_per65_gate1811`, o build da API terminou com zero erros e dois avisos existentes. O build do frontend passou. O recorte contou 60 testes aprovados e um ignorado, a comparação real. O Playwright passou com nota 33,33 após recarga e acesso de outra escola negado. A migration consta no histórico do MySQL. A trilha registrou uma chamada interrompida, uma conclusão com correspondência e duas sem correspondência. A corrida simulou o vencimento da reserva para testar a retomada. A fixture aplicou as três colunas legadas da PER-64 somente no schema de teste.

Evidências locais em `work/logs/`: `per65-build.log`, `per65-pertinentes.trx`, `per65-mysql.log`, `per65-playwright.log` e `per65-trilha.log`. O contrato Swagger permanece o mesmo. Restam o provisionamento por ambiente, a seleção dos quatro modelos e a execução da comparação real. A validação independente fica pendente.

## Operação Docker aprovada

Para reduzir o trabalho no servidor da faculdade, a imagem não contém credenciais. O técnico deverá copiar `.env.docker.example` para um arquivo privado no servidor, preencher as chaves novas e executar `docker compose --env-file .env.production up -d`. O arquivo deve permanecer fora do Git, com acesso restrito, e não deve ser enviado junto com a imagem. A pipeline futura poderá substituir esse arquivo por secrets injetados no deploy.

## Experimento de envio em lote — 19/11/2026

Medição com ditados sintéticos e três definições de erro, usando Groq `openai/gpt-oss-20b`, temperatura 0, `reasoning_effort=low` e teto de 2048 tokens de saída em todos os cenários. Esses parâmetros são do experimento. A primeira variante reproduz a estrutura por lacuna do cliente. As variantes em lote enviam as definições uma vez e, quando indicado, incluem o texto do ditado com marcadores nas lacunas. Os números vêm de `usage` nas respostas aceitas pelo provedor. A coluna de correspondências exatas compara o conjunto de erros marcados por lacuna com o gabarito sintético. Estados inconclusivos são contabilizados à parte.

| Cenário com 12 lacunas | Chamadas aceitas | Entrada | Saída | Total | Correspondências exatas |
|---|---:|---:|---:|---:|---:|
| Por lacuna | 12 | 5.868 | 2.183 | 8.051 | 10/12 |
| Lote sem texto | 1 | 625 | 843 | 1.468 | 12/12 |
| Lote com texto | 1 | 710 | 615 | 1.325 | 11/12 |

O lote com texto reduziu a entrada em 87,9% e o total em 83,5% em relação às chamadas individuais. O texto acrescentou 85 tokens de entrada ao lote. A sequência individual recebeu um HTTP 429 antes de concluir as 12 lacunas e apresentou seis lacunas com pelo menos um estado inconclusivo. O lote com texto atribuiu concordância verbal à lacuna `juntos → junto`, que não continha verbo.

O cenário ampliado para 24 lacunas incluiu duas trocas idênticas de `chegaram` por `chegou` em frases diferentes. A forma digitada concordava com `a criança` e destoava de `os professores`. O lote sem texto respondeu em uma chamada com 821 tokens de entrada, 1.015 de saída e 20 de 24 conjuntos exatos. Marcou concordância verbal nas duas frases. O lote com texto retornou HTTP 400 `json_validate_failed` em duas tentativas com a saída por lacuna e em uma com saída compacta. As chamadas rejeitadas não retornaram `usage`.

A medição mostra economia para esta amostra de 12 lacunas. O fluxo por lacuna serviu de linha de base nesse ciclo. No primeiro ensaio de 24 lacunas, os modelos apresentaram falhas de estrutura, cobertura ou classificação. O diagnóstico local reutilizável está em `work/per65-token-benchmark-20261119.ps1`. Os insumos, os testes iniciais do Cohere, do OpenRouter e do GonkaBroker e os 29 registros sanitizados estão no [caderno de evidências do TCC](documentacao/evidencias/per65-benchmark-llm-2026-11-19.md).

No [ensaio de seguimento do Groq](documentacao/evidencias/per65-groq-ab-2026-11-19.md), o lote de 24 lacunas teve duas saídas válidas em três tentativas com schema estrito e nenhuma válida em três tentativas com modo JSON. Duas chamadas adicionais foram limitadas. O corpo do HTTP 400 mostrou a chave `erros:` em vez de `erros` na última lacuna. Os dois retornos válidos confundiram o par de controle de concordância. A evidência contém os dois corpos de request e os oito corpos de resposta, com identificadores de conta removidos dos erros HTTP 429.

## Implementação contextual e validação

O índice de ensaios reúne, por rodada, a entrada, o gabarito esperado, o retorno recebido e a decisão resultante. [Índice de ensaios e gabaritos](documentacao/evidencias/per65-indice-ensaios.md)

Os ensaios acima usaram o fluxo por lacuna como referência histórica. A aplicação agora envia um lote contextual por tentativa e provedor acionado, com texto escrito por trecho e uma lista de candidatos. O QA confirmou a classificação híbrida na API e o ensaio do cliente confirmou o controle contextual do lote de 24 lacunas no Groq 120B. [Relatório do ciclo contextual](relatorio-ciclo-per65-contextual-2026-11-19.md) · [Capturas do cliente v4](documentacao/evidencias/per65-cliente-v4-2026-11-21.md)

### Perfil de referência dos ensaios

Use `openai/gpt-oss-120b` no Groq, `LLM_TIMEOUT_SECONDS=30` e `LLM_MAX_OUTPUT_TOKENS=4096` para reproduzir o perfil sintético. A repetição nesse perfil retornou 24/24 lacunas exatas no lote longo e 10/10 no inédito. Com teto de 6.144, uma repetição do lote longo retornou 23/24. A diferença entre execuções ilustra a variabilidade observada nesta amostra. Os limites do ambiente continuam configuráveis.

A comparação na mesma amostra separa conclusão HTTP, validade estrutural e acerto pedagógico. O rotador trata falhas técnicas, e a validação de estrutura impede a gravação de saídas incompletas. Um modelo pode retornar um JSON válido com falsos positivos. Os modelos secundários continuam em avaliação para uso com respostas reais. [Comparação dos provedores](documentacao/evidencias/per65-provedores-v4-2026-11-21.md)
