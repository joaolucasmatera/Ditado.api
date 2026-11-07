# Changelog da documentação

## 07/11/2026 — V0 do relatório vivo

- Criado `docs/documentacao/relatorio-aplicacao.md`.
- Registrado o estado atual conhecido da aplicação Ditado.
- Separados itens implementados, planejados, legados, hipóteses e desconhecidos.
- Incluídas arquitetura, funcionalidades, requisitos, regras, persistência, fluxos, segurança, fundamentação pedagógica, QA, riscos e pendências.
- Referenciado o diagrama ER existente e listados os diagramas ainda necessários.
- Nenhum código, banco, serviço ou issue do Linear foi alterado nesta etapa.

### Fontes usadas

- código e estrutura do backend;
- `README.md`;
- `docs/REFERENCIAL_PROJETO.md`;
- `Ditado/docs/ESPECIFICACAO_PG2.md`;
- `Ditado/docs/HISTORIAS_EPICO_CATALOGO.md`;
- contratos e relatórios em `docs/epicos/` e `docs/relatorio-ciclo-*.md`;
- relatórios de QA;
- referências pedagógicas em `docs/referencias/`.

### Pendências

- revisão humana da V0;
- dados acadêmicos e institucionais;
- validação dos limites entre estado atual e planejamento;
- definição dos próximos diagramas e prioridade de detalhamento.

## 07/11/2026 — Refinamento MT-04 incorporado à prévia

- Registrado no relatório o refinamento técnico proposto para persistência, isolamento e histórico multi-tenant.
- Incluídas as decisões sobre atividades já atribuídas, aluno ingressante, arquivamento, suspensão do professor e adoção entre escolas.
- Mantido o status como planejamento, sem implementação, migration ou alteração de dados vivos.
- Pendências técnicas do MT-04 foram adicionadas à lista de validação.

## 07/11/2026 — Decisão confirmada sobre aplicações do MT-04

- Registrado que uma turma não pode receber aplicações duplicadas do mesmo ditado.
- Registrado que novos alunos entram na aplicação existente e só acessam após atribuição.
- Registrado que o aluno pode realizar várias tentativas, cada uma com resultado próprio, referenciando a mesma aplicação e revisão imutável.
- Status: decisão confirmada pelo usuário; sem alteração de código ou banco.
- Fontes: conversa do projeto e comentário Linear associado ao PER-29.

## 07/11/2026 — Imutabilidade simples de ditados no MT-04

- Registrado que ditado aplicado ou iniciado fica imutável nesta etapa.
- Correções de texto, áudio ou resposta esperada exigem novo ditado.
- Registrado que não haverá versionamento complexo do mesmo cadastro neste momento.
- Status: decisão confirmada pelo usuário; sem alteração de código ou banco.
- Fontes: decisão do usuário e comentário Linear associado ao PER-29.

## 07/11/2026 — Rastreamento do MT-04 no Linear

- Registrada a issue `PER-30 — Persistência, isolamento e histórico` como referência de acompanhamento do MT-04.
- Link: https://linear.app/joaomoutinho/issue/PER-30/mt-04-persistencia-isolamento-e-historico
- Status documental: refinamento técnico simples/incremental, sem implementação de código ou migration.

## 07/11/2026 — Parecer técnico incremental do MT-04

- Registrada como recomendação a manutenção do modelo atual, sem `DitadoRevisao` ou `AudioVersao` nesta etapa.
- Registrada a proposta de congelar o ditado na primeira atribuição, exigir novo `DitadoId` para correções e manter uma aplicação única por turma e ditado.
- Registrados múltiplos resultados em `RespostaDitado`, seleção de erros por aplicação, FKs/índices objetivos, transações curtas e testes MySQL.
- Status: parecer técnico, não decisão de escopo e não implementação.
- Fonte: comentário técnico na issue Linear PER-30.

## 07/11/2026 — Arquivamento e tentativas no MT-04

- Registrado que arquivamento bloqueia novas atribuições, mas não interrompe tentativas de alunos já vinculados à aplicação.
- Registrado que remover o aluno da turma bloqueia novas tentativas desse aluno e preserva o histórico.
- Status: decisão confirmada pelo usuário; sem alteração de código ou banco.
- Fonte: comentário do usuário na issue Linear PER-30.

## 07/11/2026 — Sequência planejada do MT-04

- Registradas as dez etapas: baseline/documentação/ER/contratos; inventário/migração; fundação tenant; sessões/auditoria; aplicações/tentativas; visibilidade; áudio/exportação; testes MySQL; QA; UX.
- Registrados riscos e dependências relacionados a isolamento entre escolas, histórico, revogação, disco, contratos e protótipos.
- Status: planejamento, sem alteração de código e sem autorização automática de implementação.

