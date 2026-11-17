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

## Limitações e decisões pendentes

- Em banco vazio, o EF da base não descobre as três migrations legadas de `CodigoCadastro`, `PinConfigurado` e `NumeroChamada` por falta de metadados da migration. A validação aplicou essas três colunas somente na fixture isolada. A API e o MySQL compartilhados foram preservados.
- Arquivar um erro bloqueia novas seleções e atribuições. Uma aplicação já atribuída mantém a regra antes selecionada, inclusive nas próximas tentativas, até decisão sobre esse caso.
- A demonstração foi feita por HTTP com MySQL real. A validação visual em navegador e o QA independente seguem para depois da revisão de coordenação.
