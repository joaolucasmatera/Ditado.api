# PER-65 · classificação contextual por tentativa

## Cadeia de três provedores · decisão de 21/11/2026

O usuário aprovou a retirada do GonkaBroker pelos falsos positivos e timeouts observados. O commit `e4b45ba` mantém Groq, Cohere e OpenRouter nos índices 0 a 2 em ordem configurável. O Compose validou a configuração local com a credencial de cada um e sem a injeção da credencial antiga. Os ensaios anteriores dos quatro provedores continuam disponíveis. [Índice de ensaios](documentacao/evidencias/per65-indice-ensaios.md)

O build passou e a suíte pertinente do desenvolvedor registrou 69 testes aprovados. O recorte adicional da coordenação teve 37 testes aprovados. Os testes incluem recusa do GonkaBroker antes de enviar HTTP e rotação até o terceiro provedor. Na demonstração simulada, a aplicação preservou o resultado híbrido e recusou a consulta de outra escola.

O QA independente conferiu o código, a configuração renderizada do Compose e o TRX do mesmo commit. A nova execução do QA expirou ao conectar o VSTest ao TestHost. A credencial antiga permanece no arquivo privado local e o Compose a exclui do ambiente do container. [Registro na PER-65](https://linear.app/joaomoutinho/issue/PER-65)

A coordenação repetiu a suíte completa pertinente nos binários do commit `e4b45ba`. O TestHost conectou e 69/69 testes passaram, sem falhas ou ignorados. O resultado foi salvo em `work/logs/per65-tres-coordenacao.trx`.

Na demonstração complementar, a API isolada ativou a cadeia de três provedores e recebeu as cinco lacunas sintéticas da amostra de controle. Esperavam-se acentuação por código, concordância somente em “os professores chegou”, ausência de erro em “a criança chegou”, nota 20/100 e releitura idêntica. Foi registrada uma chamada Groq 120B, HTTP 200 e `ComCorrespondencia`, e o resultado da API correspondeu ao esperado. A captura é da API, com metadados da chamada na trilha.

```json
{"ditadoId":1,"tentativaId":1,"nota":20,"detalhes":[{"esperado":"café","escrito":"cafe","estado":"Concluida","achados":[{"origem":"Codigo","nome":"Acentuação"}]},{"esperado":"as crianças","escrito":"a criança","estado":"Concluida","achados":[]},{"esperado":"chegaram","escrito":"chegou","estado":"Concluida","achados":[]},{"esperado":"chegaram","escrito":"chegou","estado":"Concluida","achados":[{"origem":"LLM","nome":"Concordância entre sujeito e verbo escritos"}]},{"esperado":"tocou","escrito":"tocou","estado":"Concluida","achados":[]}]}
```

## Implementação

A fila agrupa as tarefas pendentes de uma tentativa e faz uma chamada por provedor elegível. O worker reconstrói a redação em ordem com as respostas fornecidas e o texto fixo do ditado. `textoEscrito` é contínuo e preserva os caracteres originais. O gabarito vai em `textoEsperado`, com identificadores das lacunas, acompanhado do mapa de respostas. Uma omissão tem valor nulo no mapa e fica vazia na redação.

A chamada inclui ID do catálogo, código, nome e descrição de cada definição selecionada sem executor. O prompt `per18-v3-contextual` pede evidência baseada no sujeito e no verbo efetivamente escritos. A resposta lista todas as lacunas avaliadas e agrupa por ID do erro, com as lacunas correspondentes e inconclusivas. Uma sugestão só é gravada após validar a cobertura de todo o lote, os identificadores, os estados e os limites da evidência.

O worker grava a conclusão de todas as tarefas do lote em uma operação e mantém uma trilha por chamada. A reserva vencida permite retomar uma tentativa interrompida. Uma interrupção pode repetir a chamada técnica. O isolamento e a reserva evitam duplicar sugestões persistidas. Tarefas concluídas no formato anterior preservam seu histórico e, quando pertinente, integram o contexto. A pontuação objetiva e a classificação por código seguem o caminho existente. A mudança usa o schema e os endpoints existentes.

## Arquivos

- `Ditado.Aplicacao/Llm/LlmAnalysisProcessor.cs`, `LlmContracts.cs` e `LlmOptions.cs`
- `Ditado.API/Llm/LlmProviderClient.cs` e `LlmSimulatedClient.cs`
- `Ditado.Testes/Services/LlmAnalysisProcessorTests.cs`, `LlmProviderClientTests.cs` e `LlmComparacaoModelosTests.cs`
- `scripts/demo-per64.ps1`

## Validação

- Build com `scripts/build-api.ps1`. Zero erros e dois avisos preexistentes.
- 65 testes pertinentes aprovados, incluindo concordância em contexto, controles inéditos, preservação de marcadores literais e do histórico, contrato HTTP nos quatro adaptadores e rejeição de saídas incompletas ou inválidas.
- Playwright, API na porta 5065 e schema MySQL isolado `ditado_per65_contextualnov19`. O aluno fez um ditado de três lacunas, viu a classificação **Acentuação · Código** e recarregou para ver também **Achado simulado PER-64 · LLM** na mesma lacuna. A nota 33,33 foi mantida, a lacuna omitida ficou vazia e a consulta de outra escola recebeu HTTP 404.
- Após interromper a chamada simulada e expirar sua reserva, o MySQL registrou uma chamada interrompida, uma chamada concluída para as três lacunas e três tarefas concluídas. Foram persistidos um achado objetivo, um correspondente LLM e duas lacunas sem correspondência LLM.
- Logs locais em `work/logs/per65-contextual-build.log`, `work/logs/per65-contextual.trx`, `work/logs/per65-contextual-playwright.log` e `work/logs/per65-contextual-trilha.log`.

## Reprodução pelo coordenador

O tipo `Ditado.Aplicacao.Llm.LlmProviderClient` fica em `Ditado.API.dll`. O contrato fica em `Ditado.Aplicacao.dll` no mesmo diretório. O build local está em `C:\Users\lucas\AppData\Local\Temp\ditado-per65-contextual-artifacts\bin\Ditado.API\debug`. Para o gate sintético, use o cliente com a nova versão do payload e valide a resposta com `LlmAnalysisProcessor.ValidarSaida`.

Os limites configurados de entrada e saída se aplicam à tentativa inteira. A qualidade linguística em provedores reais depende do gate sintético da coordenação. Neste ciclo de implementação a validação usou provedores simulados, sem dados escolares.

## Revisão v4 · evidência de concordância por bloco

### Rotação real após timeout

Em 21/11, a API no commit `e6f2b8d` recebeu uma tentativa sintética de cinco lacunas no schema isolado `ditado_per65_ctxreal1911e`. A cadeia local começou no GonkaBroker e seguiu para o Groq. O payload v4 agrupou o texto digitado em quatro trechos, com gabarito por lacuna e uma definição docente de concordância. A expectativa era classificar acentuação por código e atribuir concordância ao verbo de “os professores chegou”. “A criança chegou” deveria ficar sem esse achado.

O GonkaBroker expirou aos 30 segundos, sem status HTTP observado. O Groq recebeu o lote e devolveu a classificação esperada. As cinco lacunas foram concluídas com nota 20/100. A releitura autenticada repetiu integralmente o retorno. O ensaio capturou a trilha e a resposta da API, sem registrar o corpo HTTP bruto do provedor.

```text
Numero	Provedor	Modelo	PromptVersao	HttpStatus	Desfecho	Motivo
1	GonkaBroker	deepseek-ai/DeepSeek-V4-Flash-0731	per18-v4-contextual	NULL	Tecnica	Timeout
2	Groq	openai/gpt-oss-120b	per18-v4-contextual	200	ComCorrespondencia	NULL
```

```json
{"ditadoId":1,"tentativaId":1,"nota":20,"detalhes":[{"esperado":"café","escrito":"cafe","estado":"Concluida","achados":[{"origem":"Codigo","nome":"Acentuação"}]},{"esperado":"as crianças","escrito":"a criança","estado":"Concluida","achados":[]},{"esperado":"chegaram","escrito":"chegou","estado":"Concluida","achados":[]},{"esperado":"chegaram","escrito":"chegou","estado":"Concluida","achados":[{"origem":"LLM","nome":"Concordância entre sujeito e verbo escritos"}]},{"esperado":"tocou","escrito":"tocou","estado":"Concluida","achados":[]}]}
```

A coordenação observou um falso positivo na lacuna 14 do lote de 24 na v3. O modelo citou “as crianças” ao julgar “a criança chegou”. A v4 envia `blocos` em ordem. Cada bloco traz seu `textoEscrito` contínuo e as próprias `lacunas`, cada uma com ID, esperado, escrito e indicador de avaliação. O único gabarito enviado é o valor esperado por alvo. Os IDs vêm do vínculo entre resposta e segmento.

A divisão usa a pontuação e as quebras de linha do ditado. Uma resposta digitada com pontuação interna permanece vinculada à lacuna inteira. Abreviações seguidas de espaço podem encurtar o bloco. A instrução pede que o modelo use o sujeito e o verbo escritos no trecho do alvo.

O cliente agora retorna o status HTTP recebido nas respostas bem-sucedidas. Os testes exercitam HTTP 200 nos quatro adaptadores e releem o valor gravado pelo worker. O teste de montagem verifica os IDs dentro de cada bloco, inclusive em trechos adjacentes com sujeitos no singular e no plural.

O build v4 teve zero erros e dois avisos preexistentes. Passaram os 66 testes selecionados. A evidência está em `work/logs/per65-contextual-v4-build.log` e `work/logs/per65-contextual-v4.trx`.

Na repetição com Groq real em 21/11, o lote completo teve 23 de 24 lacunas exatas e preservou os dois controles de concordância. A amostra inédita teve 10 de 10 lacunas exatas. [Corpos e resultados do ensaio](documentacao/evidencias/per65-cliente-v4-2026-11-21.md)

O QA independente confirmou 66 testes pertinentes e o fluxo HTTP com código e Groq na mesma tentativa, nota 20/100 e releitura idêntica. A consulta ao schema sintético registrou duas chamadas HTTP 200 com desfecho `ComCorrespondencia`, uma por submissão nas duas demonstrações. [Reteste da PER-65](https://linear.app/joaomoutinho/issue/PER-65/configurar-e-validar-os-quatro-provedores-de-classificacao-llm#comment-a69db4f3-2234-4929-bc2e-c0c17e3dd8e6)

A comparação v4 identificou uma falha de leitura no Cohere. O Command A+ pode retornar blocos `thinking` e `text` na mesma lista. O commit `e6f2b8d` concatena somente os blocos textuais e mantém as validações de conteúdo, tamanho e JSON. O build passou com zero erros e 68 testes pertinentes passaram. [Evidências do comparativo](documentacao/evidencias/per65-provedores-v4-2026-11-21.md)

O QA repetiu 68 testes sem falhas e confirmou pela API com Cohere a nota objetiva, a regra de acentuação e o par de concordância. Duas lacunas ficaram em revisão nessa submissão. A execução parou na asserção de conclusão de todas as lacunas e deixou a releitura final pendente para esse caso. Na demonstração da coordenação, as cinco lacunas concluíram e a releitura foi idêntica. O MySQL registrou um retorno `ComCorrespondencia` e um `Inconclusiva` nas duas submissões, ambos HTTP 200 do Cohere. [Reteste independente](https://linear.app/joaomoutinho/issue/PER-65/configurar-e-validar-os-quatro-provedores-de-classificacao-llm#comment-e9b1460e-7237-416c-aab0-d6377f12af49)

## Ajuste do adaptador Cohere

No ensaio da coordenação, o Cohere devolveu HTTP 200 com `COMPLETE` e um bloco `thinking` antes do JSON. O adaptador agora concatena os blocos `text` na ordem recebida. Uma resposta contendo somente `thinking` segue para a validação de conteúdo vazio e resulta em `SaidaInvalida`. O teste de regressão cobre as duas respostas e confirma a leitura do HTTP 200 quando o JSON é válido.

Para o reteste sintético da coordenação, as DLLs estão em `C:\Users\lucas\AppData\Local\Temp\ditado-per65-cohere-artifacts\bin\Ditado.API\debug`. Os logs locais estão em `work/logs/per65-cohere-build.log` e `work/logs/per65-cohere.trx`.