## 07/11/2026 — Refinamento da sequência do MT-04

- Ajustada a sequência para incluir backup/restauração em cópia, fundação de `EscolaId`, vínculos, integridade, autorização e sessões antes das rotas tenant-aware.
- Registrada a dependência de aplicações antes de CAT-05/COR-01 e de backup/armazenamento antes da limpeza de áudio.
- Mantidos QA e UX antes das telas.
- Status: planejamento, sem alteração de código ou banco.

## 07/11/2026 — Decomposição Linear do MT-04

- Registradas as etapas PER-31 a PER-38: baseline; EscolaId; autorização/sessões/auditoria; aplicações/participação/tentativas; visibilidade/arquivamento; áudio/exportação; testes MySQL/QA; protótipos UX.
- Registrado que PER-31 está In Progress e as demais etapas estão em Backlog, com dependências no Linear.
- Status: planejamento/executável, sem alteração de código nesta decomposição.

## 07/11/2026 — Baseline do PER-31

- Registrada a branch `codex/mt04-foundation-tenant`.
- Registrada a ausência de `Escolas` e `VinculosEscolares` e o histórico de migration até `20261106051829_PublicarErroMarketplace`.
- Registrado backup lógico privado pré-migration concluído com `mysqldump --single-transaction --no-tablespaces --routines --triggers`.
- Registrado que nenhuma migration, dado ou serviço foi alterado no baseline.
- Registrado bloqueio técnico de `pending model changes` por divergência entre assembly de inicialização/API e snapshot EF; fundação/migration permanecem locais até validação.
- Status: fatos do PER-31, sem parecer ou decisão de escopo.

## 07/11/2026 — Conclusão registrada do PER-31/PER-32

- Registrada a conclusão de PER-31 e PER-32.
- Registrada a aplicação única da migration `20261107044724_AdicionarFundacaoTenant` após backup e validação do modelo.
- Registradas as tabelas `Escolas` e `VinculosEscolares` vazias, FKs/índices/uniques validados e dados legados preservados.
- Registrados API/frontend HTTP 200 e `MarketplaceControllerTests` 33/33 aprovados.
- Registrada validação de schema/regressão pelo QA sem achado funcional, com limitação de runner por ausência de `testhost.dll`.
- Registrado início do PER-33.
- Status: atualização factual, sem opinião ou aprovação adicional da documentação.

## 07/11/2026 — Incidente controlado de migration no PER-33

- Registrado que `dotnet ef migrations remove --no-build --force` utilizou assembly anterior.
- Registrado rollback local apenas das tabelas novas vazias `Escolas`/`VinculosEscolares` e do histórico.
- Registrado que nenhum dado legado foi alterado.
- Registrado plano do Fullstack para recompilar, corrigir snapshot, regenerar migrations e validar SQL, histórico e contagens antes de reaplicar.
- Registrado QA como gate antes do PER-34.
- Status: incidente controlado, sem alteração de escopo.

## 07/11/2026 — Atualização do PER-33 após reaplicação

- Registrada a aplicação da migration `20261107050300_AdicionarFundacaoTenant` após backup e revisão SQL.
- Registradas as tabelas criadas `AuditoriaEventos`, `Escolas`, `SessoesUsuarios` e `VinculosEscolares`, todas com zero registros.
- Registrada a ausência de `DROP`, `UPDATE` e backfill, com dados legados preservados.
- Registradas a confirmação do histórico e de `has-pending-model-changes`.
- Registrada a limitação ambiental do runner/SDK: `SecurityException` de telemetria e falha de ambiente impediram concluir suíte/build; sem erro de código reportado.
- Registrados QA oficial acionado e PER-34 bloqueado até validação.

## 07/11/2026 — Contexto escolar e autorização no PER-33

- Registrado `GET /api/usuarios/contexto-escolar`.
- Registradas validação server-side por vínculo/escola ativa e auditoria de decisões permitidas/negadas.
- Registrados testes InMemory isolados de serviço e revogação revisados pelo QA.
- Registrada limitação de execução do runner/SDK por falha de workloads e saída vazia, com zero testes executados.
- Registradas lacunas de E2E autenticado, controller/claims, escola inativa e concorrência.
- PER-33 permanece In Progress e PER-34 bloqueada.

## 07/11/2026 — Fechamento do PER-33

- Registrada aceitação do QA após confirmação de schema/migration.
- Registrados 6/6 testes isolados aprovados, cobrindo serviço e controller/header.
- Registrado build .NET 9 estável, com zero erros e avisos preexistentes.
- Registradas como limitações futuras não bloqueantes as coberturas runtime de claims/JTI inválido, expiração e concorrência.
- Registrada transição no Linear: PER-33 Done e PER-34 In Progress.

## 07/11/2026 — Compatibilidade temporária do PER-34

