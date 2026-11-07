# Ditado — Relatório vivo da aplicação

**Versão:** V0  
**Data:** 07/11/2026  
**Status:** prévia estruturada para revisão  
**Natureza:** documentação evolutiva; não é uma versão acadêmica final

> Este documento usa o PDF de referência apenas para organização, nível de formalidade e tipos de diagramas. O estado factual da aplicação é reconstruído a partir do código, dos testes, das especificações, do Linear e dos relatórios de ciclo.

## 1. Como ler este documento

As afirmações recebem uma destas classificações:

- **Confirmado/implementado:** sustentado por código, teste, decisão explícita ou evidência de validação.
- **Planejado:** descrito em especificação, história ou Linear, mas não comprovado como implementação atual.
- **Hipótese/recomendação:** alternativa técnica ou interpretação que ainda depende de decisão.
- **Legado:** informação preservada por compatibilidade ou documentação histórica.
- **Desconhecido:** lacuna que impede uma afirmação confiável.

O relatório deve ser atualizado quando uma mudança alterar qualquer seção, requisito, fluxo, contrato, dado, decisão ou diagrama aqui registrado.

## 2. Identificação do trabalho

- **Sistema:** Ditado.
- **Tipo:** aplicação web para gerenciamento e realização de atividades de ditado escolar.
- **Escopo técnico:** backend .NET, frontend Vue e banco MySQL.
- **Repositório documentado nesta versão:** backend em `Ditado - backend (1)`.
- **Frontend relacionado:** repositório `ditado-main`.
- **Curso, instituição, integrantes, orientação e disciplina:** **[PENDENTE DE CONFIRMAÇÃO]**.
- **Norma acadêmica e prazo de entrega:** **[PENDENTE DE CONFIRMAÇÃO]**.

## 3. Visão geral e propósito

O Ditado apoia professores na criação e aplicação de atividades de ditado e permite que alunos realizem as atividades, recebam correção e acompanhem seus resultados. A aplicação também prevê classificação de erros ortográficos, catálogo de erros, feedbacks personalizados e relatórios de desempenho.

O MVP documentado no README concentra-se na aplicação online de ditados, correção automática e acompanhamento de resultados. OCR para respostas manuscritas, suporte mobile avançado, arquitetura multi-tenant e análises mais sofisticadas aparecem como evoluções ou planejamento, não como funcionalidades integralmente confirmadas no código atual.

## 4. Problema, justificativa e objetivos

### 4.1 Problema

O sistema busca reduzir o trabalho manual de criação, aplicação, correção e acompanhamento de ditados escolares, mantendo o registro das respostas e dos tipos de erro observados.

**Status:** confirmado em nível geral; a formulação acadêmica detalhada do problema ainda precisa ser validada pelos autores do trabalho.

### 4.2 Justificativa

Uma aplicação centralizada pode apoiar a organização de turmas e ditados, preservar o histórico das tentativas e fornecer informações para acompanhamento pedagógico.

**Status:** recomendação de redação; dados quantitativos, estudos de campo e resultados com usuários ainda não foram fornecidos.

### 4.3 Objetivo geral

Documentar e evoluir uma aplicação web capaz de gerenciar atividades de ditado, registrar respostas, calcular a correção objetiva e apoiar a análise do desempenho de alunos e turmas.

**Status:** formulação provisória para validação.

### 4.4 Objetivos específicos

- autenticar usuários e controlar permissões;
- organizar alunos e turmas;
- criar, visualizar, disponibilizar e realizar ditados;
- corrigir respostas e calcular nota;
- registrar operações observáveis e tipos de erro quando houver regra segura;
- estruturar um catálogo de erros;
- apoiar feedbacks e relatórios futuros;
- preservar histórico, rastreabilidade e integridade dos dados.

## 5. Usuários, atores e permissões

### 5.1 Estado funcional atual

