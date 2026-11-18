# PER-65 · Provedores e trilha da classificação LLM

Implementação de 18/11/2026 na branch `codex/per-65-configurar-e-validar-os-quatro`, baseada na PER-64. A API conserva a nota, o gabarito e os achados por código. A fila assíncrona registra provedor, modelo, versão das instruções, desfecho, motivo sanitizado, status HTTP e horários em `LlmTentativasProvedor`. A migration `20261118225924_RegistrarTentativasLlmPer65` cria a tabela sem preencher tentativas históricas. Uma chamada interrompida permanece na trilha e é marcada como interrompida na retomada da tarefa.

O cliente avança na cadeia por timeout, rede, indisponibilidade ou limite confirmado pelo fornecedor. O HTTP 403 do OpenRouter só avança se o metadado indicar orçamento esgotado. Erros de autenticação, contrato ou saída inválida param a cadeia e sinalizam revisão. Um JSON válido sem correspondência encerra a análise. A trilha usa códigos internos e nunca grava corpo de erro, cabeçalhos ou credenciais.

## Configuração por ambiente

Use `Llm__Enabled=true` para habilitar a fila real. Defina `Llm__Chain__0__Provider` e `Llm__Chain__0__Model` para a primeira posição e repita para os índices 1 a 3 na ordem desejada. Use cada provedor uma vez e selecione o identificador exato do modelo na conta correspondente. OpenRouter requer um slug `fornecedor/modelo`. A API continua operando com correção objetiva e a fila fica desabilitada quando faltam modelos ou a cadeia é inválida.

| Provedor | Base HTTPS | Variável de credencial |
| --- | --- | --- |
| Groq | `https://api.groq.com/openai/v1` | `GROQ_API_KEY` |
| Cohere | `https://api.cohere.com/v2` | `COHERE_API_KEY` |
| GonkaBroker | `https://proxy.gonkabroker.com/v1` | `GONKA_BROKER_API_KEY` |
| OpenRouter | `https://openrouter.ai/api/v1` | `OPENROUTER_API_KEY` |

Provisione as credenciais por ambiente ou gerenciador de segredos. Use credenciais novas quando uma anterior tiver sido exposta. Os destinos HTTPS são definidos por provedor no código. O cliente usa `/chat` com schema nativo do Cohere e `/chat/completions` com JSON Schema nos demais. O cliente OpenRouter exige suporte aos parâmetros e desabilita fallback interno. Confirme o suporte do modelo específico com a comparação sintética antes de habilitar a fila com dados escolares.

| Variável | Padrão | Faixa |
| --- | ---: | ---: |
| `Llm__TimeoutSeconds` por chamada | 15 | 1–60 |
| `Llm__MaxInputCharacters` do JSON com a lacuna e os candidatos | 8000 | 128–64000 |
| `Llm__MaxOutputTokens` por chamada | 768 | 16–8192 |

Cada tarefa percorre até quatro provedores sem retry no mesmo provedor durante a execução. A resposta HTTP tem teto de 100.000 bytes. Configure o orçamento acumulado e a taxa de consumo na conta do fornecedor quando disponíveis. O limite local de entrada encerra a tarefa em revisão sem descartar candidatos nem enviar o texto.

## Comparação separada da operação

Com os quatro modelos configurados e as credenciais provisionadas, execute no backend usando o SDK .NET 9.

```powershell
$env:DITADO_LLM_COMPARAR_SINTETICO = '1'
dotnet test Ditado.Testes/Ditado.Testes.csproj --filter Categoria=LlmReal --logger 'console;verbosity=detailed'
Remove-Item Env:DITADO_LLM_COMPARAR_SINTETICO
```

O teste usa sete casos sintéticos rotulados com acentuação, segmentação, acerto, ausência e casos sem correspondência. Faz até 28 chamadas diretas, sem fila ou fallback. Relata por provedor e modelo a proporção de saídas válidas, correspondência exata e mediana da latência. O smoke exige formato válido em todas as chamadas. A amostra serve para comparação inicial. O critério pedagógico e o conjunto representativo devem ser definidos antes de ativar o processamento de respostas escolares.

Referências de protocolo. [Groq Structured Outputs](https://console.groq.com/docs/structured-outputs) · [Cohere Chat v2](https://docs.cohere.com/v2/reference/chat) · [Gonka Broker API Compatibility](https://docs.gonkabroker.com/reference/api-compatibility/) · [OpenRouter Errors and Debugging](https://openrouter.ai/docs/api_reference/errors-and-debugging).

## Verificação local

O build oficial da API foi executado por `powershell -ExecutionPolicy Bypass -File .\scripts\build-api.ps1` com saída isolada. O frontend foi compilado com `npm.cmd run build`. O recorte de testes usa handlers HTTP sintéticos para verificar os contratos, a ordem, 429, timeout, rede, indisponibilidade dos quatro serviços, autenticação, saída inválida e ausência de correspondência. O teste de comparação real fica ignorado por padrão.

Para a demonstração local, use a [fixture da PER-64](relatorio-ciclo-per64-2026-11-18.md) com `DITADO_LLM_DEMO_PORT=5065`, schema novo `ditado_per65_<sufixo-unico>` e frontend em 5195. Ajuste as URLs em `VITE_API_BASE_URL`, `E2E_API_URL` e `E2E_BASE_URL`. A fixture usa o ambiente `Testing` com provedor simulado e verifica a retomada após reinício. Os arquivos de evidência ficam em `work/logs/`.

Em `ditado_per65_gate1811`, o build da API terminou com zero erros e dois avisos existentes. O build do frontend passou. O recorte contou 60 testes aprovados e um ignorado, a comparação real. O Playwright passou com nota 33,33 após recarga e acesso de outra escola negado. A migration consta no histórico do MySQL. A trilha registrou uma chamada interrompida, uma conclusão com correspondência e duas sem correspondência. A corrida simulou o vencimento da reserva para testar a retomada. A fixture aplicou as três colunas legadas da PER-64 somente no schema de teste.

Evidências locais em `work/logs/`: `per65-build.log`, `per65-pertinentes.trx`, `per65-mysql.log`, `per65-playwright.log` e `per65-trilha.log`. O contrato Swagger permanece o mesmo. Restam o provisionamento por ambiente, a seleção dos quatro modelos e a execução da comparação real. A validação independente fica pendente.

## Operação Docker aprovada

Para reduzir o trabalho no servidor da faculdade, a imagem não contém credenciais. O técnico deverá copiar `.env.docker.example` para um arquivo privado no servidor, preencher as chaves novas e executar `docker compose --env-file .env.production up -d`. O arquivo deve permanecer fora do Git, com acesso restrito, e não deve ser enviado junto com a imagem. A pipeline futura poderá substituir esse arquivo por secrets injetados no deploy.