- Registrada a preservação e legibilidade do legado `TurmaDitado`/`TurmaAluno`/`RespostasDitados`.
- Registradas novas escritas exclusivamente em `AplicacaoDitado`/`Participacao*`, sem escrita dupla.
- Registrado que limpeza do banco não está autorizada.
- Registrado que migrations dependem de adaptadores e testes.
- Status: decisão confirmada pelo usuário.

## 07/11/2026 — Melhor resultado principal nos relatórios

- Registrado conflito anterior em que relatórios do professor usavam a primeira tentativa.
- Registrado ajuste dos caminhos legado/novo para selecionar o melhor resultado por aluno em nota, data e métricas de erros.
- Registrada preservação de todas as tentativas.
- Registrados build com zero erros e 21/21 testes de `ProfessorService` aprovados.
- Registrada investigação detalhada de métricas no PER-40 para etapa posterior.

## 07/11/2026 — Fechamento do PER-34

- Registrada aplicação única da migration após backup verificável.
- Registradas contagens legadas preservadas e novas tabelas vazias.
- Registrados build sem erros, 9/9 testes isolados e 21/21 testes de `ProfessorService` aprovados.
- Registrada aceitação pelo QA.
- Registradas como limitações não bloqueantes a ausência de consumidores de `ParticipacaoTurma` nesta fatia e a investigação separada no PER-40.
- Registrada transição no Linear: PER-34 Done e PER-35 In Progress.

## 07/11/2026 — EscolaId nullable no PER-35

- Registrado que `Turma.EscolaId` será inicialmente nullable.
- Registrada a ausência de backfill automático para turmas legadas.
- Registrado que novas turmas e novas publicações exigem escola definida.
- Registrada a preservação da legibilidade de registros antigos nulos até mapeamento explícito.
- Status: decisão aprovada no PER-35.

## 07/11/2026 — Início funcional do PER-35

- Registrado que o gate de `Turma.EscolaId` foi concluído com migration aplicada e QA aceito.
- Registrado o início da implementação funcional de visibilidade e arquivamento para Ditados e Catálogo de erros.
- Registrados como pendentes arquivos, testes, revisão do QA e novo gate de backup/migration.

## 07/11/2026 — Correção fail-closed do PER-35

- Registrada exigência de vínculo, papel e ator para autorização.
- Registrado que Administrador sem vínculo é negado.
- Registradas restauração autorizada e concorrência controlada.
- Registrada validação final em andamento.
- Registrada pendência da capability/claim explícita do Administrador Geral/Técnico, sem inferência por ausência de vínculo.

## 07/11/2026 — Capability do Administrador Geral/Técnico

- Registrada decisão aprovada de identificação por capability explícita persistida.
- Registrado que a capability fica desativada por padrão.
- Registrado que acesso global não será inferido pela ausência de vínculo escolar.
- Implementação e validação permanecem pendentes.

## 07/11/2026 — Implementação da capability AdministradorGeral

- Registrado `Usuario.AdministradorGeral` com padrão `false`.
- Registrados claim JWT e guards fail-closed.
- Registrado que nenhuma conta existente foi promovida.
- Registrada migration aditiva gerada, ainda não aplicada.
- Registrados 6/6 testes aprovados e build sem erros.
- Registrados QA e gate de backup como pendências.

## 07/11/2026 — Transição PER-35 para PER-36

- Registrado encerramento do PER-35 após QA e gate pós-DDL.
- Registrado início do PER-36, dedicado a áudio e exportação segura.
- Reafirmadas as decisões de limpeza em duas fases após 24 horas, preservação de referenciados e exportação sem áudio com limites.

## 07/11/2026 — Persistência, áudios e exportação no MT-04

- Registrado MySQL/InnoDB compartilhado com isolamento por `EscolaId` e autorização server-side.
- Registrada retenção de 24 horas para áudios não atribuídos antes da limpeza em duas fases.
- Registrado que áudios referenciados por aplicação, tentativa ou histórico nunca são removidos.
- Registrada exportação direta sem áudio, com cursor/streaming limitado e limites de linhas, tamanho e tempo.
- Registrada recusa acima dos limites e ausência de arquivo temporário/e-mail nesta etapa.
- Status: decisões confirmadas pelo usuário; sem alteração de código ou banco.
- Fonte: comentário do usuário na issue Linear PER-30.

## 07/11/2026 — Estratégia de transição do PER-34

- Registrada a decisão do usuário pela compatibilidade temporária entre o modelo legado e as novas entidades de aplicação/participação.
- Dados legados permanecem preservados e legíveis durante a transição; novas escritas devem usar somente o modelo novo.
- Registrada a proibição de duas escritas concorrentes e de limpeza do banco de testes sem aviso e autorização explícitos.
- Migration de aplicações/participações permanece condicionada à definição dos adaptadores e testes de transição.
- Fontes: decisão do usuário, parecer do gerente de projetos e levantamento do Fullstack 01.
- Status: decisão confirmada; implementação da transição ainda pendente.