- **Aluno:** acessa ditados disponíveis, realiza atividades e envia respostas.
- **Professor:** cria e gerencia ditados, turmas e alunos, acompanha resultados e utiliza funcionalidades de publicação previstas para o catálogo.
- **Administrador legado:** possui permissões administrativas amplas no modelo atual.

Esses papéis são confirmados no código e na documentação introdutória, mas o modelo atual ainda não implementa integralmente o isolamento por escola.

### 5.2 Modelo planejado de multi-tenant

- cada tenant representa uma escola;
- professor e aluno ficam associados a uma escola conforme as decisões vigentes;
- Administrador da Escola atua no escopo escolar;
- Administrador Geral/Técnico possui capacidades globais enumeradas;
- consultas e exportações devem informar escola ou conjunto de escolas-alvo e gerar auditoria;
- visibilidades planejadas: Público geral, Instituição e Turmas.

**Status:** decisões e contratos de planejamento. Não declarar como implementação atual sem migration, entidade, autorização server-side e testes correspondentes.

O refinamento MT-04 detalha, como proposta técnica, escolas, vínculos, papéis, participação em turmas, revisões de conteúdo, publicações tipadas, aplicações entre escolas, seleções de erros, sessões revogáveis, auditoria, constraints, migração e cenários de verificação em MySQL. As decisões registradas incluem a continuidade de atividades já atribuídas após mudança de visibilidade, a possibilidade de aluno ingressante receber atividade da turma autorizada, a distinção entre arquivamento e visibilidade, a continuidade das atividades após suspensão do professor e a criação de aplicação própria quando uma escola adota conteúdo público de outra.

