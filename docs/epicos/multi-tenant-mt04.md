# MT-04 — Persistência, isolamento e histórico

**Épico:** Multi-tenant (PER-26)  
**Estado:** planejamento técnico; sem implementação autorizada nesta etapa.

## Entradas confirmadas

- Cada escola é um tenant.
- Professor e aluno pertencem a uma única escola por conta.
- Ditados e erros têm visibilidade **Público geral**, **Instituição** ou **Turmas**.
- Professor pode localizar e adotar conteúdo autorizado, mas não editar conteúdo de outro professor nem consultar seus resultados.
- Um único nível de visibilidade fica vigente por recurso; alterações encerram o anterior e preservam o histórico.
- Atividades já atribuídas continuam válidas após mudança de visibilidade.
- Um aluno que entra depois pode receber uma atividade existente se pertencer à turma originalmente autorizada.
- A suspensão do professor bloqueia suas novas operações e mantém as atividades já atribuídas acessíveis aos alunos autorizados.
- Conteúdo aplicado a uma turma ou iniciado por aluno fica imutável; para corrigir texto, áudio ou resposta esperada, o professor cria outro ditado. Não haverá versionamento complexo do mesmo cadastro nesta etapa.
- Arquivamento bloqueia novas utilizações sem apagar histórico.
- Conteúdo público adotado por outra escola gera uma aplicação própria da escola consumidora.
- A arquitetura inicial usa um único banco MySQL/InnoDB compartilhado, com isolamento por `EscolaId` e autorização server-side; não haverá banco separado por escola nesta etapa.
- Áudios de ditados não atribuídos entram em limpeza após 24 horas, em duas fases, com verificação de referências, backup e auditoria; áudios referenciados por aplicação, tentativa ou histórico nunca são removidos.
- Exportações são feitas por download direto na tela, sem áudio, com cursor, streaming limitado, limites de linhas/tamanho/tempo e cancelamento. Acima do limite, a operação é recusada e solicita filtros menores; não há arquivo temporário nem envio por e-mail nesta etapa.
- O mesmo ditado não pode gerar aplicações duplicadas para a mesma turma; novos alunos entram na aplicação existente.
- O aluno só acessa o ditado depois da atribuição à turma.
- O aluno pode realizar o ditado quantas vezes quiser. Cada tentativa preserva seu próprio resultado e referencia a mesma aplicação e o mesmo conteúdo imutável do ditado.
- O áudio exato usado na atividade deve ser preservado enquanto houver histórico relacionado, incluindo os arquivos no procedimento de backup.
- Todas as tentativas permanecem armazenadas para estatísticas, recomendações e uso futuro por LLM; a tentativa mais recente não substitui as anteriores.
- O melhor resultado é o resultado principal exibido nos relatórios, sem apagar o histórico completo de tentativas.
- A remoção do aluno da turma bloqueia novas tentativas, mas preserva as tentativas e resultados anteriores.
- O arquivamento bloqueia novas atribuições; para novo uso, o ditado precisa ser restaurado ou substituído por um novo cadastro.
- O arquivamento não interrompe novas tentativas dos alunos já vinculados à aplicação. A remoção do aluno da turma bloqueia novas tentativas para esse aluno, preservando seu histórico.

## Escopo do refinamento técnico

- Modelo ER para escolas, vínculos, papéis, turmas, publicações tipadas e alvos de escola/turma.
- Persistência da aplicação do ditado e da definição de erro usada, separada da política de visibilidade vigente.
- Sessões persistidas e revogáveis após suspensão, troca de papel, recuperação de PIN ou reemissão de convite.
- Auditoria transacional de autorização, publicação, alteração de visibilidade, adoção, exportação, download e restauração.
- Revisão de FKs, constraints, cascades e `SetNull` para preservar respostas, notas, áudios, autoria e histórico.
- Estratégia de migração sem tornar dados legados públicos por padrão.
- Testes reais em MySQL para isolamento, concorrência, revogação, visibilidade e exportações grandes.

## Modelo relacional proposto