## 07/11/2026 — Métrica principal dos relatórios do professor

- A verificação identificou que os relatórios usavam a primeira tentativa como métrica principal, em conflito com a decisão do MT-04.
- O Fullstack ajustou os caminhos legado e novo para selecionar o melhor resultado por aluno; nota, data e métricas de erros passaram a usar esse resultado.
- Todas as tentativas continuam preservadas no histórico.
- Validação registrada: build sem erros e 21/21 testes do `ProfessorService` aprovados.
- A investigação detalhada de quais métricas devem exibir primeira tentativa, melhor resultado ou ambas foi registrada no PER-40 para etapa posterior.

## 07/11/2026 — Aplicação controlada do PER-34

- Registrada a aplicação única da migration `20261107052632_AdicionarAplicacoesParticipacoes` após backup lógico verificável e revisão do SQL.
- Registrado o backup `mt04-pre-per34-20261107.sql`, com 511.321 bytes e SHA-256 `3947C04632684444A0317108D2B598FD307B22A6EBD3E03455A5BC675CE3CF00`.
- Contagens legadas preservadas após a aplicação: TurmaDitados=2, TurmaAluno=1, RespostasDitados=15, RespostaSegmentos=112 e ErrosCatalogo=5; tabelas novas iniciaram vazias.
- Registrados build sem erros e 9/9 testes isolados relevantes aprovados; a suíte do ProfessorService alinhada ao melhor resultado passou 21/21.
- QA final ainda deve confirmar a transição e recomendar o fechamento do PER-34.

## 07/11/2026 — Escola canônica da turma no PER-35

- Registrada decisão de escopo aprovada pelo usuário: adicionar `Turma.EscolaId` inicialmente nullable, com FK e índice para `Escolas`.
- Não haverá backfill automático das turmas legadas; registros sem escola permanecem legíveis até mapeamento explícito.
- Novas turmas e novas publicações exigirão escola definida; o vínculo do professor continua como checagem adicional de autorização.
- Fonte: decisão do usuário, após parecer do gerente de projetos e do QA oficial.
- Status: decisão confirmada; implementação e validação do PER-35 pendentes.

## 07/11/2026 — Fundação `Turma.EscolaId` preparada e revisada

- O Fullstack adicionou `Turma.EscolaId` nullable, navegação, FK restritiva e índice; a criação de novas turmas exige escola ativa.
- Novas atribuições bloqueiam turmas legadas sem escola e validam, no servidor, o vínculo ativo do usuário autenticado e do professor responsável na mesma escola.
- A migration `20261107062008_AdicionarEscolaTurma` é aditiva (coluna nullable, índice e FK), sem backfill, `UPDATE` ou `DROP`; não foi aplicada ao banco.
- Validação registrada: build com 0 erros, `TurmaEscolaTests` 4/4 aprovados e modelo sem alterações pendentes.
- QA oficial aceitou esta fatia, condicionando qualquer aplicação ao gate separado de backup, revisão do SQL e verificação pós-DDL.
- Fonte: Fullstack 01, QA oficial e decisão de escopo do usuário; status: implementação preparada, migration pendente de gate operacional.

## 07/11/2026 — Aplicação controlada da migration do PER-35

- Após o aceite do QA, foi gerado o backup lógico `backups/mt04-pre-per35-20261107-032819.sql` com 475.566 bytes e SHA-256 `2539FC2BDB8B56CD99BC30E0C99865104640836141E796B544A2BFC057C3C7AE`.
- A migration `20261107062008_AdicionarEscolaTurma` foi aplicada uma única vez com lock do EF Core, sem backfill, `UPDATE` ou `DROP`.
- Verificação pós-DDL: `Turmas.EscolaId` é nullable; FK `FK_Turmas_Escolas_EscolaId` aponta para `Escolas(Id)` com `RESTRICT`; índice `IX_Turmas_EscolaId` existe; migration consta em `__EFMigrationsHistory`.
- Contagem após a aplicação: 1 turma, 1 sem escola; nenhum registro foi preenchido automaticamente.
- Limitação observada: o primeiro dump tentou tablespaces e encontrou falta de privilégio `PROCESS`; o backup válido foi refeito com `--no-tablespaces`, concluído e hash registrado acima.
- Fonte: Local App Runner e consultas SQL pós-DDL; status: gate de persistência concluído, permanecem as histórias funcionais de visibilidade/arquivamento do PER-35.

## 07/11/2026 — Capability explícita do Administrador Geral

