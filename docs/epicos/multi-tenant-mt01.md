# MT-01 — Fronteiras de dados e matriz de permissões

**Épico:** [PER-26 — Multi-tenant](https://linear.app/joaomoutinho/issue/PER-26/multi-tenant-isolar-organizacoes-e-seus-dados)  
**Tarefa:** [PER-27 — MT-01](https://linear.app/joaomoutinho/issue/PER-27/mt-01-definir-fronteiras-de-dados-e-matriz-de-permissoes)  
**Estado:** planejamento; não autoriza código, migration ou limpeza de dados.

## Contexto e decisões de produto

- Cada tenant representa uma escola.
- Cada conta de professor pertence a uma única escola. Para trabalhar em outra escola, a pessoa usa outra conta. Esta decisão substitui a proposta de professor multi-escola.
- No cadastro, o professor deve escolher uma escola existente ou criar uma nova. Não há conta de professor sem escola.
- Ao criar uma nova escola, esse professor se torna o Administrador da Escola.
- O Administrador Geral administra os Administradores da Escola.
- Uma escola deve manter pelo menos um Administrador da Escola ativo; para suspender ou remover o último, o Administrador Geral deve promover ou criar o substituto na mesma operação.
- Alunos também pertencem a uma única escola por conta.
- Ditados têm visibilidade explícita: público geral, instituição ou turmas. Apenas o nível público geral permite uso entre escolas.
- Categorias pertencem exclusivamente a uma escola, seguindo a mesma regra dos ditados.
- Administrador da Escola administra uma escola por conta.
- Administrador Geral tem escopo global para as consultas e exportações enumeradas, sempre com escola ou conjunto de escolas-alvo e auditoria.
- Administrador Geral e Administrador Técnico são a mesma pessoa, responsável pelo produto em emergências e pela infraestrutura da universidade. As capacidades podem continuar separadas por função, mas não há pessoas distintas neste projeto.
- Ditados e Catálogo de erros são áreas distintas do produto. Ambos podem ser Público geral, Instituição ou Turmas; respostas, notas e áudios continuam privados da escola do ditado e do aluno.
- Decisão humana da revisão: erros cadastrados pertencem ao autor e não possuem escola de origem. A publicação não exige escolher uma escola. O Administrador da Escola administra os erros dos professores vinculados à sua escola, respeitando a imutabilidade após primeira associação a um ditado.
- Restauração inicial é integral; restauração isolada por escola não faz parte da primeira versão.

## Classificação inicial dos dados

| Recurso | Classificação | Regra de escopo |
|---|---|---|
| Conta (`Usuario`) | Identidade de autenticação | Conta de professor vinculada a uma escola; outra escola exige outra conta. Não deduplicar ou reunir contas automaticamente por pessoa. O modelo dos demais perfis continua em revisão. |
| Escola/tenant | Global de administração, delimitador escolar | Representa a organização e é usada para delimitar todos os recursos escolares. |
| Vínculo usuário–escola | Escolar | Define participação, papel, estado ativo/revogado e limites do usuário naquela escola. |
| Turma e seus alunos | Escolar | Só podem ser consultados e administrados por vínculos autorizados na escola da turma. |
| Ditado, segmentos e atribuições | Escolar | Pertencem à escola responsável; associações entre escolas são recusadas. |
| Categorias | Escolar | Pertencem à escola; visibilidade pública de ditados e erros não transforma categorias em recursos globais. |
| Respostas, notas e classificações | Escolar | Pertencem à escola do ditado e do aluno; não são expostas pela visibilidade pública dos Ditados ou do Catálogo de erros. |
| Áudios, arquivos e links pedagógicos | Escolar, salvo definição explícita futura | Acesso segue o recurso escolar ao qual estão vinculados; o Administrador Geral pode consultar/exportar dentro do escopo auditado. |
| Feedbacks e relatórios | Escolar por origem | Agregam dados da escola autorizada; exportações globais do Administrador Geral identificam o conjunto de escolas. |
| Definição do Catálogo de erros (`ErroCatalogo`) | Compartilhada conforme visibilidade | Vinculada ao autor; visibilidade institucional ou por turmas mantém escola/turmas-alvo. Mantém histórico e imutabilidade após primeira associação a um ditado, inclusive para administradores. |
| Auditoria | Transversal e protegida | Administrador Geral consulta a auditoria global; Administrador da Escola consulta somente a auditoria da sua escola. Registra usuário, papel, escola/alcance, recurso, data, operação e resultado. |
| Backup e restauração | Global operacional | Backup e restauração iniciais abrangem o banco inteiro e todas as escolas; não implicam restauração isolada. |

## Matriz de permissões inicial

| Capacidade | Aluno | Professor vinculado | Administrador da Escola | Administrador Geral | Administrador Técnico |
|---|---|---|---|---|---|
| Acessar atividades atribuídas | Próprias | Conforme vínculo/turma | Na escola administrada | Consulta global auditada | Não por padrão |
| Enviar e consultar própria resposta | Própria | Não em nome do aluno | Conforme suporte autorizado | Consulta/exportação auditada | Não por padrão |
| Administrar turmas e ditados | Não | Suas turmas/ditados autorizados | Escola administrada | Consulta/exportação auditada; alteração depende de capacidade global explícita | Não por padrão |
| Consultar Catálogo de erros | Não | Sim | Sim | Sim | Não por padrão |
| Publicar definição no Catálogo de erros | Não | Sim, com autoria e sem escola de origem | Sim, conforme política do produto | Em revisão | Não por padrão |
| Editar/arquivar definição própria | Não | Sim, enquanto permitido pelo estado de uso | Conforme autoria/política | Sim, dentro da capacidade global | Não por padrão |
| Editar/arquivar definição de outro autor | Não | Não | Erros dos professores de sua escola; edição somente antes da primeira associação | Qualquer autor, respeitando a imutabilidade após uso | Não por padrão |
| Consultar/exportar respostas, notas, relatórios, áudios e arquivos | Não | Apenas escolas vinculadas e recursos autorizados | Escola administrada | Uma ou várias escolas, com escopo explícito e auditoria | Não por padrão |
| Restaurar backup | Não | Não | Não | Sim, operação integral e auditada, com salvaguardas | Conforme atribuição operacional institucional |
| Operar host, storage e rotina de backup | Não | Não | Não | Não por herança | Sim, conforme atribuição institucional |

### Regras transversais

1. O servidor resolve e valida a escola pelo recurso e pelo vínculo; nome, ID ou filtro enviado pelo cliente não concede acesso.
2. Operações sobre dados escolares identificam escola ou conjunto de escolas-alvo, mesmo quando executadas pelo Administrador Geral. Operações sobre erros globais identificam o recurso e o autor, sem exigir escola de origem.
3. Trocar o contexto escolar não amplia permissões nem mistura dados, cache, abas ou resultados.
4. Revogar vínculo ou papel bloqueia novas operações e downloads, sem apagar autoria, histórico ou resultados.
5. Associações entre recursos de escolas diferentes são recusadas, exceto a adoção de definições públicas gerais do Catálogo de erros.
6. O papel legado `Usuario.Tipo=Administrador` não recebe escopo global automaticamente.
7. Administrador Técnico não recebe permissões do produto por administrar a infraestrutura.

## Impactos e dependências

- **CAT-09/PER-14:** incorporar esta classificação, a matriz e as regras de Ditados e do Catálogo de erros.
- **CAT-01/PER-6:** consultar definições globais sem expor dados escolares indevidos.
- **CAT-02/PER-7:** permanece concluída; preservar autoria e contrato aprovado, sem acrescentar escola de origem ao erro.
- **CAT-05/PER-10:** selecionar definições no contexto escolar do ditado.
- **COR-01/PER-22:** classificar usando a seleção persistida do ditado e manter respostas/notas no tenant de origem.
- **PER-13/CAT-08:** backup integral e restauração global; não oferecer restauração escolar isolada nesta fase.

## Pendências encaminhadas a MT-02/MT-04

- Estratégia física de persistência: banco compartilhado, schemas separados ou bancos separados.
- Modelo definitivo de vínculo, estado, convite, transferência e revogação.
- Capacidades administrativas globais exatas além de consulta/exportação e restauração.
- Detalhes de auditoria, retenção e proteção dos arquivos de log.
- Plano de migração dos dados atuais de teste, sem execução nesta etapa.

## Critério de conclusão do MT-01

O MT-01 estará pronto quando a classificação, a matriz e as regras transversais forem revisadas, suas pendências forem encaminhadas a MT-02/MT-04 e o CAT-09 puder incorporar o contrato sem reabrir decisões já confirmadas.

