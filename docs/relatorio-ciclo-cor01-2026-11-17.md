# Ciclo PER-22 — Seleção e correção objetiva

Base do backend `origin/development` em `40fa46e`. Base do frontend `origin/develop` em `a59529c`. Branch de trabalho `codex/per-22-cor01` nos dois repositórios. O contrato e o planejamento aprovado foram registrados antes da implementação. [Contrato COR-01](epicos/cor-01-classificacao-hibrida.md)

## Implementação

- `DitadoService` persiste a seleção de erros e seu contexto escolar. O detalhe administrativo informa a seleção e a existência de respostas. Alterações de seleção verificam as atribuições ativas e a primeira resposta. `TurmaService` valida o destino e copia as referências autorizadas para a aplicação.
- `ClassificadorTiposErro` executa as regras correspondentes à seleção. A submissão persiste o ID da definição associada e retorna `classificacoes` com ID, código e nome. O campo `tiposErro` mantém os tipos legados. A tela `ResultadoAluno.vue` apresenta os nomes do catálogo quando disponíveis. `CadastroDitado.vue` permite editar os metadados de um ditado legado sem enviar seleção vazia.
- A migration `20261117050554_IntegrarCatalogoCor01` cria a associação e as referências opcionais para os resultados e disponibiliza 12 definições públicas vinculadas às regras existentes. As novas definições criadas pelo professor continuam com `temRegraExecutavel=false`. O uso congela a definição mesmo quando o item é desassociado antes da primeira resposta.
- Atualizados Swagger, testes HTTP e de serviço, contrato do catálogo e especificação PG2. LLM assíncrono, contingência, protocolo de coleta, revisão humana e exportação auditada pseudonimizada estão registrados como planejamento da PER-18.

## Validação

- Build da API com `scripts/build-api.ps1` e SDK 9.0.320 em saída isolada, com `UseArtifactsOutput=true` e `ArtifactsPath=%TEMP%\ditado-per22-artifacts`. Zero erros, com os avisos preexistentes. `dotnet ef migrations has-pending-model-changes` confirmou o snapshot. Build do frontend com `npm.cmd run build` concluído.
- 139 testes pertinentes aprovados em EF InMemory e TestServer, incluindo vínculo explícito, múltiplas regras selecionadas, item sem executor, escopo escolar e de turma, imutabilidade, legado e Swagger. A suíte completa teve 256 aprovações de 277, com as mesmas 21 falhas já registradas na base.
- No MySQL 8.0.21 isolado, a migration gerou 12 regras públicas vigentes. Com dados sintéticos e a API servindo este build, o fluxo autenticado passou por criação do ditado, atribuição, alteração da seleção antes da resposta e submissão. O aluno autorizado recebeu HTTP 200 com nota de aproximadamente 33,33, um acerto em três lacunas e duas classificações, Acentuação e Maiúsculas e pontuação. A segunda lacuna ficou revisável. O banco confirmou os dois IDs de catálogo gravados. O aluno externo recebeu 404 e a tentativa de alterar a seleção depois da primeira resposta recebeu 400.

## Revisão da submissão de ditados legados

A revisão da coordenação confirmou que novas tentativas de ditados sem seleção recebem nota e registram respostas incorretas como revisáveis sem classificação. Em `0a6526c`, o teste de regressão recebeu `Classificado` para uma nova tentativa legada. A correção sempre fornece ao classificador o conjunto de regras selecionadas, inclusive quando vazio.

O teste HTTP prepara uma tentativa histórica com Acentuação, realiza uma nova tentativa de duas lacunas e verifica nota 50, ausência de tipos na nova ocorrência e leitura da classificação anterior no relatório do professor. Os cenários de execução por regra continuam usando erros explicitamente selecionados.

Build da API com 0 erros. Os 139 testes pertinentes passaram, incluindo o Swagger gerado. A suíte completa teve 256 aprovações de 277 e repetiu as mesmas 21 falhas da compilação anterior. Esta revisão foi validada em TestServer e EF InMemory.

## Revisão do efeito do arquivamento