- Registrada decisão do usuário: o acesso global do Administrador Geral/Técnico será uma capability persistida no usuário, desativada por padrão.
- A capability será validada no servidor e refletida em claims/contexto autenticado; não será inferida por ausência de vínculo nem recebida do frontend.
- Administradores escolares continuam limitados aos vínculos ativos; contas existentes não serão promovidas automaticamente.
- Status: decisão confirmada; implementação, testes e migration aditiva pendentes.

## 07/11/2026 — Parecer técnico sobre a capability global

- Gerente de projetos, QA e especialista em banco convergiram que o booleano `AdministradorGeral NOT NULL DEFAULT false` é adequado para a migration inicial, sem backfill ou promoção automática.
- O claim JWT deve ser apenas contexto; guards críticos devem consultar a capability atual, usuário ativo e sessão válida no servidor.
- Antes do gate operacional, deve existir operação dedicada de concessão/revogação, fora do DTO genérico, com transação, auditoria e invalidação das sessões do alvo.
- A capability não substitui `VinculoEscolar`; administradores escolares continuam limitados à própria escola. Não criar tabela genérica de capabilities nesta etapa.
- Status: recomendação técnica aceita para implementação; operação dedicada e testes ainda pendentes.

## 07/11/2026 — Aplicação das migrations de visibilidade e capability global

- Backup pré-DDL registrado em `backups/mt04-pre-visibility-capability-20261107-041412.sql`, com 475.810 bytes e SHA-256 `29DD3F41E43BCB4B0F10F1B4657C4A220EF64878EAF4D30F6809DBCE66C5CCDE`.
- Aplicadas, em ordem e uma única vez, as migrations `20261107063402_AdicionarVisibilidadeArquivamento` e `20261107070513_AdicionarCapabilityAdministradorGeral`, com lock do EF Core.
- Verificado por leitura: quatro tabelas de publicação; colunas de arquivamento nullable; `Usuarios.AdministradorGeral` NOT NULL com default 0; índices únicos/compostos; FKs de domínio `RESTRICT` e junções `CASCADE`.
- Contagens preservadas: usuários=3, administradores gerais=0, ditados=8 e erros=5; nenhuma conta foi promovida e nenhum backfill foi executado.
- Status: gate de persistência do PER-35 concluído; aguarda aceite pós-DDL do QA e validação final da suíte no ambiente compatível.

## 07/11/2026 — Encerramento do PER-36: áudio e exportação segura

- O Fullstack implementou retenção de áudio em duas fases: candidatos acima de 24 horas sem `AplicacaoDitado`, `TurmaDitado` ou `RespostaDitado`, confirmação com revalidação em transação `Serializable`, preservação de registros/metadados e auditoria de selecionados, removidos e preservados.
- A exportação de respostas não inclui áudio, valida escolas ativas e escola canônica da aplicação, aplica cursor e limites de linhas/tamanho/tempo, audita recusas e cancelamentos e gera o CSV incrementalmente em arquivo temporário limitado antes de iniciar o download; o temporário é removido em `finally`.
- A autorização global exige simultaneamente `TipoUsuario.Administrador` e `AdministradorGeral=true`; flags inconsistentes em professores são negadas.
- Evidências: `AudioExportacaoTests` 5/5 e build sem erros; nenhuma migration ou alteração de produção foi feita nesta história.
- O QA independente aceitou o PER-36. Limitação: o runner não executou a suíte por incompatibilidade já reproduzida entre VSTest 17.14.1 e TestHost/TestPlatform 18.0.1; o aceite baseou-se na inspeção e na suíte do Fullstack. Melhoria não bloqueante: evento específico para falha de limite durante a geração.
- Fonte: código, testes, parecer do gerente de projetos, QA independente e Linear `PER-36`; status: implementado e aceito, com limitação de ambiente registrada.

## 07/11/2026 — Início dos gates PER-37 e PER-38

- O PER-37 foi iniciado para repetir a validação real em MySQL e executar o QA independente. O gate está temporariamente bloqueado no ambiente atual: não há cliente `mysql`/`mariadb`, o Docker Desktop não está acessível pelo named pipe, não há listener MySQL local e o runner VSTest 17.14.1 não negocia com TestHost/TestPlatform 18.0.1. A verificação do Local App Runner foi somente leitura e não alterou dados.
- O PER-38 foi iniciado com o agente UX para prototipar os fluxos multi-tenant antes de qualquer implementação de telas: escola no cadastro, contexto escolar, visibilidade, atribuição, acesso negado, arquivamento, áudio e exportação. O padrão visual existente e a linguagem simples para professores permanecem requisitos.
- Fonte: QA oficial, Local App Runner, Linear `PER-37`/`PER-38` e coordenação; status: PER-37 bloqueado por infraestrutura externa, PER-38 em prototipação.