| Entidade ou relação | Invariante |
|---|---|
| Escola, conta, vínculo e papéis | Professor e aluno têm uma escola por conta. Estado e papéis do vínculo determinam o acesso escolar. A autorização global do Geral tem atribuição explícita. |
| Turma e participação do aluno | Escola e turma são persistidas. A participação liga aluno e turma da mesma escola. Saída encerra acesso e preserva o registro histórico. |
| Ditado e conteúdo congelado | O ditado existente mantém autor, escola responsável, texto, resposta esperada e áudio. Na primeira atribuição, o conteúdo é marcado como utilizado e fica imutável; correções exigem outro `DitadoId`, sem criar versionamento complexo nesta etapa. |
| Erro do Catálogo | A definição preserva a autoria global e fica imutável após a primeira associação. A escola ou as turmas da publicação restrita pertencem ao escopo da publicação. |
| Publicação de ditado e publicação de erro | Tabelas tipadas por recurso, cada qual com nível, versão, vigência e alvos tipados. Cada recurso aponta para uma publicação vigente. A troca de publicação é atômica. |
| Aplicação do ditado | Evolui a relação `TurmaDitado` com identidade própria e escola consumidora, preservando a unicidade duradoura de turma + ditado. Pode apontar para conteúdo público de outra escola. |
| Seleção de erros e resultados | A seleção fica em uma relação da aplicação e congela os erros usados. Tentativas múltiplas referenciam a aplicação; respostas, notas e classificações pertencem à escola consumidora. |

Guardar apenas o alcance atual e auditar suas mudanças reduziria tabelas, mas exigiria reconstruir o alcance de cada adoção. A proposta usa publicações tipadas e congela a seleção na aplicação, sem duplicar texto ou áudio por aluno. O áudio de leitura necessário à atividade segue a autorização do conteúdo ou da aplicação; o backup deve preservar os bytes ou a referência imutável. Eventual gravação de aluno segue a autorização do resultado escolar.

## Autorização e sessões

- Para descobrir, adotar ou atribuir conteúdo, validar conta, vínculo, papel, arquivo ativo e publicação vigente no servidor. Público geral permite compartilhamento entre escolas no contexto autorizado. O alvo informado pelo cliente seleciona o recurso candidato.
- Para realizar atividade existente, validar a aplicação, sua escola consumidora e a participação atual do aluno na turma originalmente autorizada. Entrada posterior permite que a escola conceda a atividade existente. A concessão registra o aluno e a aplicação, sem criar nova adoção do conteúdo. A visibilidade vigente e o estado do professor autor ficam fora desse predicado.
- Separar acesso histórico de permissão para responder. Ler resultados depende das permissões atuais sobre a escola consumidora e o registro. Arquivamento bloqueia novos usos do conteúdo, mantendo aplicações e resultados anteriores.
- Persistir sessão com conta, identificador, expiração, revogação e versões emitidas de credencial e autorização. Validar sessão e versões atuais em cada operação protegida. Suspensão, mudança de papel e recuperação de PIN atualizam estado e revogação na mesma transação. Reemitir convite invalida a credencial de convite anterior e as sessões afetadas pelo fluxo de ativação.
- Geração e download revalidam conta, sessão e escopo. Exportações assíncronas carregam o escopo autorizado e revalidam antes da execução e entrega. Link temporário precisa passar pelo controle de acesso para que a revogação bloqueie novas solicitações.

## Integridade e auditoria

- Usar chaves únicas por escola e FKs compostas em relações que exigem a mesma escola, como participação em turma, alvos de uma publicação restrita e resposta vinculada à aplicação. A aplicação de B sobre conteúdo público de A é uma exceção explícita. A FK garante a existência do conteúdo e a política vigente autoriza a criação da aplicação.
- Público geral dispensa escola-alvo e lista de turmas. Instituição exige uma escola, com lista de turmas vazia. Turmas exige escola e ao menos um alvo daquela escola. Validar invariantes que envolvam várias linhas na transação. Serializar troca de publicação e confirmação de nova adoção no mesmo recurso para resolver concorrência.
- Revisar cascades de ditado e aluno para respostas, cascades de vínculos históricos e autoria com `SetNull`. Preferir arquivamento, suspensão e FKs restritivas para recursos referenciados. Limitar cascades a dependentes descartáveis. Proteger a substituição do último administrador por transação e controle de concorrência por escola.
- Registrar ator, papel, escola ou escopo global, recurso, operação, instante UTC, motivo quando exigido, resultado e correlação. Para mudanças de alcance, guardar referências ao estado anterior e ao novo. Persistir evento de sucesso com a mutação ou via outbox transacional. Registrar recusas por caminho que sobreviva ao rollback, com dados mínimos e sem credenciais ou conteúdo pedagógico.