Em 17/11/2026, o usuário confirmou que um erro programado arquivado continua classificando novas tentativas das aplicações já atribuídas que conservem o item na seleção. Novas seleções e atribuições recusam esse erro. [Registro da decisão na PER-22](https://linear.app/joaomoutinho/issue/PER-22/cor-01-integrar-erros-mapeados-a-correcao-existente#comment-53a92c17-012e-4497-aa78-456000b35282)

A submissão usa as definições selecionadas na aplicação. A criação de uma seleção ou atribuição exige que o item esteja disponível no catálogo. O teste HTTP atribui o ditado, registra uma tentativa, arquiva o erro e verifica o mesmo ID e nome de classificação em segunda tentativa, nota 50 nas duas tentativas, vínculos persistidos com a mesma escola e aplicação e recusa de novos usos.

O teste confirmou o comportamento da implementação existente. Build da API com 0 erros e 140 testes pertinentes aprovados em TestServer com EF InMemory. Na suíte completa, 257 de 278 testes passaram. As mesmas 21 falhas já ocorriam em `0be3c55`.

## Limitações

- Em banco vazio, o EF da base não descobre as três migrations legadas de `CodigoCadastro`, `PinConfigurado` e `NumeroChamada` por falta de metadados da migration. A validação aplicou essas três colunas somente na fixture isolada. A API e o MySQL compartilhados foram preservados.
- A demonstração inicial foi feita por HTTP com MySQL real. O reteste independente dos fluxos corrigidos segue após a revisão da coordenação.

## Correções dos fluxos apontados pelo QA

O QA de `424ed94` e `14905a7` identificou ausência de vínculo escolar no cadastro manual e uma rota inexistente no carregamento da edição. [Registro do QA na PER-22](https://linear.app/joaomoutinho/issue/PER-22/cor-01-integrar-erros-mapeados-a-correcao-existente#comment-1f688419-5d54-457b-bb82-c2940da20b49)

- `UsuarioService.CadastrarAlunoManualAsync` valida a escola ativa e o escopo do ator, cria o vínculo escolar do aluno e concede participação nas aplicações ativas da turma na mesma transação. Cadastro e atribuição bloqueiam a linha da turma para serializar o ingresso com uma atribuição simultânea. Administrador sem vínculo e sem capability global recebe 403. Cadastro em lote e confirmação de importação no frontend usam esse endpoint.
- `CadastroDitado.vue` usa `ditadoService.visualizar`, reconstrói o texto com colchetes a partir dos segmentos e carrega áudio, categorias e seleção. O PUT envia título, descrição, categorias e a seleção editável. Texto e áudio são exibidos para consulta conforme os campos permitidos na atualização.
- Os testes HTTP cadastram o aluno antes e depois da atribuição, configuram o primeiro PIN, autenticam e submetem pelo fluxo público da API. O próprio cadastro fornece vínculo e participação. A regressão também recusa professores e administradores sem escopo.
- O teste de navegador reutilizável `tests/e2e/cor01-fluxos.e2e.js` usa dados sintéticos na API e no MySQL isolados. Ele abre a edição, salva metadados e seleção antes da primeira resposta, cria o PIN do aluno cadastrado após a atribuição, submete pela tela e verifica o bloqueio da seleção ao reabrir o formulário.

Os builds da API e do frontend passaram. O recorte de 45 testes de cadastro, contexto escolar, importação, turma e correção passou em TestServer/EF InMemory. A suíte completa teve 263 aprovações de 284, com as mesmas 21 falhas da base `424ed94`.

A regressão no Chrome passou com a API recompilada em 5041, frontend em 5181 e MySQL 8.0.21 isolado da tarefa. A criação do aluno após a atribuição, o primeiro PIN, a submissão e o resultado ocorreram pelos endpoints reais. A edição pela tela carregou título, descrição, categorias, texto reconstruído, áudio e seleção. O salvamento manteve texto, áudio e categoria e alterou os metadados e a seleção antes da resposta. Após a submissão, a seleção ficou desabilitada na tela, e salvar metadados manteve as referências anteriores. O Swagger gerado documenta o vínculo ativo e a concessão de participação pelo cadastro.

Para executar a regressão de navegador, definir `E2E_API_URL` para a raiz da API isolada, incluindo `/api`, e `E2E_BASE_URL` para o frontend correspondente. No checkout frontend, executar `node tests/e2e/cor01-fluxos.e2e.js`. O teste usa o Playwright instalado e o Chrome em `%LOCALAPPDATA%\Google\Chrome\Application\chrome.exe`, com substituição por `E2E_CHROME_PATH` quando necessário.

## Correção de colisão no código de entrada

O reteste do QA identificou que o código composto pelos nomes completos da escola e da turma era cortado em 32 caracteres e perdia o número da chamada. Duas chamadas diferentes da mesma turma geravam o mesmo código. A fixture curta do primeiro ciclo não cobria essa condição. A regressão com nomes longos reproduziu a igualdade dos códigos em `582e9e2`.

`CadastrarAlunoManualAsync` passa a emitir códigos com os IDs persistidos da escola e da turma e a chamada completa, no formato `E12-T34-N003`. Esse formato cabe no limite existente e mantém a composição escola–turma–chamada. A geração é compartilhada pelo cadastro manual, lote e confirmação de importação. O cadastro de novos alunos mantém os códigos anteriormente emitidos. Swagger e a ajuda da tela de login registram o novo exemplo. [Contrato de acesso do aluno](epicos/multi-tenant-mt02.md)

O teste HTTP cria dois alunos com chamadas 1 e 999, em escola e turma com nomes longos, e verifica códigos distintos, primeiro PIN, login na conta correspondente e submissão de ambos. Uma conta com código no formato anterior também configura o PIN e faz login. A regressão de navegador foi ampliada com o mesmo caso no MySQL isolado.

Os builds da API e do frontend passaram. Foram aprovados 46 testes pertinentes em TestServer/EF InMemory. A suíte completa teve 264 aprovações de 285, com as mesmas 21 falhas da base `582e9e2`.

No MySQL 8.0.21 isolado, o E2E confirmou HTTP 200 para os dois cadastros de chamadas 1 e 999 na mesma escola/turma com nomes longos e códigos distintos. Ambos concluíram primeiro PIN, login e submissão com os IDs corretos de usuário e de classificação. O mesmo teste voltou a conferir a edição e o bloqueio da seleção após a primeira resposta. Nenhum código existente foi reescrito.