## 07/11/2026 — Protótipo UX do PER-38 disponível

- O agente UX produziu o protótipo navegável `per38-multi-tenant.html` no diretório de visualizações da tarefa. Ele cobre escolha/criação de escola, escola ativa, visibilidade Público geral/Instituição/Turmas, atribuição, acesso negado, arquivamento/restauração e limpeza/exportação de áudio.
- O protótipo preserva o shell visual existente e a nomenclatura de produto **Ditados** e **Catálogo de erros**. Ainda não há implementação de telas; a aprovação visual é pré-requisito para solicitar trabalho ao Fullstack.
- Fonte: protótipo e parecer do agente UX; status: artefato produzido, aguardando aprovação.

## 07/11/2026 — Gate de persistência isolada do PER-37

- O Docker Desktop foi reabilitado e o gate foi executado no container existente `ditado-mysql` (`mysql:8.0.21`, contexto `desktop-linux`, `127.0.0.1:3306`, estado `healthy`), sem recriar ou remover container/volume.
- Foi gerado backup lógico consistente antes dos cenários mutáveis: `work/backups/ditado-before-per37-root-20261107-044241.sql`, 480.858 bytes, SHA-256 `2D4CC003D712507AC44E382DE6371A8429E63548715C4FF7FF2D7A3D719BF627`.
- O backup foi restaurado no schema derivado `ditado_per37_validation_20261107`, preservado para auditoria. O dump pós-cenários ficou em `work/backups/ditado-per37-validation-schema-20261107.sql`, 481.918 bytes, SHA-256 `94BCAD9CD8DE2EE7127A93551DFFFCC5DA4B76EB9D5A635BF9AD3A9319B38923`.
- No derivado foram confirmados baseline/contagens, 25 tabelas, capability `AdministradorGeral` 0→1, arquivamento com metadados, sessão revogada, auditoria, rejeição de publicação duplicada por índice único (1062) e rejeição de escola inexistente por FK (1452). O banco compartilhado não foi alterado.
- O QA aceitou o gate de persistência/constraints, mas não o aceite final: ainda faltam execução de API/serviços contra o derivado, isolamento/guards por endpoints, concorrência, retenção de áudio, exportação/cancelamento/auditoria funcional e regressão automatizada. O runner continua incompatível entre VSTest 17.14.1 e TestHost/TestPlatform 18.0.1.
- Fonte: Local App Runner, Fullstack, QA, gerente de projetos e Linear `PER-37`; status: implementação de persistência validada, aceite funcional pendente.

## 07/11/2026 — Aceite funcional do PER-37 após correção MySQL

- O filtro de retenção foi corrigido para consulta SQL explícita com `OCTET_LENGTH(AudioLeitura) > 0`, pois tanto `Any()` quanto `.Length` eram reescritos pelo Pomelo para uma expressão não traduzível. A consulta projeta somente identificador, data e tamanho, sem carregar o BLOB para decidir elegibilidade.
- No schema derivado, o build SDK .NET 9.0.320 terminou com exit 0; a API temporária respondeu Swagger 200. Um ditado antigo com 3 bytes foi selecionado e removido, um ditado com áudio vazio foi ignorado, a confirmação retornou 200/1 e as fases de seleção/confirmação foram auditadas. Sem capability, o endpoint retornou 403 explícito.
- Regressões funcionais repetidas: contexto escolar sem vínculo 403, exportação global 200, revogação de sessão 204 e token revogado 401. O banco compartilhado, container e volume permaneceram intactos.
- O QA aceitou funcionalmente o PER-37 no ambiente isolado. A limitação residual é a não repetição da suíte automatizada pelo runner independente devido à incompatibilidade VSTest 17.14.1 × TestHost/TestPlatform 18.0.1; o Fullstack reportou 101/101 com SDK9. A limitação não foi confundida com falha funcional.
- Fonte: código, Local App Runner, Fullstack, QA, gerente de projetos e Linear `PER-37`; status: concluído com limitação de ambiente documentada.

## 07/11/2026 — Decisão do fluxo de escola no PER-38

- O usuário aprovou o fluxo recomendado: o professor escolhe uma escola existente ou cria uma nova no cadastro, mas a seleção não cria vínculo automaticamente.
- O vínculo só será criado por convite ou aprovação. Solicitação autônoma de entrada fica fora desta fatia.
- O protótipo UX será atualizado para mostrar escola selecionada, vínculo pendente/aguardando aprovação e vínculo ativo, sem implementar telas de produção antes da revisão visual.
- Fonte: decisão explícita do usuário, parecer do gerente e Linear `PER-38`; status: decisão confirmada, protótipo em atualização.

## 07/11/2026 — Regra de operações durante vínculo pendente