## Migração e ordem técnica

### Decisão de transição legada — aceita em 2026-11-07

- Foi escolhida a **compatibilidade temporária** (opção 1) para reduzir retrabalho e preservar o histórico existente.
- Registros legados continuam sendo lidos pelos caminhos atuais enquanto a transição não for concluída.
- Novas atribuições e funcionalidades do MT-04 devem usar as entidades novas, sem manter duas escritas concorrentes.
- A migration de `AplicacaoDitado`/`Participacao*` só deve ser aplicada após definir os adaptadores e os testes que garantem a separação entre leitura legada e escrita nova.
- A limpeza do banco de testes não está autorizada por esta decisão. Qualquer limpeza futura exige aviso e autorização explícitos.
- `TurmaDitado`, `TurmaAluno` e `RespostasDitados` permanecem compatíveis durante a transição; sua remoção ou desativação será uma decisão posterior, baseada em evidência de que nenhum caminho operacional depende deles.

### Escola canônica da turma — decisão aprovada em 2026-11-07

- `Turma.EscolaId` será adicionado como campo inicialmente nullable, com FK e índice para `Escolas`.
- Não haverá backfill automático das turmas legadas: linhas sem escola permanecem legíveis e pendentes de mapeamento explícito.
- Novas turmas e novas publicações exigirão escola definida; o vínculo do professor continua sendo uma checagem adicional de autorização.
- A migration e os guards devem preservar os dados existentes e não executar limpeza ou atribuição silenciosa.

### Capability do Administrador Geral/Técnico — decisão aprovada em 2026-11-07

- O acesso global será representado por uma capability persistida no usuário, desativada por padrão (`AdministradorGeral` ou equivalente).
- A capability será validada no servidor e refletida no contexto/claims autenticado; nunca será aceita a partir de payload ou frontend.
- Administrador da Escola continua limitado aos vínculos escolares ativos. Conta administrativa sem vínculo e sem capability permanece negada.
- Contas existentes não serão promovidas automaticamente; a concessão inicial será explícita e auditável no procedimento de instalação/administração técnica.

1. Inventariar schema efetivo, FKs, dados e versão das migrations. Ensaiar backup e recuperação em cópia.
2. Registrar destino dos dados de teste e mapeamento explícito dos registros que serão preservados, incluindo autores e administradores legados. Qualquer limpeza segue o escopo autorizado e o aviso combinado.
3. Expandir o schema e preencher escola, papéis, aplicações e visibilidades a partir do mapeamento aprovado. Manter registros ambíguos fora de publicação até resolver o destino.
4. Reconciliar IDs, contagens, autoria, vínculos, notas e referências de áudio. Ativar constraints obrigatórias e autorização escolar após a validação. Preservar um ponto de recuperação verificado antes do corte.
5. Validar a fundação de sessões, auditoria e isolamento antes de integrar consulta do Catálogo, seleção e correção.

## Verificação proposta em MySQL

| Cenário | Resultado esperado |
|---|---|
| A publica e B adota um ditado público | B cria aplicação própria e mantém seus alunos, respostas, notas e arquivos no seu escopo. |
| A restringe ou arquiva após B atribuir | A aplicação existente continua. Nova adoção incompatível é negada. Aluno recém-chegado à turma autorizada pode receber a aplicação. Aluno de outra turma tem acesso negado. |
| Professor autor é suspenso com sessão aberta | Próxima operação protegida e download do professor são negados. Alunos autorizados continuam na aplicação. |
| Escola ou turma adulterada e adoção concorrente com restrição | Relações inválidas falham sem escrita parcial. A adoção segue a ordem efetiva das transações e captura a política autorizadora. |
| Troca de PIN, papel ou reemissão de convite | Sessões e credenciais afetadas são invalidadas. O histórico permanece consultável conforme autorização atual. |
| Exportação volumosa, cancelamento, falha e revogação | Escopo e consumo de memória são verificados. Uma tarefa cancelada ou revogada não libera novo download. Auditoria registra o resultado efetivo. |
| Exclusão, substituição concorrente do último admin e ensaio de migração | Histórico protegido sobrevive. A escola mantém administrador ativo. Reconciliação e recuperação são demonstradas em cópia. |

