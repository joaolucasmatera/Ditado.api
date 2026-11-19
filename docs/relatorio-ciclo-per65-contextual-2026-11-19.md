# PER-65 · classificação contextual por tentativa

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

A coordenação observou um falso positivo na lacuna 14 do lote de 24 na v3. O modelo citou “as crianças” ao julgar “a criança chegou”. A v4 envia `blocos` em ordem. Cada bloco traz seu `textoEscrito` contínuo e as próprias `lacunas`, cada uma com ID, esperado, escrito e indicador de avaliação. O único gabarito enviado é o valor esperado por alvo. Os IDs vêm do vínculo entre resposta e segmento.

A divisão usa a pontuação e as quebras de linha do ditado. Uma resposta digitada com pontuação interna permanece vinculada à lacuna inteira. Abreviações seguidas de espaço podem encurtar o bloco. A instrução pede que o modelo use o sujeito e o verbo escritos no trecho do alvo.

O cliente agora retorna o status HTTP recebido nas respostas bem-sucedidas. Os testes exercitam HTTP 200 nos quatro adaptadores e releem o valor gravado pelo worker. O teste de montagem verifica os IDs dentro de cada bloco, inclusive em trechos adjacentes com sujeitos no singular e no plural.

O build v4 teve zero erros e dois avisos preexistentes. Passaram os 66 testes selecionados. A evidência está em `work/logs/per65-contextual-v4-build.log` e `work/logs/per65-contextual-v4.trx`.

Para repetir o lote com `per18-v4-contextual`, construa `LlmAttemptPrompt(blocos, candidatos)` com um `LlmBlocoPrompt(textoEscrito, lacunas)` por trecho. Use as DLLs de `C:\Users\lucas\AppData\Local\Temp\ditado-per65-contextual-v4-artifacts\bin\Ditado.API\debug`. O resultado linguístico da lacuna 14 depende da repetição com o provedor real.
