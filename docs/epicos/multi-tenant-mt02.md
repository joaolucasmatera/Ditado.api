# MT-02 — Escolas, vínculos e ciclo de vida dos papéis

**Épico:** [PER-26 — Multi-tenant](https://linear.app/joaomoutinho/issue/PER-26/multi-tenant-isolar-organizacoes-e-seus-dados)  
**Tarefa:** [PER-28 — MT-02](https://linear.app/joaomoutinho/issue/PER-28/mt-02-modelar-escolas-vinculos-e-ciclo-de-vida-dos-papeis)  
**Estado:** planejamento; não autoriza código ou migration.

## Fluxo de cadastro

1. A pessoa se cadastra como professor.
2. No cadastro, escolhe explicitamente entre **criar uma nova escola** ou **selecionar uma escola existente**.
3. Cada escola possui nome e código curto único, gerado pelo sistema, para evitar homônimos.
4. Se escolher escola existente, pesquisa pelo nome ou informa o código e confirma a escola encontrada.
5. O professor envia a solicitação e o Administrador da Escola aprova ou recusa.
6. Qualquer Administrador da Escola ativo pode aprovar ou recusar a solicitação.
7. Enquanto a solicitação estiver pendente, o professor não acessa dados nem cria ditados naquela escola.
8. Se criar escola, torna-se seu Administrador da Escola imediatamente.
9. Não existe conta de professor sem escola e a mesma conta não pode pertencer a duas escolas.

### Estados apresentados no cadastro do professor

- **Criar nova escola:** o professor informa os dados da escola; após o cadastro confirmado, recebe vínculo ativo e o papel de Administrador da Escola.
- **Usar escola existente:** o professor seleciona a escola cadastrada; o cadastro fica pendente até aprovação de um Administrador da Escola. Enquanto aguarda, não recebe sessão de uso, não entra na plataforma e não acessa qualquer dado ou operação escolar; apenas pode consultar o estado público do próprio pedido por um mecanismo específico de acompanhamento.

## Cadastro de alunos por turma

- O professor ou Administrador da Escola pode enviar a planilha de presença da turma ou cadastrar os alunos manualmente.
- A tela oferece **Cadastre-se como aluno**, mas esse fluxo não cria um aluno novo: na primeira tela, o aluno informa somente a matrícula ou o código de cadastro para localizar uma conta já pré-cadastrada pelo professor ou Administrador da Escola.
- O cadastro em lote também pode ser preenchido em uma página em formato de planilha, permitindo revisar e corrigir os dados antes da gravação.
- O sistema pode importar planilhas de presença nos formatos CSV, XLS ou XLSX para preencher automaticamente essa página, apresenta uma prévia e exige confirmação antes de criar os alunos.
- A prévia identifica linhas inválidas, duplicadas ou sem dados necessários antes da gravação.
- Alunos já existentes não têm seus dados alterados por uma nova importação; são reconhecidos e informados como já cadastrados.
- Linhas inválidas ou incompletas ficam fora da importação para cadastro manual posterior; a confirmação informa apenas o total não importado e o motivo geral, sem exigir a exibição de cada linha.
- Para cada aluno, o nome e o número na chamada são obrigatórios. A matrícula é opcional, mas altamente recomendada.
- Após a confirmação, o sistema gera os convites individuais da turma.
- O convite pode ser exibido em uma tela de apresentação com tutorial curto e lúdico ou preparado em folha para impressão e recorte.
- Cada convite apresenta nome do aluno, matrícula quando existir, código de cadastro e QR Code.
- A matrícula do aluno é opcional, mas deve ser apresentada como altamente recomendada no cadastro, pois facilita localizar e reconhecer o aluno em novas importações.
- O QR Code abre a ativação do aluno; a matrícula ou o código de cadastro também podem iniciar o mesmo fluxo.
- O professor ou Administrador da Escola não cria nem visualiza o PIN do aluno.
- No cadastro como aluno, o aluno informa somente sua matrícula ou seu código de cadastro na primeira tela. Se houver pré-cadastro, o sistema encaminha para a segunda tela já existente de criação do PIN; somente depois de criar o próprio PIN o aluno segue para a plataforma. Se não houver, o sistema informa: **Você ainda não foi pré-cadastrado na plataforma. Fale com seu professor ou administrador da sua escola.**
- O código de cadastro serve para ativação inicial; depois da ativação, o acesso exige o PIN do aluno.
- O código de cadastro seguirá a composição simples: escola + turma + número da chamada. A matrícula, quando existir, também pode ser usada diretamente pelo aluno para iniciar a ativação.

### Critérios do convite

- O professor pode escolher entre apresentar os convites na tela, imprimir, ou fazer ambos.
- A tela e a impressão não exibem dados de outros alunos além dos necessários para identificar cada convite.
- O professor pode reemitir um convite sem criar uma segunda conta.
- Convite perdido pode ser invalidado e substituído pelo professor ou Administrador da Escola.
- Convites são de uso único, expiram em 72 horas e a reemissão invalida o convite anterior.
- A matrícula, quando existente, não substitui a validação da escola e da turma no servidor.
- A ausência de matrícula não impede o cadastro; nesse caso, o código escola + turma + número da chamada identifica a ativação inicial.

## Papéis e responsabilidades

| Papel | Escopo |
|---|---|
| Professor | Uma escola; cria ditados e categorias da escola e usa os recursos autorizados. |
| Aluno | Uma escola; acessa atividades, respostas e feedbacks próprios conforme autorização. |
| Administrador da Escola | Uma escola; administra professores, alunos, turmas, ditados, categorias e erros dos professores da escola. Pode promover professores, mas não remover nem suspender outro administrador. |
| Administrador Geral | Todas as escolas; administra Administradores das Escolas, atua em emergências, consulta/exporta conteúdo conforme escopo aprovado e administra erros de qualquer autor. |
| Administrador Técnico | A mesma pessoa do Administrador Geral; opera servidor, banco, storage, backup e logs técnicos. O acesso de infraestrutura é controlado institucionalmente e não concede automaticamente permissões de produto a outras pessoas. |

## Ciclo de vida do Administrador da Escola

- O professor que cria a escola recebe o papel inicial.
- Um Administrador da Escola pode promover outro professor vinculado à mesma escola.
- Um Administrador da Escola não pode remover nem suspender outro administrador.
- O Administrador Geral remove ou suspende administradores, mediante motivo registrado.
- Cada administrador pode renunciar ao próprio papel se outro administrador ativo permanecer.
- A escola deve manter pelo menos um administrador ativo.
- A suspensão ou remoção do último exige promover ou criar um substituto na mesma operação do Administrador Geral.
- A suspensão revoga novas operações administrativas e sessões privilegiadas; não apaga dados, autoria, ditados, categorias, respostas, notas ou históricos.
- O Administrador Geral pode substituir ou criar administrador em emergência, com auditoria.
- Remover o papel administrativo não remove automaticamente outros papéis; a suspensão da conta é uma operação distinta.

## Saída de professores

- Qualquer Administrador da Escola ativo pode suspender ou remover professores da própria escola.
- A suspensão ou remoção revoga imediatamente o acesso do professor à escola.
- A conta do professor não é apagada automaticamente.
- Ditados, categorias, respostas, notas, autoria e históricos permanecem preservados.
- Erros cadastrados continuam vinculados ao autor e seguem a política de administração e imutabilidade do Catálogo de erros.
- A operação registra autor, professor afetado, escola, motivo, data e resultado.

## Importação, convites e auditoria

- A matrícula é opcional e altamente recomendada; quando existente, facilita reconhecer o aluno em importações futuras.
- O código escola + turma + número da chamada é o identificador de ativação quando a matrícula não for usada.
- No primeiro acesso, matrícula ou código de cadastro são obrigatórios; o aluno cria o PIN nessa etapa.
- O limite inicial é de cinco tentativas inválidas, seguido de bloqueio temporário de 15 minutos; novas falhas podem exigir reemissão do convite.
- Professor ou Administrador da Escola autoriza a recuperação do PIN; o PIN anterior é invalidado e o aluno cria outro por novo convite.
- Auditoria de ações administrativas, convites, PINs e acessos será mantida por dois anos; logs técnicos de infraestrutura, por 90 dias, salvo prazo institucional maior.
- A Política de Segurança da Informação e Comunicação da UNIRIO exige registros históricos para auditoria, mas a pesquisa inicial não localizou prazo específico de retenção; o padrão será revisado com a universidade.

## Saída de alunos

- O Administrador da Escola pode suspender ou remover alunos da escola.
- O professor pode remover o aluno apenas de suas próprias turmas ou atividades.
- Suspender ou remover o aluno da escola revoga imediatamente seu acesso.
- Respostas, notas, atividades e históricos permanecem preservados.
- A conta do aluno não é apagada automaticamente.
- O aluno não é transferido automaticamente para outra escola.

## Auditoria e emergência

- Administrador da Escola consulta auditoria somente da própria escola.
- Administrador Geral consulta auditoria global e administra mudanças de papéis.
- Mudanças de cadastro, escola, solicitação, aprovação, recusa, promoção, suspensão e remoção registram autor, alvo, escola, data, motivo e resultado.
- A conta Geral/Técnica é de uso emergencial no produto e operacional na infraestrutura; suas ações devem ser identificáveis e revisadas.

## Pendências para MT-04

- Estratégia de persistência e chaves estrangeiras.
- Contexto ativo e validação server-side.
- Revogação de sessões e tokens já emitidos.
- Migração e compatibilidade com dados existentes.
- Fluxos de recuperação em caso de erro na promoção/substituição.

## Critério de conclusão

O MT-02 estará pronto quando o fluxo de cadastro, aprovação, papéis, substituição do último administrador, revogação e auditoria estiverem documentados e revisados, com pendências técnicas encaminhadas ao MT-04.