O MT-04 está rastreado no Linear pela issue [PER-30 - Persistência, isolamento e histórico](https://linear.app/joaomoutinho/issue/PER-30/mt-04-persistencia-isolamento-e-historico). O estado atual permanece como refinamento técnico, sem implementação de código ou migration.

Também está confirmado que uma turma não pode receber aplicações duplicadas do mesmo ditado. Novos alunos entram na aplicação existente e só acessam o ditado após atribuição. Um aluno pode realizar o ditado várias vezes; cada tentativa possui resultado próprio e referencia a mesma aplicação e a mesma revisão imutável.

Para manter a solução simples e incremental, um ditado aplicado ou iniciado fica imutável nesta etapa. Correções de texto, áudio ou resposta esperada exigem a criação de um novo ditado; não será adotado versionamento complexo do mesmo cadastro neste momento.

O arquivamento bloqueia novas atribuições, mas não interrompe novas tentativas de alunos que já estejam vinculados à aplicação. A remoção do aluno da turma bloqueia novas tentativas para esse aluno, preservando o histórico já registrado.

**Status:** planejamento técnico/proposto, sem implementação, migration ou alteração de dados vivos.

## 6. Funcionalidades atuais evidenciadas

### 6.1 Autenticação e usuários

- registro e login;
- autenticação com JWT Bearer;
- claims de identificação, nome, papel e login;
- aprovação e gerenciamento de usuários em rotas administrativas.

### 6.2 Turmas e alunos

- cadastro e gerenciamento de turmas;
- associação de alunos;
- consulta de informações de turmas e desempenho.

### 6.3 Ditados

- criação e gerenciamento de ditados;
- áudio de leitura;
- segmentos com texto esperado e ordem;
- categorias associadas;
- atribuição a turmas;
- realização por alunos;
- visualização de detalhes por professor/administrador, com gabarito protegido de alunos.

### 6.4 Correção e resultados

- comparação entre resposta e texto esperado;
- cálculo de acerto e nota por código;
- persistência de tentativas e respostas por segmento;
- classificação de zero ou mais tipos quando regras independentes se aplicam;
- preservação de tipos históricos;
- consultas de resultados e evolução.

Os relatórios do professor foram ajustados para usar o melhor resultado principal por aluno, considerando nota, data e métricas de erros, em vez de assumir automaticamente a primeira tentativa. Todas as tentativas continuam preservadas. A investigação detalhada das métricas foi encaminhada para o PER-40.

Fechamento do PER-34: a migration foi aplicada uma vez após backup verificável; contagens legadas foram preservadas e as novas tabelas permaneceram vazias. O build apresentou zero erros, com 9/9 testes isolados e 21/21 testes de `ProfessorService` aprovados. O QA aceitou a entrega. `ParticipacaoTurma` ainda não possui consumidores nesta fatia, e a investigação de métricas permanece separada no PER-40. No Linear, PER-34 está Done e PER-35 está In Progress.

### 6.5 Catálogo de erros

O código possui uma implementação inicial de publicação/consulta mínima do catálogo técnico de erros. Os nomes internos `Marketplace*` permanecem por compatibilidade, mas o nome de produto vigente é **Catálogo de erros**.

O ciclo CAT-02 registra publicação, persistência, validações, testes automatizados e ajustes visuais. A aprovação funcional independente e o status atual no Linear devem ser conferidos antes da versão final do relatório.

### 6.6 Funcionalidades planejadas

- consulta ampla do catálogo;
- edição, arquivamento e restauração;
- seleção de erros aplicáveis a um ditado;
- importação JSON;
- classificação usando regras selecionadas;
- feedback personalizado de aluno e professor;
- dashboard e exportação PDF;
- backup e restauração integral;
- isolamento multi-tenant.

O cadastro de alunos em lote e a importação de listas de presença em CSV, XLS e XLSX já fazem parte do fluxo funcional atual. O refinamento da extração de matrícula, da exclusão de linhas inválidas e do mapeamento manual de colunas está planejado separadamente na [PER-43](https://linear.app/joaomoutinho/issue/PER-43/importacao-de-chamada-melhorar-extracao-de-matricula-e-edicao-de).

## 7. Escopo incluído e excluído

### Incluído na documentação

- estado atual implementado e suas evidências;
- contratos e decisões de planejamento;
- arquitetura, persistência, fluxos e permissões;
- testes, QA, limitações e histórico;
- diagramas editáveis e sua rastreabilidade;
- fundamentação pedagógica aplicável ao catálogo e à classificação.

### Fora do estado implementado confirmado

- entidade Escola/TenantId e isolamento completo;
- visibilidade multi-tenant aplicada ao código;
- auditoria integral;
- sessões revogáveis;
- QR/PIN e presença; a importação CSV/XLS/XLSX está funcional, mas ainda possui melhorias planejadas na PER-43;
- fallback LLM funcional;
- OCR para respostas manuscritas;
- qualquer resultado pedagógico não sustentado por avaliação específica.

## 8. Requisitos e regras de negócio

### 8.1 Requisitos funcionais atuais ou evidenciados

- autenticar usuários;
- cadastrar e gerenciar turmas;
- criar e aplicar ditados;
- permitir a realização por alunos;
- corrigir respostas e calcular nota;
- consultar resultados;
- visualizar detalhes de ditados conforme papel;
- publicar definições iniciais no catálogo técnico.

### 8.2 Requisitos planejados

- permitir seleção de erros por ditado;
- impedir edição de erro após sua primeira associação;
- arquivar sem apagar histórico;
- classificar múltiplos erros quando houver correspondência confiável;
- manter correção objetiva mesmo se serviços externos falharem;
- gerar feedback somente sob solicitação explícita;
- exportar PDF refletindo filtros e gráficos exibidos;
- manter retenção configurável dos feedbacks.

### 8.3 Regras relevantes

- nota e acerto são calculados pelo código, não pelo LLM;
- diferenças de caixa podem ser relevantes para a correção;
- apenas espaços externos são ignorados conforme a decisão registrada;
- omissão, acréscimo e substituição são operações distintas nos novos resultados;
- transcrição da fala e hipercorreção não são inferidas automaticamente na entrega COR-01;
- respostas e resultados históricos devem ser preservados;
- recomendações de agentes não encerram decisões de escopo.

## 9. Arquitetura e tecnologias

### 9.1 Camadas

- `Ditado.API`: entrada HTTP, controllers, autenticação e Swagger;
- `Ditado.Aplicacao`: serviços, DTOs e orquestração dos casos de uso;
- `Ditado.Dominio`: entidades, enums e regras de domínio;
- `Ditado.Infra`: DbContext, configurações, persistência e migrations;
- `Ditado.Testes`: testes unitários e de integração.

### 9.2 Tecnologias registradas

- C# / .NET 9;
- ASP.NET Core Web API;
- Entity Framework Core 9;
- MySQL 8;
- JWT Bearer;
- Swagger/OpenAPI;
- xUnit e FluentAssertions;
- Vue/Vite no frontend relacionado;
- GitHub Actions e Azure App Service conforme README.

### 9.3 Componentes principais

- controllers em `Ditado.API/Controllers/`;
- serviços em `Ditado.Aplicacao/Services/`;
- entidades em `Ditado.Dominio/Entidades/`;
- enums em `Ditado.Dominio/Enums/`;
- persistência em `Ditado.Infra/Data/` e `Configurations/`;
- migrations em `Ditado.Infra/Migrations/`;
- testes em `Ditado.Testes/`.

## 10. API, persistência e modelo de dados

O domínio atual contém, entre outras, as entidades `Usuario`, `Turma`, `Ditado`, `DitadoSegmento`, `RespostaDitado`, `RespostaSegmento`, `RespostaSegmentoTipoErro`, `Categoria`, `DitadoCategoria`, `TurmaDitado` e `ErroCatalogo`.

O modelo planejado acrescenta ou altera conceitos como `DitadoErro`, vínculos de escola, escopos de publicação, classificações persistidas, feedbacks gerados e auditoria. Esses elementos devem ser marcados como proposta até que existam código, migration e testes.

**Recomendação técnica registrada para o MT-04:** manter desenho incremental, sem criar `DitadoRevisao` ou `AudioVersao` nesta etapa; congelar o `Ditado` na primeira atribuição e exigir novo `DitadoId` para correções; evoluir `TurmaDitado` para uma aplicação com identidade e restrição única duradoura por `(TurmaId, DitadoId)`; manter múltiplas tentativas em `RespostaDitado`, seleção de erros por aplicação, FKs e índices objetivos, transações curtas e testes MySQL. Esta recomendação não é implementação nem decisão de escopo.

Contratos de endpoints e DTOs devem ser documentados a partir do Swagger e do código atual. O relatório não deve inventar rotas a partir de diagramas ou contratos ainda em revisão.

## 11. Fluxos principais

1. Usuário realiza login e recebe token.
2. Professor gerencia turmas e alunos.
3. Professor cria ou disponibiliza um ditado.
4. Aluno acessa o ditado no contrato próprio de realização, sem receber o gabarito.
5. Aluno envia respostas.
6. API compara respostas, calcula acerto/nota e classifica quando aplicável.
7. Resultado é persistido e disponibilizado conforme o papel.
8. Professor consulta desempenho e, em evoluções planejadas, solicita feedback ou relatórios.

Fluxos de catálogo, multi-tenant, backup e LLM permanecem parcialmente planejados e precisam de diagramas próprios quando os contratos forem estabilizados.

## 12. Fundamentação pedagógica

Os três PDFs em `docs/referencias/` sustentam discussões sobre erros ortográficos, fonologia, escrita infantil e categorias de análise.

O relatório deve separar:

- diferença observável entre resposta e esperado;
- operação textual, como substituição, omissão ou acréscimo;
- interpretação pedagógico-linguística sobre a possível motivação do erro.

Uma troca de letras, isoladamente, não prova transcrição da fala. A classificação causal deve ser tratada como hipótese quando não houver evidência suficiente.

## 13. Segurança, privacidade e operação

- segredos devem vir de configuração segura, nunca do frontend ou do repositório;
- endpoints devem validar identidade, papel e vínculo antes de liberar dados;
- respostas do LLM, quando houver, devem ser tratadas como conteúdo não confiável;
- falha de serviço externo não pode desfazer a correção tradicional;
- dados de menores e respostas devem receber proteção compatível com o contexto educacional;
- backup deve incluir o banco inteiro e coordenar arquivos externos;
- testes de restauração e parâmetros finais dependem do ambiente universitário.

Decisões confirmadas do MT-04 para a evolução multi-tenant:

- usar MySQL/InnoDB compartilhado, com isolamento por `EscolaId` e autorização validada no servidor;
- aplicar retenção de 24 horas antes da limpeza em duas fases para áudios não atribuídos;
- nunca remover áudios referenciados por aplicação, tentativa ou histórico;
- realizar exportação por download direto, sem áudio, com cursor/streaming limitado e limites de linhas, tamanho e tempo;
- recusar exportações acima dos limites;
- não usar arquivo temporário nem envio por e-mail nesta etapa.

## 14. Diagramas

O índice completo está em [`indice-diagramas.md`](indice-diagramas.md).

### Diagrama existente

- [`diagramas/catalogo-er.md`](diagramas/catalogo-er.md): modelo ER proposto do catálogo; status: revisão pendente, não representa integralmente o schema implementado.

### Diagramas previstos

- contexto e arquitetura geral;
- casos de uso por ator;
- fluxo de realização e correção;
- sequência de submissão de resposta;
- modelo ER atual e planejado, separados quando necessário;
- arquitetura de componentes e implantação;
- fluxos de catálogo, feedback, relatórios e multi-tenant após validação dos contratos.

## 15. Metodologia, desenvolvimento e validação

O projeto é organizado por épicos e histórias no Linear. O fluxo de trabalho exige documentação e diagramas antes da implementação de épicos com dependências ou decisões bloqueadoras.

### Sequência planejada do MT-04

1. baseline, documentação, ER e contratos;
2. inventário e backup/restauração em cópia;
3. fundação de `EscolaId`, vínculos e integridade;
4. autorização, sessões e auditoria;
5. aplicação única por turma/ditado e tentativas;
6. visibilidade e arquivamento;
7. limpeza de áudio após 24 horas e exportação direta sem áudio;
8. testes MySQL;
9. QA;
10. UX e protótipos antes das telas.

Dependências e riscos registrados: autorização antes de rotas tenant-aware; aplicação antes de CAT-05/COR-01; backup e armazenamento antes da limpeza de áudio; isolamento entre escolas, preservação do histórico, revogação, uso de disco, validação dos contratos e protótipos UX antes da implementação das telas. A sequência não autoriza implementação por si só.

### Decomposição Linear do MT-04

- **PER-31:** inventário e baseline;
- **PER-32:** fundação de `EscolaId`;
- **PER-33:** autorização, sessões e auditoria;
- **PER-34:** aplicações, participação e tentativas;
- **PER-35:** visibilidade e arquivamento;
- **PER-36:** áudio e exportação;
- **PER-37:** testes MySQL e QA;
- **PER-38:** protótipos UX.

PER-31 está em andamento e as demais etapas estão planejadas no Linear, com dependências registradas entre elas.

### Baseline registrado do PER-31

- branch de trabalho: `codex/mt04-foundation-tenant`;
- inventário somente leitura confirmou que `Escolas` e `VinculosEscolares` ainda não existem;
- histórico do MySQL conferido até `20261106051829_PublicarErroMarketplace`;
- backup lógico privado pré-migration concluído com `mysqldump --single-transaction --no-tablespaces --routines --triggers`;
- nenhuma migration, dado ou serviço foi alterado neste baseline;
- implementação permanece em andamento, com bloqueio técnico de `pending model changes` por divergência entre assembly de inicialização/API e snapshot EF;
- fundação e migration permanecem locais até a validação.

### Atualização do PER-31 e PER-32

Em 07/11/2026, PER-31 e PER-32 foram registrados como concluídos. A migration `20261107044724_AdicionarFundacaoTenant` foi aplicada uma vez após backup e validação do modelo. As tabelas `Escolas` e `VinculosEscolares` foram criadas vazias; FKs, índices e restrições únicas foram validados, e os dados legados foram preservados. API e frontend responderam HTTP 200. O Fullstack registrou `MarketplaceControllerTests` com 33/33 aprovados. O QA confirmou schema e regressão sem achado funcional, com limitação do runner por ausência de `testhost.dll`. O PER-33 foi iniciado.

Durante o PER-33 ocorreu um incidente controlado de migration: `dotnet ef migrations remove --no-build --force` utilizou um assembly anterior e reverteu localmente apenas as tabelas novas vazias `Escolas`/`VinculosEscolares` e o respectivo histórico. Nenhum dado legado foi alterado. Em seguida, a migration `20261107050300_AdicionarFundacaoTenant` foi aplicada após backup e revisão SQL. Foram criadas `AuditoriaEventos`, `Escolas`, `SessoesUsuarios` e `VinculosEscolares`, todas com zero registros; não houve `DROP`, `UPDATE` ou backfill. O histórico e `has-pending-model-changes` foram confirmados. A suíte/build atual não foi concluída por limitações ambientais do runner/SDK, incluindo `SecurityException` de telemetria; não foram reportados erros de código. O QA oficial foi acionado e o PER-34 permanece bloqueado até validação.

Na evolução do PER-33, foi incluído `GET /api/usuarios/contexto-escolar`, com validação server-side por vínculo e escola ativa, além de auditoria de decisões permitidas e negadas. Foram preparados testes InMemory isolados para serviço e revogação. O QA revisou os testes, mas o runner/SDK não conseguiu executá-los por falha de workloads e saída vazia, resultando em zero testes executados. Permanecem lacunas de E2E autenticado, controller/claims, escola inativa e concorrência. O PER-33 continua In Progress e o PER-34 permanece bloqueado.

Fechamento posterior do PER-33: o QA aceitou a entrega após confirmação do schema/migration e aprovação de 6/6 testes isolados de serviço e controller/header. O build .NET 9 ficou estável, com zero erros e apenas avisos preexistentes. Permanecem como limitações futuras, sem bloqueio, a cobertura runtime específica para claims/JTI inválido, expiração e concorrência. No Linear, PER-33 foi movido para Done e PER-34 está In Progress.

### Decisão confirmada do PER-34

Foi adotada compatibilidade temporária: o legado `TurmaDitado`/`TurmaAluno`/`RespostasDitados` permanece preservado e legível; novas escritas ocorrerão exclusivamente no modelo `AplicacaoDitado`/`Participacao*`, sem escrita dupla. A limpeza do banco não está autorizada. Qualquer migration depende de adaptadores e testes.

Decisão aprovada no PER-35: `Turma.EscolaId` será inicialmente nullable. Não haverá backfill automático das turmas legadas; novas turmas e novas publicações exigirão escola definida. Registros antigos com valor nulo permanecerão legíveis até que exista mapeamento explícito.

Após o gate de `Turma.EscolaId`, com migration aplicada e aceitação do QA, o PER-35 entrou na implementação funcional de visibilidade e arquivamento para Ditados e Catálogo de erros. Permanecem aguardando os arquivos, testes, revisão do QA e novo gate de backup/migration.

No PER-35, a implementação foi corrigida para comportamento fail-closed: vínculo, papel e ator são obrigatórios; Administrador sem vínculo é negado; restauração exige autorização; e concorrência é controlada. A validação final está em andamento. Permanece pendente a definição explícita da capability/claim do Administrador Geral/Técnico; a ausência de vínculo não deve conceder essa permissão por inferência.

Decisão aprovada: o Administrador Geral/Técnico será identificado por capability explícita persistida, desativada por padrão. Acesso global não será inferido pela ausência de vínculo escolar. Implementação e validação permanecem pendentes.

Atualização de implementação: `Usuario.AdministradorGeral` foi criado com valor padrão `false`, com claim JWT e guards fail-closed. Nenhuma conta existente foi promovida. Uma migration aditiva foi gerada, mas ainda não aplicada. Foram registrados 6/6 testes aprovados e build sem erros; permanecem pendentes QA e gate de backup.

O PER-35 foi encerrado após QA e gate pós-DDL. O PER-36, dedicado a áudio e exportação segura, foi iniciado. Permanecem válidas as decisões de limpeza em duas fases após 24 horas, preservação de áudios referenciados e exportação sem áudio com limites.

As evidências disponíveis incluem relatórios de ciclo e QA para CAT-02, COR-01 e PER-25. Elas devem ser interpretadas como evidência daquele ciclo, com data e limitações, e não como garantia permanente de todo o sistema.

## 16. Decisões, riscos e pendências

### Decisões confirmadas

- correção objetiva e nota permanecem independentes de LLM;
- tipos de operação podem ser múltiplos e separados;
- transcrição da fala e hipercorreção foram retiradas da inferência automática desta entrega;
- Catálogo de erros é o termo de produto vigente;
- decisões de multi-tenant devem tratar escola como unidade organizacional, conforme referencial consolidado.

### Pendências

- dados acadêmicos e institucionais do TCC;
- contrato final de multi-tenant e MT-04;
- estratégia transacional, índices, versões de áudio, retenção e migração efetiva do MT-04;
- seleção persistida de erros por ditado;
- correspondência entre regras do catálogo e classificação existente;
- provedor, modelo, custo e fluxo do LLM;
- retenção e regeneração de feedback;
- contrato de importação JSON;
- parâmetros operacionais de backup;
- definição de sessão para relatórios;
- confirmação dos status atuais no Linear quando houver divergência entre relatórios.

### Riscos e dívida técnica

- nomenclatura técnica legada `Marketplace*`;
- documentação histórica com propostas incompatíveis;
- dependência entre catálogo, classificação, feedbacks e relatórios;
- ausência atual de isolamento multi-tenant no código;
- limitações de runtime/build registradas nos relatórios de QA;
- risco de transformar inferência pedagógica em regra automática sem evidência.

## 17. Histórico desta documentação

| Versão | Data | Alteração |
|---|---|---|
| V0 | 07/11/2026 | Criação da primeira prévia do relatório vivo, com estado atual, planejamento, fontes, lacunas e diagramas previstos. |
| V0.1 | 07/11/2026 | Inclusão do refinamento planejado do MT-04 sobre persistência, isolamento, histórico, aplicações entre escolas, sessões e auditoria. |
| V0.2 | 07/11/2026 | Registro da decisão confirmada de aplicação única por turma, novas tentativas por aluno e referência à mesma aplicação/revisão imutável. |
| V0.3 | 07/11/2026 | Registro da decisão confirmada de imutabilidade do ditado aplicado/iniciado e substituição por novo cadastro quando houver correção de conteúdo. |
| V0.4 | 07/11/2026 | Inclusão do rastreamento do MT-04 na issue Linear PER-30, ainda em refinamento técnico e sem implementação. |
| V0.5 | 07/11/2026 | Registro do parecer técnico incremental do MT-04, separado de decisões confirmadas e sem implementação. |
| V0.6 | 07/11/2026 | Registro da decisão sobre arquivamento, novas tentativas e remoção de aluno da turma no MT-04. |
| V0.7 | 07/11/2026 | Registro das decisões confirmadas sobre MySQL/InnoDB, isolamento por EscolaId, retenção de áudios e limites de exportação do MT-04. |
| V0.8 | 07/11/2026 | Registro da sequência planejada do MT-04, suas dependências e riscos, sem autorização automática de implementação. |
| V0.9 | 07/11/2026 | Refinamento da sequência do MT-04 com backup/restauração, EscolaId, autorização, dependências de CAT-05/COR-01 e limpeza de áudio. |
| V0.10 | 07/11/2026 | Registro da decomposição do MT-04/PER-30 em PER-31 a PER-38, com status e dependências planejadas. |
| V0.11 | 07/11/2026 | Registro factual do baseline do PER-31, backup lógico, estado do schema e bloqueio técnico de validação. |
| V0.12 | 07/11/2026 | Registro da conclusão documentada do PER-31/PER-32, aplicação da migration, validações e início do PER-33. |
| V0.13 | 07/11/2026 | Registro do incidente controlado de migration no PER-33, impacto limitado às tabelas novas vazias e gate de QA antes do PER-34. |
| V0.14 | 07/11/2026 | Atualização do PER-33 após aplicação da migration 20261107050300, criação das tabelas vazias, validações e bloqueio do PER-34 por QA. |
| V0.15 | 07/11/2026 | Registro do endpoint de contexto escolar, autorização server-side, auditoria, testes preparados e limitações de execução do PER-33. |
| V0.16 | 07/11/2026 | Registro do fechamento do PER-33 pelo QA, 6/6 testes aprovados, build estável e transição do PER-34 para In Progress. |
| V0.17 | 07/11/2026 | Registro da compatibilidade temporária confirmada no PER-34, sem escrita dupla e sem limpeza do banco. |
| V0.18 | 07/11/2026 | Registro do ajuste dos relatórios do professor para melhor resultado principal, preservação das tentativas e investigação posterior no PER-40. |
| V0.19 | 07/11/2026 | Registro do fechamento do PER-34, validações do QA, preservação do legado e transição do PER-35 para In Progress. |
| V0.20 | 07/11/2026 | Registro da decisão do PER-35 sobre EscolaId nullable, ausência de backfill automático e exigência para novos registros. |
| V0.21 | 07/11/2026 | Registro do início da implementação funcional de visibilidade e arquivamento no PER-35 após o gate de Turma.EscolaId. |
| V0.22 | 07/11/2026 | Registro da correção fail-closed do PER-35 e da pendência explícita sobre capability/claim do Administrador Geral/Técnico. |
| V0.23 | 07/11/2026 | Registro da decisão aprovada de capability persistida e desativada por padrão para Administrador Geral/Técnico. |
| V0.24 | 07/11/2026 | Registro da implementação da capability AdministradorGeral, claim/guards fail-closed, migration ainda não aplicada e gates pendentes. |
| V0.25 | 07/11/2026 | Registro do encerramento do PER-35 e início do PER-36 sobre áudio e exportação segura. |

## 18. Referências principais

- [`README.md`](../../README.md)
- [`docs/REFERENCIAL_PROJETO.md`](../REFERENCIAL_PROJETO.md)
- [`Ditado/docs/ESPECIFICACAO_PG2.md`](../../Ditado/docs/ESPECIFICACAO_PG2.md)
- [`Ditado/docs/HISTORIAS_EPICO_CATALOGO.md`](../../Ditado/docs/HISTORIAS_EPICO_CATALOGO.md)
- [`Ditado/DOCUMENTACAO.md`](../../Ditado/DOCUMENTACAO.md) — contexto legado, sujeito a confronto com o código atual
- [`docs/referencias/README.md`](../referencias/README.md)
- [`docs/epicos/`](../epicos/)
- [`docs/relatorio-ciclo-cat02-2026-11-06.md`](../relatorio-ciclo-cat02-2026-11-06.md)
- [`docs/relatorio-ciclo-cor01-2026-11-05.md`](../relatorio-ciclo-cor01-2026-11-05.md)
- [`docs/relatorio-ciclo-per25-2026-11-06.md`](../relatorio-ciclo-per25-2026-11-06.md)
- Projeto Linear Ditado: https://linear.app/joaomoutinho/project/ditado-29348d0fee1d

## 19. Validação pendente da V0

- confirmar identificação acadêmica e padrão institucional;
- confirmar se a redação do problema e dos objetivos representa o trabalho dos autores;
- revisar o limite entre funcionalidades atuais e planejadas;
- validar os diagramas necessários para a primeira versão acadêmica;
- confirmar o status no Linear das entregas mencionadas;
- decidir se a próxima versão deve detalhar primeiro o produto atual ou um épico específico.
