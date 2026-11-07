# Referencial consolidado do projeto

**Atualizado em:** 06/11/2026  
**Finalidade:** apontar rapidamente para as fontes vigentes do planejamento, evitando redescoberta e divergência entre documentos.

## Como usar este arquivo

Consulte este índice antes de abrir novas histórias, revisar contratos ou responder dúvidas de escopo. A especificação global e as histórias do catálogo são as fontes normativas; contratos, relatórios e protótipos detalham entregas específicas. Este arquivo resume decisões aceitas, mas não substitui os documentos de origem.

## Fontes normativas

| Fonte | Uso | Observação |
|---|---|---|
| [`Ditado/docs/ESPECIFICACAO_PG2.md`](../Ditado/docs/ESPECIFICACAO_PG2.md) | Decisões globais, dependências, critérios transversais, segurança e operação | Fonte principal do planejamento técnico; a seção 11 registra a atualização mais recente. |
| [`Ditado/docs/HISTORIAS_EPICO_CATALOGO.md`](../Ditado/docs/HISTORIAS_EPICO_CATALOGO.md) | Mapa de histórias CAT-01 a CAT-10 e critérios de aceite | CAT-08 agora trata do backup e da restauração integral do banco. |
| [`README.md`](../README.md) | Visão geral do produto, tecnologias e estrutura da solução | Referência introdutória; não substitui as decisões de planejamento. |
| [`Ditado/DOCUMENTACAO.md`](../Ditado/DOCUMENTACAO.md) | Arquitetura e atores legados do backend | Útil para contexto técnico; a seção de papéis deve ser lida junto das decisões multi-tenant deste índice. |

## Contratos e evidências de entrega

- [`docs/epicos/catalogo-contrato.md`](epicos/catalogo-contrato.md): contrato de refinamento do CAT-09; contém propostas e pendências ainda em revisão.
- [`docs/epicos/multi-tenant-mt01.md`](epicos/multi-tenant-mt01.md): classificação inicial de dados e matriz de permissões do MT-01.
- [`docs/epicos/multi-tenant-mt02.md`](epicos/multi-tenant-mt02.md): cadastro, vínculos e ciclo de vida dos papéis do MT-02.
- [`docs/epicos/multi-tenant-mt03.md`](epicos/multi-tenant-mt03.md): contexto escolar, autorização e revogação do MT-03.
- [`docs/epicos/multi-tenant-mt04.md`](epicos/multi-tenant-mt04.md): persistência, isolamento e histórico do MT-04.
- [`docs/epicos/marketplace-cat02.md`](epicos/marketplace-cat02.md): contrato legado da publicação no **Catálogo de erros** (CAT-02); o nome do arquivo permanece por compatibilidade.
- [`docs/diagramas/catalogo-er.md`](diagramas/catalogo-er.md): modelo ER do catálogo.
- [`docs/relatorio-ciclo-cat02-2026-11-06.md`](relatorio-ciclo-cat02-2026-11-06.md): relatório do ciclo CAT-02.
- [`docs/relatorio-ciclo-cat09-2026-11-06.md`](relatorio-ciclo-cat09-2026-11-06.md): relatório do refinamento CAT-09.
- [`docs/prototipos`](prototipos): protótipos e referências visuais existentes.

## Planejamento no Linear