- Enquanto o professor aguarda convite/aprovação para uma escola existente, o sistema permitirá apenas concluir o próprio cadastro e acompanhar o estado. Turmas, alunos, publicações, atribuições, consultas escolares e exportações permanecerão bloqueadas até o vínculo ativo.
- Ao criar uma escola nova, o professor receberá vínculo ativo e os papéis de Professor e Administrador da Escola na mesma transação, podendo criar turmas e alunos após o cadastro.
- O QA aceitou o protótipo UX atualizado com ajustes menores de acessibilidade/semântica; o UX foi acionado para aplicá-los e tornar o contexto de vínculo pendente visualmente inequívoco.
- Fonte: parecer do gerente, QA e decisão consolidada do usuário; status: regra confirmada, protótipo em ajuste final.

## 07/11/2026 — Encerramento do MT-04

- As oito histórias do épico (`PER-31` a `PER-38`) foram encerradas no Linear.
- A implementação e validação cobrem fundação de tenant/escola, vínculos, autorização server-side, sessões revogáveis, auditoria, aplicações/participações/tentativas, visibilidade e arquivamento, retenção de áudio, exportação segura sem áudio, capability do Administrador Geral e protótipos UX alinhados às telas reais.
- O gate MySQL foi realizado no container existente, com backup e schema derivado preservado; os cenários mutáveis não tocaram o banco compartilhado. O QA aceitou funcionalmente o PER-37 e visualmente o PER-38.
- Limitações mantidas explícitas: a suíte independente do QA não foi repetível no Windows por incompatibilidade VSTest 17.14.1 × TestHost/TestPlatform 18.0.1; o Fullstack reportou 101/101 na execução com SDK9. A recomendação não bloqueante do protótipo é completar headings semânticos/`aria-label` do `aside` se houver nova iteração visual.
- Fonte: Linear `PER-30`/`PER-31`–`PER-38`, código, testes, Local App Runner, Fullstack, QA e gerente de projetos; status: MT-04 concluído com limitações documentadas.

## 07/11/2026 — Correção do reset pós-cadastro e nova história de autenticação

- O QA reproduziu que o cadastro retornava HTTP 200, mas `Cadastro.vue` executava redirecionamento automático para `/login` após 2,5 segundos; a tela desmontava e a confirmação desaparecia.
- O Fullstack removeu o timer/redirecionamento automático, manteve a confirmação persistente, adicionou o botão explícito **Ir para login**, bloqueou reenvio duplicado e limpou senha/confirmação da memória, preservando nome/login/matrícula.
- O build frontend passou com 684 módulos. O QA repetiu o fluxo no Playwright: URL permaneceu `/cadastro` após 30 segundos, alerta permaneceu visível, houve exatamente um POST, e o botão navegou uma vez para `/login`.
- Foi criada a história Linear `PER-41` para separar login/cadastro de Professor e Aluno. O protótipo `login-cadastro-per38.html` apresenta Professor por e-mail/senha/escola e Aluno por matrícula/código/PIN; a implementação aguarda aprovação explícita do usuário.
- Fonte: código, QA Playwright, Fullstack, gerente e Linear `PER-41`; status: correção implementada e validada, separação de autenticação planejada.

## 07/11/2026 — Cadastro de alunos em lote e PIN criado no primeiro acesso

- Decisão confirmada: o professor ou Administrador da Escola cadastra alunos pela administração de usuários, individualmente ou em lote.
- O lote pode ser preenchido em uma página em formato de planilha e também pode ser pré-preenchido pela importação da planilha de presença da turma. O professor revisa e corrige os dados manualmente antes de confirmar a gravação.
- Nome e número na chamada são obrigatórios para o pré-cadastro. A matrícula é opcional, mas altamente recomendada.
- O código de cadastro segue a composição escola–turma–número na chamada. No primeiro acesso, o aluno informa matrícula ou código de cadastro e cria o próprio PIN; professor e administrador não criam nem visualizam esse PIN.
- Fontes afetadas: `docs/epicos/multi-tenant-mt02.md` e `docs/REFERENCIAL_PROJETO.md`. Status: decisão confirmada, implementação ainda não autorizada até aprovação das telas UX.

## 07/11/2026 — Formatos confirmados para importação de presença

- Decisão confirmada: a importação de presença aceitará arquivos CSV, XLS e XLSX. TSV não faz parte do escopo.
- O fluxo permanece seleção do arquivo, mapeamento das colunas, revisão dos alunos, confirmação e geração dos códigos/convites.
- Fonte: decisão explícita do usuário e Linear `PER-41`; status: escopo confirmado, implementação pendente.

## 07/11/2026 — Escolha explícita de escola no cadastro do professor

