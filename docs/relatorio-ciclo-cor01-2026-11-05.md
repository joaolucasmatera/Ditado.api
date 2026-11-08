# Relatório do ciclo COR-01 — 5 nov 2026

## Resultado implementado

* O classificador por código retorna múltiplos tipos quando regras independentes se aplicam. Omissão, acréscimo e substituição passaram a ser tipos separados nos novos resultados, com IDs 29, 30 e 31; o ID legado 23 permanece apenas para leitura de dados antigos. Não houve migração destrutiva nem reclassificação histórica.
* A classificação de caixa agora pode coexistir com uma edição simples. Os casos cobertos são `café → Cafe` (Acentuação + Maiúsculas), `casa → Caza` (Grafia arbitrária + Substituição + Maiúsculas) e `prato → Pato` (Omissão + Complexidade silábica + Maiúsculas).
* Omissão/acréscimo/substituição tem rótulos próprios na resposta. Registros legados recebem uma descrição que indica operação não discriminada, sem agrupar os novos tipos.
* As heurísticas interpretativas de Transcrição da fala (troca de vogais) e Hipercorreção (`-il → -io`) foram removidas desta correção automática. As respostas ainda podem receber o tipo de operação observável ou ficar sem classificação quando não houver regra segura.
* Permanecem as decisões já confirmadas: nota sensível a maiúsculas, apenas espaços externos ignorados, análise somente do par esperado/resposta da lacuna, múltiplos rótulos e preservação da correção objetiva.

## Arquivos do backend

* `Ditado.Dominio/Services/ClassificadorTiposErro.cs` — regras determinísticas, acumulação multi-tipo e distinção entre regra de caixa pura e caixa combinada a uma edição.
* `Ditado.Dominio/Enums/TipoErro.cs` — IDs novos 29–31 acrescentados; IDs históricos preservados.
* `Ditado.Dominio/Extensions/TipoErroExtensions.cs` — rótulos próprios e legíveis para novos tipos e indicação explícita de operações históricas não discriminadas.
* `Ditado.Aplicacao/Services/DitadoService.cs` — mantém a comparação objetiva case-sensitive, o cálculo de nota e a delegação da classificação para o domínio.
* `Ditado.Aplicacao/DTOs/ResultadoDitadoResponse.cs`, `Ditado.Dominio/Entidades/RespostaSegmento.cs`, `RespostaSegmentoTipoErro.cs`, configurações e `DitadoDbContext` — persistência e retorno de múltiplos tipos, método e estado da classificação.
* `Ditado.Aplicacao/Services/ProfessorService.cs` — analytics contam os tipos associados sem colapsar operações distintas.
* `Ditado.Infra/Migrations/20261105010000_PersistirClassificacaoResposta*`, `20261105182032_SuportarMultiplosTiposErro*` e `DitadoDbContextModelSnapshot.cs` — classificação persistida e associação multi-tipo. Os IDs 29–31 são armazenados no campo inteiro existente, portanto não exigiram nova migração.
* `Ditado.Testes/Services/DitadoServiceTests_TiposErro.cs`, `Ditado.Testes/Controllers/DitadosControllerTests.cs`, `Ditado.Testes/Services/ProfessorServiceTests.cs` — regressões de regras, nota, API e analytics, incluindo operações separadas e as três combinações de caixa.
* `Ditado/docs/ESPECIFICACAO_PG2.md` — atualizada a tabela 10.6: operações separadas; transcrição e hipercorreção adiadas para possível cadastro do professor e tratamento por LLM futuro.

## Validação

* Testes filtrados na solução oficial: **62/62 passaram** (`DitadoServiceTests_TiposErro`, `DitadosControllerTests`, `ProfessorServiceTests`). Executados com output isolado e acesso de loopback para o testhost do VSTest.
* Build da API: passou em output isolado. Avisos existentes: SDK .NET preview, conflito entre versões do Entity Framework Core e variável não usada no controller.
* QA headed anterior: 32/35 casos passaram; os três casos de combinação de caixa acima falharam e originaram a correção atual.
* Reteste headed final: **35/35 passaram** na API atual. QA confirmou IDs separados, classificação múltipla, nota case-sensitive e snapshot da fixture original idêntico antes/depois; a janela do navegador permanece aberta.
* QA executou diretamente o assembly atualizado em `C:\Users\lucas\AppData\Local\Temp\ditado-cor01-test-build4\Debug\net9.0\Ditado.Testes.dll`, usando roll-forward para o runtime instalado: **62/62 passaram**, sem falhas, incluindo as três regressões de caixa combinada. Os 35/35 casos headed também passaram.
* A tentativa de recompilar pelo ambiente QA encontrou `NETSDK1127` (pacote targeting ausente); isso bloqueou o rebuild, não a execução nem a implementação. QA usou o assembly atual com roll-forward e confirmou os 62 testes.
* API atualizada está ativa em `http://127.0.0.1:5031`, PID 10976, com Swagger HTTP 200. Vite permanece ativo em 5173 e MySQL não foi alterado.
* A fixture original Ditado 1 / RespostaDitado 1 permaneceu intacta; o QA confirmou snapshot dos dados e hash do áudio antes/depois.

## Pendências e estado

* CAT-05 ainda é necessária para filtrar as regras pela seleção persistida de erros em cada ditado; é a única dependência funcional restante, e a história continua **In Progress** no Linear PER-22.
* Transcrição da fala e hipercorreção estão fora do classificador automático desta entrega. Fallback LLM também permanece fora do escopo.
* Trabalho na branch `codex/cor-01-multiplos-tipos`; nenhuma alteração foi commitada.