## Pendências técnicas

- Confirmar com Fullstack a estratégia transacional e os índices após testar o provedor e o schema efetivos. O risco principal é permitir uma associação cruzada legítima e, junto dela, expor o resultado escolar.
- Fechar como o marco de primeira atribuição será persistido no `Ditado` e como tratar seleções legadas. A visibilidade vigente não deve reclassificar resultados.
- Definir política operacional final para armazenamento e retenção de auditoria/exportações no servidor universitário. A implementação do PER-36 já aplica retenção segura de áudio e arquivos temporários limitados; os parâmetros de operação continuam dependentes do ambiente.

## Critérios de aceite do planejamento

- O ER distingue autoria, visibilidade, aplicação e resultado histórico.
- Publicações institucionais só aceitam escola-alvo válida; publicações por turma só aceitam turmas da mesma escola.
- Adoção pública entre escolas não mistura categorias, alunos, respostas, notas ou áudios.
- Uma turma possui no máximo uma aplicação do mesmo ditado durante toda a vida do registro, enquanto um aluno pode ter várias tentativas nessa aplicação.
- Mudança de visibilidade não remove atividades nem recalcula resultados.
- Suspensão revoga novas operações e downloads sem interromper atividades já atribuídas.
- Exclusão física não apaga histórico pedagógico protegido.
- O plano de testes inclui duas escolas, payload adulterado, concorrência, sessão antiga e falhas de exportação.

## Fora do escopo

- Implementação de código, migrations ou alteração de dados vivos.
- Escolha final do servidor físico e dos parâmetros operacionais de backup.
- Novas telas antes do protótipo e revisão de UX.

## Sequência de execução planejada

1. **PER-31 — Inventário e baseline:** consolidar schema, migrations, ER mínimo, contratos e critérios; ensaiar backup/restauração em cópia.
2. **PER-32 — Fundação:** implementar escola, vínculos, papéis, `EscolaId`, participação e integridade escolar.
3. **PER-33 — Autorização:** implementar sessões revogáveis, autorização server-side e auditoria.
4. **PER-34 — Aplicações:** evoluir `TurmaDitado` para aplicação única por turma+ditado, participação, seleção congelada e tentativas múltiplas.
5. **PER-35 — Visibilidade:** implementar Público geral, Instituição e Turmas para Ditados e Catálogo de erros, com arquivamento separado.
6. **PER-36 — Áudio e exportação:** concluído com limpeza segura após 24 horas e exportação sem áudio, limites, cancelamento, auditoria e arquivo temporário bounded.
7. **PER-37 — Testes e QA:** concluído com gate MySQL em schema derivado, validação funcional da API e aceite do QA. A limitação do runner VSTest/TestHost permanece registrada separadamente; o Fullstack reportou 101/101 na execução com SDK9.
8. **PER-38 — UX:** concluído com protótipo alinhado às telas reais, aprovado pelo QA; nenhuma tela de produção foi implementada nesta história.

### Fluxo de escolha de escola no cadastro — decisão aprovada em 2026-11-07

- O professor deve escolher uma escola existente ou criar uma nova durante o cadastro.
- Selecionar uma escola existente não cria vínculo automaticamente e não concede acesso.
- O vínculo do professor será criado somente após convite ou aprovação pelo fluxo autorizado.
- Solicitação autônoma de entrada em escola fica fora desta fatia e poderá ser avaliada como evolução posterior.
- Enquanto o vínculo estiver pendente, o professor pode acompanhar o cadastro/aprovação, mas não pode criar turmas, cadastrar alunos, publicar ou atribuir ditados, consultar dados da escola ou exportar informações.
- Ao criar uma escola nova, o professor recebe vínculo ativo e acumula os papéis de Professor e Administrador da Escola na mesma transação; turmas e alunos podem ser criados após o sucesso do cadastro.

As etapas 1 e 2, o planejamento de backup, a preparação do QA e os fluxos conceituais de UX podem avançar em paralelo. As etapas 3 e 4 bloqueiam aplicações e visibilidade; a etapa 5 bloqueia CAT-05/COR-01; a etapa 8 deve ocorrer antes da liberação.