- [PER-26 — Multi-tenant — Isolar organizações e seus dados](https://linear.app/joaomoutinho/issue/PER-26/multi-tenant-isolar-organizacoes-e-seus-dados): épico de planejamento para isolamento por escola.
- [PER-27 — MT-01 — Definir fronteiras de dados e matriz de permissões](https://linear.app/joaomoutinho/issue/PER-27/mt-01-definir-fronteiras-de-dados-e-matriz-de-permissoes): primeira etapa de detalhamento do épico Multi-tenant.
- [PER-30 — MT-04 — Persistência, isolamento e histórico](https://linear.app/joaomoutinho/issue/PER-30/mt-04-persistencia-isolamento-e-historico): refinamento técnico da persistência, autorização, histórico e testes.
- MT-04 está decomposto no Linear em PER-31 a PER-38: inventário, fundação, autorização, aplicações, visibilidade, áudio/exportação, QA e UX.
- [PER-13 — CAT-08 — Backup e restauração integral do banco de dados](https://linear.app/joaomoutinho/issue/PER-13/cat-08-backup-e-restauracao-integral-do-banco-de-dados-enabler): enabler operacional de planejamento.

## Decisões aceitas

### Multi-tenant e papéis

- Cada tenant representa uma escola.
- Cada conta de professor pertence a uma escola. Para outra escola, usa-se outra conta; substitui a decisão anterior de múltiplos vínculos.
- O cadastro é aberto: qualquer pessoa pode se cadastrar como professor e criar seus próprios ditados; ao criar uma escola, torna-se seu Administrador da Escola. O Administrador Geral administra os Administradores da Escola.
- No cadastro, o professor escolhe uma escola existente ou cria uma nova; não há conta de professor sem escola.
- A tela de cadastro deve apresentar essas duas opções de forma explícita: **criar uma nova escola** ou **selecionar uma escola existente**. Criar a escola ativa o vínculo e o papel inicial de Administrador da Escola; selecionar uma escola existente deixa o vínculo pendente até aprovação de um administrador da escola. Enquanto estiver pendente, o professor não recebe sessão de uso, não entra na plataforma nem acessa dados escolares; somente poderá acompanhar o próprio pedido por mecanismo específico.
- Uma escola mantém pelo menos um Administrador da Escola ativo. Para suspender ou remover o último, o Administrador Geral promove ou cria o substituto na mesma operação.
- Administrador da Escola pode promover professores e renunciar ao próprio papel quando outro administrador ativo permanecer; não pode remover ou suspender outro administrador. Essa ação é do Administrador Geral, com motivo e auditoria.
- Alunos também pertencem a uma única escola por conta.
- O professor ou Administrador da Escola cria as contas dos alunos individualmente ou em lote. A opção **Cadastre-se como aluno** apenas localiza uma conta pré-cadastrada: na primeira tela, o aluno informa somente a matrícula ou o código de cadastro. Se houver pré-cadastro, ele é encaminhado para a segunda tela já existente de criação do próprio PIN; se não houver, o sistema informa que ele deve procurar o professor ou administrador da escola. Nenhum aluno novo é criado por esse fluxo.
- O professor ou Administrador da Escola cadastra alunos na administração de usuários, individualmente ou em lote. O lote pode ser preenchido em uma página em formato de planilha ou pré-preenchido pela importação de uma planilha de presença CSV, XLS ou XLSX; os dados são revisados e confirmados antes da gravação.
- Para criar o pré-cadastro, nome e número na chamada são obrigatórios. A matrícula é opcional, mas altamente recomendada. O código de cadastro segue a composição escola–turma–número na chamada e é gerado para o aluno quando necessário.
- No primeiro acesso, o aluno informa a matrícula ou o código de cadastro na primeira tela. A criação do PIN ocorre na segunda tela já existente, após a identificação do pré-cadastro. O professor e o administrador não definem nem visualizam esse PIN; após criá-lo, o aluno segue para a plataforma.
- Ditados e categorias têm escopo de visibilidade explícito: público geral, instituição ou turmas. Ditados públicos gerais podem ser utilizados entre escolas; os demais permanecem limitados à escola ou às turmas selecionadas.
- O Administrador da Escola fica limitado a uma escola por conta; para outra escola, usa-se outra conta.
- O Administrador Geral tem escopo global para consultas e exportações, inclusive respostas, notas, relatórios, áudios, arquivos, links e Catálogo de erros privado, sem autorização adicional por escola ou formato.
- O Administrador Geral pode editar, arquivar e restaurar erros de qualquer autor, respeitando a imutabilidade após a primeira associação a um ditado.
- O Administrador da Escola consulta a auditoria da própria escola; o Administrador Geral consulta a auditoria global.
- Toda consulta ou exportação identifica a escola ou o conjunto de escolas-alvo e é auditada com usuário, escopo, recurso, data e resultado.
- Revogar o papel bloqueia novas operações e downloads.
- Professor pode localizar e adotar conteúdo de Ditados publicado para a própria escola ou para turmas sob sua responsabilidade, sem receber permissão para editar o conteúdo ou consultar respostas de outro professor.
- Após um ditado ser aplicado a uma turma ou iniciado por um aluno, seu conteúdo, áudio e resposta esperada ficam imutáveis; para corrigir qualquer desses dados, o professor cria outro ditado. Não haverá versionamento complexo do mesmo cadastro nesta etapa.
- Cada ditado possui no máximo uma aplicação ativa por turma; alunos adicionados depois entram nessa aplicação. O aluno só acessa após a atribuição e pode realizar o ditado quantas vezes quiser, com uma tentativa e um resultado próprios a cada realização.
- Todas as tentativas são preservadas para estatísticas, recomendações e uso futuro por LLM; o melhor resultado é o principal nos relatórios. O áudio usado é preservado com o histórico. Remover o aluno da turma bloqueia novas tentativas sem apagar o histórico. Ditado arquivado não recebe novas atribuições.
- Ditado arquivado não recebe novas atribuições, mas alunos já vinculados podem continuar realizando tentativas. Remover o aluno da turma bloqueia novas tentativas para ele sem apagar o histórico.
- A arquitetura inicial usa MySQL/InnoDB compartilhado, com isolamento por `EscolaId` e autorização no servidor. Áudios não atribuídos têm retenção de 24 horas antes da limpeza segura; exportações não incluem áudio e acima dos limites são recusadas para proteger o servidor.
- O Administrador Geral e o Administrador Técnico são a mesma pessoa neste projeto: o responsável técnico da universidade, com função de emergência no produto e manutenção da infraestrutura. As capacidades podem ser separadas por função, mas não há pessoas distintas.
- `Usuario.Tipo=Administrador` não recebe acesso global automaticamente sem mapeamento aprovado.

### Ditados e Catálogo de erros

- **Ditados** é a página e o conceito dos ditados. **Catálogo de erros** é a página e o conceito das definições de erros; “Marketplace” foi descontinuado como termo de produto.
- Ditados e erros podem ter visibilidade pública geral, institucional ou por turmas. A autoria dos erros continua global; visibilidades institucional e por turmas mantêm um escopo de publicação com escola/turmas-alvo. O Administrador da Escola administra erros dos professores da sua escola, respeitando a imutabilidade após primeira associação a ditado.
- Respostas, notas e áudios permanecem privados da escola de origem.
- Autoria, histórico e imutabilidade da definição publicada devem ser preservados.
- Nomes técnicos antigos, como `ErroCatalogo`, `MarketplaceController`, rotas e migrations, permanecem apenas por compatibilidade até uma futura renomeação planejada.

### Backup e restauração

- O backup cobre o banco inteiro, e não apenas o catálogo; arquivos externos, como áudios, devem ser incluídos ou coordenados no procedimento de implantação.
- Baseline de planejamento: backup completo diário quando o volume permitir, retenção das três cópias completas mais recentes e rotação somente após geração e verificação da nova cópia.
- Deve existir ao menos uma cópia protegida fora do mesmo servidor quando a infraestrutura da universidade permitir.
- Falha de geração ou verificação não remove a última cópia válida.
- Deve haver teste periódico e documentado de restauração.
- A restauração inicial é integral, afetando todas as escolas e dados compartilhados; restauração por escola fica para evolução futura.
- Frequência final, retenção, local, espaço, responsáveis, substituto e RPO/RTO serão confirmados na instalação no servidor universitário.
- Os dados atuais são de teste; qualquer limpeza para preparação de produção será comunicada antes e terá escopo explícito.

### Decisão de transição do MT-04 — 2026-11-07

- Foi aceita a **opção 1: compatibilidade temporária** entre o modelo legado e as novas entidades de aplicação/participação.
- Os dados legados continuam preservados e podem ser lidos pelos caminhos atuais durante a transição.
- Novas atribuições e funcionalidades devem escrever somente no modelo novo; não haverá duas escritas concorrentes.
- A migration de aplicações/participações não deve ser aplicada antes dos adaptadores e testes de transição correspondentes.
- Não há autorização para limpar o banco de testes nesta etapa. Qualquer limpeza futura exigirá aviso e autorização explícitos.

### Decisão adicional do MT-04 — escola canônica da turma (2026-11-07)

- `Turma.EscolaId` será inicialmente nullable, com FK e índice para `Escolas`, sem backfill automático das turmas legadas.
- Novas turmas e novas publicações exigirão escola definida; turmas legadas sem escola continuarão legíveis até mapeamento explícito.
- O vínculo do professor permanece como verificação de autorização e não substitui a escola persistida na turma.

### Decisão adicional do MT-04 — capability global (2026-11-07)

- O Administrador Geral/Técnico será identificado por capability persistida no usuário, desativada por padrão.
- A autorização global será validada no servidor e incluída no contexto/claims; não será inferida pela ausência de vínculo escolar nem aceita do cliente.
- Administradores escolares permanecem limitados às escolas com vínculo ativo; contas administrativas sem vínculo e sem capability são negadas.
- Nenhuma conta existente será promovida automaticamente; a concessão inicial deverá ser explícita e auditável.

## Pendências sem decisão final

- Procedimento operacional do responsável que acumula Administrador Geral e Administrador Técnico, incluindo acesso emergencial, logs, backup e recuperação.
- Parâmetros finais de backup no ambiente da universidade.
- Pendências do refinamento CAT-09 registradas no contrato, sem tratá-las como autorização de implementação.

## Sequência aceita para o épico Multi-tenant

**Contexto:** a criação do épico Multi-tenant altera a ordem das histórias porque CAT-01, CAT-05, COR-01, feedbacks e relatórios dependem de escola, vínculo, autorização e isolamento de dados. A sequência abaixo foi aceita após parecer do chat oficial do gerente de projetos. Ela é uma ordem de planejamento; não cria automaticamente subtarefas nem autoriza implementação.

1. **MT-01 — Fronteiras e classificação dos dados:** definir o que é global e o que pertence à escola, além da matriz de permissões.
2. **CAT-09 / PER-14 — Reconciliar contratos:** incorporar Multi-tenant, papéis, contexto escolar, Catálogo de erros e auditoria ao refinamento de Ditados e do Catálogo de erros.
3. **MT-02 — Escolas, vínculos e papéis:** modelar escola como tenant, professor e Administrador da Escola limitados a uma escola por conta, e o responsável que acumula Administrador Geral e Administrador Técnico.
4. **MT-03 — Contexto e autorização:** validar contexto ativo/escola-alvo no servidor, revogação e auditoria.
5. **MT-04 — Isolamento e migração:** aplicar isolamento a consultas, gravações e associações; definir estratégia de persistência, integridade e migração.
6. **MT-08 — Protótipos UX:** prototipar contexto escolar, administração e estados de acesso antes das novas telas.
7. **Fundação Multi-tenant:** implementar e validar as entregas concretas de contexto, autorização e isolamento.
8. **CAT-01 / PER-6 — Consulta do catálogo:** consulta global respeitando permissões e escola-alvo.
9. **CAT-05 / PER-10 — Seleção por ditado:** seleção no contexto escolar do ditado.
10. **COR-01 / PER-22 — Classificação:** integração usando as seleções persistidas e o tenant do ditado.
11. **Feedbacks, relatórios e dashboards:** somente após estabilizar escopos e agregações escolares.
12. **CAT-08 / PER-13 — Backup integral:** planejamento em paralelo; execução antes de migrações destrutivas e da entrada em produção.

### Paralelismo permitido

- MT-01 e o refinamento documental do CAT-09 podem avançar juntos.
- Planejamento de backup, inventário de migração, preparação de QA e análise de compatibilidade podem avançar em paralelo.
- UX pode preparar fluxos depois de definidos contexto e permissões, antes da implementação das telas.

### Dependências que devem aparecer no Linear

- Fundação MT → CAT-01.
- Fundação MT → CAT-05.
- CAT-05 → COR-01.
- PER-13 relacionado ao MT; bloqueia migração destrutiva e entrada em produção, mas não todo o desenvolvimento.
- CAT-02 / PER-7 permanece Done e não deve ser reaberta por inferência; adaptações futuras serão rastreadas no MT.

Não usar o épico PER-26 inteiro como bloqueio universal: as dependências devem ser granulares para evitar ciclos e bloqueios artificiais.

## Regras de coordenação

- Toda nova tela passa primeiro por protótipo e revisão de UX, antes do desenvolvimento Fullstack.
- Dúvidas de português, correção de erros e critérios pedagógicos devem consultar o pesquisador do squad Ditado antigo.
- Dúvidas de arquitetura, contratos, persistência, dependências, ordem das histórias e versionamento devem consultar o gerente de projetos.
- O Fullstack 01 é o responsável pelo desenvolvimento; o QA oficial valida de forma independente.
- Trabalhar em branches `codex/`, preservar alterações existentes e manter Linear e relatório de ciclo atualizados.

