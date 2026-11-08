# MT-03 — Contexto escolar, autorização e revogação

**Épico:** [PER-26 — Multi-tenant](https://linear.app/joaomoutinho/issue/PER-26/multi-tenant-isolar-organizacoes-e-seus-dados)  
**Tarefa:** [PER-29 — MT-03](https://linear.app/joaomoutinho/issue/PER-29/mt-03-definir-contexto-escolar-autorizacao-e-revogacao)  
**Estado:** planejamento; depende das decisões do MT-02.

## Objetivo

Definir como o servidor identifica a escola do recurso, valida a autorização e revoga o acesso após mudanças de papel, vínculo, PIN ou sessão.

## Regras

- A escola é resolvida pelo recurso persistido e pelo vínculo autorizado; nome, filtro ou ID enviado pelo cliente não concede acesso.
- Professor e aluno só acessam recursos da escola da conta.
- Administrador da Escola opera somente na própria escola.
- Administrador Geral consulta e exporta no escopo global auditado.
- Ditados e Catálogo de erros são áreas distintas do produto; “Marketplace” não é mais um termo de produto. Ambos usam visibilidade pública geral, institucional ou por turmas; categorias permanecem escolares.
- Suspensão, remoção, mudança de papel, recuperação de PIN e reemissão de convite revogam o acesso correspondente.
- Cache, abas, filtros, filas e tarefas em segundo plano não podem misturar escolas.

## Auditoria

Registrar ator, papel e escopo no momento da operação, escola-alvo quando aplicável, recurso, operação, data UTC, motivo e resultado. Operações recusadas também devem ser registradas sem incluir PIN, senha, token, QR Code ou conteúdo desnecessário.

## Matriz de autorização

| Papel | Escopo | Pode consultar | Pode criar/alterar | Pode arquivar/remover | Observações |
|---|---|---|---|---|---|
| Professor | A própria escola | Seus ditados, turmas, alunos e resultados autorizados; também pode localizar conteúdo publicado para a escola ou para suas turmas | Seus próprios ditados e registros permitidos pelo produto; pode adotar conteúdo publicado por outros professores | Apenas seus registros que ainda não tenham histórico dependente; depois disso, arquiva | Usar/adotar conteúdo não concede edição nem acesso a respostas de outro professor |
| Administrador da Escola | A própria escola | Dados pedagógicos e administrativos da escola | Professores, alunos, turmas e recursos escolares previstos | Recursos da escola conforme as regras de imutabilidade | Não suspende nem remove outro administrador |
| Administrador Geral/Técnico | Global, com escola-alvo explícita | Dados e auditorias de qualquer escola | Operações administrativas de emergência | Pode suspender/remover administradores e restaurar backup integral | Toda consulta, exportação ou restauração é auditada |
| Aluno | A própria escola e seus próprios resultados | Seu cadastro, convites e resultados próprios | PIN próprio e dados permitidos no fluxo de entrada | Não remove dados pedagógicos | Matrícula, código e QR Code são localizadores, não prova de identidade |

### Regras de resolução do contexto

1. Cada rota que lê ou grava um recurso deve resolver a escola a partir do registro persistido e do vínculo do usuário.
2. Um `EscolaId` recebido na URL, no corpo, em filtro ou no cabeçalho apenas seleciona um recurso candidato; nunca concede autorização.
3. Recursos de Ditados e do Catálogo de erros devem validar autoria e nível de visibilidade; publicações institucionais ou por turmas também validam escola e turmas-alvo.
4. Exportações globais precisam informar explicitamente o conjunto de escolas consultado e gerar um único registro de auditoria por operação, com o resultado e o escopo efetivamente aplicado.

### Revogação e concorrência

- Suspensão de usuário, remoção de vínculo, troca de papel, recuperação de PIN e reemissão de convite invalidam sessões e credenciais afetadas.
- A revogação deve ser verificada no servidor em toda operação protegida; não basta esperar a expiração do JWT.
- A substituição de um administrador e a proteção do último administrador ativo devem ocorrer em transação, com controle de concorrência.
- A revogação impede novas operações e downloads, mas preserva autoria, resultados, auditoria e histórico necessários ao produto.
- Falhas de autorização também são auditadas, sem revelar ao cliente informações de outra escola.

## Decisões confirmadas nesta etapa

- O Professor consulta apenas seus próprios ditados, turmas, alunos e resultados.
- O Administrador da Escola consulta os dados pedagógicos e administrativos da própria escola.
- O Administrador Geral/Técnico pode operar em qualquer escola, sempre com escola-alvo explícita.
- Suspensão, remoção, exportação, restauração e alterações administrativas sensíveis exigem justificativa auditável.
- Arquivos, áudios e relatórios validam a autorização na geração e no download, usando links temporários.
- Restauração é procedimento técnico no servidor, precedido por backup e executado em janela de manutenção.
- Exportações grandes são assíncronas, paginadas, transmitidas em fluxo, limitadas e auditadas.
- A implementação de exportação deve ser validada localmente com testes de volume, concorrência, cancelamento, retomada, consistência e consumo de memória antes de ser disponibilizada.

## Visibilidade de ditados e erros

Ditados e erros cadastrados terão três níveis de publicação:

1. **Público geral:** disponível para todas as escolas, com aviso claro de que o conteúdo é compartilhado entre instituições.
2. **Instituição:** disponível somente dentro da escola selecionada.
3. **Turmas:** disponível somente para as turmas selecionadas.

O nível de visibilidade deve ser verificado no servidor em toda consulta, seleção, uso, exportação e download. A mudança de visibilidade também deve ser auditada.

Para ditados, o escopo de instituição ou de turmas mantém o recurso associado à escola de origem. Para erros, a autoria continua sendo global, mas os níveis **Instituição** e **Turmas** exigem um escopo de publicação persistido, com escola e, quando aplicável, turmas autorizadas. Isso evita transformar a autoria em vínculo escolar e preserva a possibilidade de um mesmo erro ser publicado novamente com outro alcance.

Essa decisão deve ser incorporada ao MT-01 e aos contratos do catálogo, pois altera consulta, seleção, compartilhamento e autorização.

### Decisões confirmadas sobre visibilidade

- No nível **Instituição**, todos os professores e administradores da escola podem localizar e usar o conteúdo. Alunos só acessam quando estiverem vinculados ao ditado ou à turma correspondente.
- No nível **Turmas**, o autor pode selecionar várias turmas da mesma escola.
- O professor pode publicar diretamente nos três níveis, sem aprovação prévia; a operação fica auditada e o conteúdo pode ser arquivado conforme as regras de imutabilidade.
- Alterar a visibilidade afeta apenas usos futuros. Resultados, respostas, notas, áudios e auditorias já produzidos permanecem preservados.
- Professores responsáveis pelas turmas selecionadas podem localizar e adotar conteúdo publicado no nível **Turmas**; professores de outras turmas não o visualizam. Administradores da escola continuam podendo administrá-lo.
- Depois que um ditado é aplicado a uma turma ou iniciado por um aluno, seu conteúdo, áudio e resposta esperada ficam imutáveis; alterações exigem nova versão ou novo ditado.

### Decisões adicionais confirmadas para o MT-04

- Se a visibilidade mudar depois da atribuição, alunos já atribuídos continuam podendo acessar a atividade; a mudança bloqueia novas atribuições e adoções externas.
- Alunos que entrarem posteriormente na turma podem receber uma atividade existente quando pertencerem à turma originalmente autorizada.
- Arquivamento e visibilidade são controles distintos: arquivar bloqueia novas utilizações; alterar visibilidade controla o alcance de novas adoções. Ambos preservam atividades, resultados e auditorias históricas.
- A suspensão do professor autor bloqueia novas publicações, edições e adoções feitas por ele, mas não interrompe atividades já atribuídas aos alunos.
- Quando um conteúdo público de outra escola é adotado, a atividade é registrada como aplicação própria da escola consumidora. Respostas, áudios, notas e dados dos alunos permanecem separados.
- O detalhamento técnico deverá cobrir modelo ER, sessões revogáveis, auditoria, constraints, cascades, migração e testes de isolamento entre escolas.

## Critérios de teste para o próximo refinamento

- Acesso permitido e negado em duas escolas diferentes.
- Tentativa de trocar escola por URL, ID, filtro ou payload.
- Suspensão durante uma sessão já aberta.
- Reemissão de convite e recuperação de PIN invalidando credenciais anteriores.
- Concorrência na promoção, suspensão e substituição de administrador.
- Exportação global com escopo explícito e auditoria.

## Pendências para MT-04

- Implementação server-side de autorização e revogação.
- Persistência de sessões, versões de credencial e vínculos.
- Constraints, transações e isolamento no banco MySQL.
- Migração e compatibilidade com dados existentes.