- Decisão confirmada: a tela de cadastro do professor deve oferecer as opções **criar nova escola** ou **selecionar escola existente**.
- Ao criar uma escola, o professor recebe vínculo ativo e o papel de Administrador da Escola.
- Ao selecionar uma escola existente, o vínculo fica pendente até aprovação; operações escolares permanecem bloqueadas durante a espera.
- Fontes afetadas: `docs/epicos/multi-tenant-mt02.md` e `docs/REFERENCIAL_PROJETO.md`. Status: regra confirmada, implementação/validação visual pendentes.

## 07/11/2026 — Bloqueio de acesso enquanto vínculo escolar aguarda aprovação

- Decisão confirmada: ao selecionar uma escola existente, o professor fica pendente e não recebe sessão de uso, não entra na plataforma e não acessa dados/operações escolares até a aprovação.
- O acompanhamento do próprio pedido deverá ocorrer por mecanismo específico, sem transformar a pendência em acesso autenticado à aplicação.
- Fonte: decisão explícita do usuário; status: regra confirmada, contrato técnico de acompanhamento pendente.

## 07/11/2026 — Consolidação do fluxo simples de cadastro do professor

- Confirmado que o professor que cria uma escola acumula os papéis de Professor e Administrador da Escola.
- Confirmado que a escola criada fica ativa imediatamente.
- Para escola existente, haverá solicitação de entrada para aprovação; “sem convite separado” significa que não haverá uma segunda etapa de convite enviada por outro canal ou fluxo distinto: a própria solicitação pendente será aprovada pelo administrador da escola.
- O fluxo pendente será simples: mensagem de confirmação, sem token público de acompanhamento nesta etapa. Enquanto aguarda, o professor não recebe sessão nem acessa a plataforma.
- Atualizado `AGENTS.md` para exigir consulta prévia ao `docs/REFERENCIAL_PROJETO.md` e às especificações normativas antes de encaminhar dúvidas de escopo.

## 07/11/2026 — Confirmação do cadastro em página dedicada

- Após cadastro válido, o formulário agora é substituído por uma página dedicada de confirmação, com mensagem de aprovação pendente e ação **Ir para login**.
- A confirmação de senha é validada antes do envio; divergências exibem mensagem acessível sem disparar o POST.
- Após o envio, senhas não permanecem expostas no estado visual.
- QA Playwright aprovou os dois comportamentos; console sem erros funcionais.
- Fonte: código, Fullstack, QA Playwright e Linear `PER-41`; status: implementado e validado.

## 07/11/2026 — Fluxo “Cadastre-se como aluno” para conta pré-cadastrada

- Decisão confirmada: a tela oferece **Cadastre-se como aluno**, mas o fluxo apenas localiza uma conta pré-cadastrada pelo professor ou Administrador da Escola.
- O aluno informa matrícula ou código de cadastro; se encontrado, cria o próprio PIN e segue para a plataforma. Se não encontrado, recebe o aviso para falar com o professor ou administrador da escola.
- Fontes afetadas: `docs/epicos/multi-tenant-mt02.md`, `docs/REFERENCIAL_PROJETO.md` e `PER-41`. Status: implementado e validado por Playwright em 07/11/2026.

## 07/11/2026 — Confirmação do fluxo de ativação do aluno em duas telas

- Decisão confirmada: o aluno não cria uma conta nova. Na primeira tela de **Cadastre-se como aluno**, informa somente a matrícula ou o código de cadastro.
- Quando o pré-cadastro é localizado, a aplicação navega para a segunda tela de PIN já existente. O aluno cria o próprio PIN nessa tela e só então entra na plataforma.
- Quando não há pré-cadastro, deve ser exibida a mensagem: **Você ainda não foi pré-cadastrado na plataforma. Fale com seu professor ou administrador da sua escola.**
- Fontes afetadas: `docs/epicos/multi-tenant-mt02.md` e `docs/REFERENCIAL_PROJETO.md`. Status: implementado e validado por Playwright em 07/11/2026.

## 07/11/2026 — Login automático após criação do PIN do aluno

- Corrigido o fluxo para que a segunda tela configure o PIN e faça uma única autenticação automática, sem repetir o login com o PIN já limpo.
- A aplicação redireciona o aluno para `/aluno` após a criação do PIN; o campo informa o mínimo de quatro caracteres e apresenta erros de validação de forma clara.
- QA Playwright MCP confirmou: verificação HTTP 200, criação do PIN HTTP 200, login automático HTTP 200, navegação para `/aluno` e carregamento de `/api/Alunos/meus-ditados` HTTP 200. Não houve segunda chamada com PIN vazio.
- O caso de identificador inexistente continua exibindo a mensagem exata de não pré-cadastro. A validação usou schema isolado, sem alterar o banco compartilhado.
- Fonte: código do frontend, Fullstack, Runner, QA Playwright e Linear `PER-41`; status: implementado e validado.
