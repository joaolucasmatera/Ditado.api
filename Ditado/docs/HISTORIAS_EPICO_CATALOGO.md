# Histórias — Épico: Catálogo e gerenciamento de erros

**Origem:** `ESPECIFICACAO_PG2.md`, seções 2, 3.1, 4, 6 e 7.
**Status:** refinamento em andamento. Decisões de acesso ao catálogo, arquivamento/restauração, escopo das anotações e importação JSON foram incorporadas; pendências restantes estão listadas ao final.

## Objetivo do épico

Como professor ou administrador, quero manter um catálogo compartilhado de erros e selecionar regras relevantes para cada ditado, para que o conteúdo apoie a correção e a análise pedagógica sem perder o histórico.

## Histórias

### CAT-01 — Consultar o catálogo compartilhado

**Como** professor ou administrador, **quero** consultar os erros ativos do catálogo, **para** reutilizar conceitos já publicados ao preparar atividades.

**Critérios de aceite**
- Professores e administradores autenticados conseguem consultar erros ativos do catálogo compartilhado; alunos não consultam o catálogo.
- Cada item apresenta, no mínimo, nome do erro (que identifica seu tipo), descrição e indicação de regra executável quando disponível; não há categoria separada.
- A consulta não expõe dados de autenticação ou outros dados pessoais do autor além do necessário para identificar autoria.
- Itens arquivados não aparecem na seleção padrão de erros ativos.

### CAT-02 — Publicar erro no catálogo

**Fatia aprovada vigente:** [CAT-02 — Publicar erro no Marketplace](../../docs/epicos/marketplace-cat02.md). Nome/Descrição obrigatórios; grupo Exemplo opcional (esperado e resposta, ambos ou nenhum), Observação/Série opcionais. Código e metadados gerados pelo servidor; publicação sem executor/LLM. Persistência, formulário e confirmação; não inclui listagem ampla CAT-01.

**Como** professor ou administrador, **quero** publicar uma definição de erro, **para** que ela possa ser usada no catálogo e em ditados.

**Critérios de aceite**
- Usuários com papel de professor ou administrador podem criar um erro.
- O cadastro valida os campos obrigatórios definidos para o catálogo.
- O nome identifica o tipo do erro; a descrição e os exemplos detalham o padrão. A regra executável é opcional e o erro possui código estável.
- O registro guarda autoria e datas de criação/alteração.
- A definição pode conter descrição/orientações e exemplos para uso pedagógico; a regra executável é opcional e sua ausência fica identificável.
- A definição pode conter uma observação pedagógica simples em texto livre e série/ano opcional, como campos do próprio erro.
- Após salvar, o erro fica disponível no catálogo ativo.

### CAT-03 — Editar erro ainda não usado conforme autoria e papel

**Como** autor de um erro ou administrador, **quero** editar sua definição, **para** corrigir ou aprimorar o conteúdo publicado.

**Critérios de aceite**
- Um professor pode editar somente erros de sua autoria.
- Um administrador pode editar erros de qualquer autor.
- Demais usuários não conseguem alterar o registro.
- A edição mantém o identificador estável do erro e atualiza a data de alteração.
- Erros já associados a qualquer ditado não podem ser editados; para substituí-los, o autor ou administrador arquiva a definição usada e publica outro registro.
- A observação pedagógica e a série/ano seguem as mesmas regras de edição da definição do erro.
- A API aplica a autorização mesmo quando chamada diretamente, sem depender da interface.

### CAT-04 — Arquivar erro sem apagar o histórico

**Como** autor de um erro ou administrador, **quero** arquivar uma definição que não deve mais ser usada, **para** preservar respostas e associações históricas.

**Critérios de aceite**
- Um professor pode arquivar somente erros de sua autoria; um administrador pode arquivar qualquer erro.
- Arquivar não apaga respostas, associações a ditados nem referências históricas.
- Erros arquivados deixam de ser selecionáveis em novos ditados.
- Referências a erros arquivados continuam legíveis em resultados históricos.
- A ação é lógica e fica registrada no estado e nas datas do erro.

### CAT-10 — Consultar e restaurar erros arquivados

**Como** autor de um erro ou administrador, **quero** consultar erros arquivados e restaurar um erro quando necessário, **para** administrar o catálogo sem perder seu histórico.

**Critérios de aceite**
- Um professor consulta e restaura apenas erros próprios arquivados; um administrador consulta e restaura erros arquivados de qualquer autor.
- Erros arquivados continuam distinguíveis dos ativos na lista e não são selecionáveis em novos ditados.
- Restaurar um erro muda seu estado para ativo e permite sua seleção em novos ditados.
- A restauração preserva o identificador e as associações e referências históricas.
- A API aplica autorização em chamadas diretas.

### CAT-05 — Selecionar erros aplicáveis a um ditado

**Como** professor, **quero** associar erros ativos a um ditado, **para** limitar as regras e referências usadas na análise daquele ditado.

**Critérios de aceite**
- O professor pode escolher um ou mais erros ativos ao criar ou editar um ditado, conforme autorização existente para aquele ditado.
- A seleção vale para o ditado inteiro e é persistida como associação entre ditado e erro.
- Erros arquivados não podem ser adicionados a uma nova seleção.
- Alterar a seleção atualiza as associações sem apagar respostas ou classificações já registradas.
- A consulta do ditado retorna a seleção para que a interface possa exibi-la e editá-la.

### CAT-06 — Absorvida por CAT-02 e CAT-03

A observação pedagógica é apenas um campo simples no erro, com série/ano opcional. Ela é criada junto com o erro em CAT-02 e atualizada junto com a definição não utilizada em CAT-03; não possui entidade, histórico ou operações independentes. A issue CAT-06 correspondente no Linear foi marcada como duplicada de CAT-02.

### CAT-07 — Importar erros por arquivo JSON

**Como** professor ou administrador, **quero** importar definições de erros em JSON, **para** cadastrar ou compartilhar itens do catálogo em lote.

**Critérios de aceite**
- O sistema valida estrutura, campos, tipos e versão/formato do arquivo antes de gravar itens.
- Erros de validação identificam item e campo; itens válidos são importados e itens inválidos são rejeitados com seus motivos, sem falha parcial silenciosa.
- A importação não sobrescreve identificadores existentes nem contorna regras de autoria e autorização.
- Itens com código estável duplicado são rejeitados sem sobrescrever o registro existente.
- O resultado informa itens criados, rejeitados e motivos.
- O contrato JSON e o tratamento de duplicatas são definidos antes da implementação.

### CAT-08 — Fazer backup e restaurar o catálogo

**Tipo:** enabler operacional.

**Como** responsável pela operação, **quero** gerar e restaurar cópias do catálogo, **para** recuperar dados diante de falha operacional.

**Critérios de aceite**
- O procedimento define frequência, retenção, localização segura, controle de acesso e criptografia quando aplicável.
- O backup do catálogo é versionado em disco e não é a única cópia de segurança do banco.
- Um teste de restauração documentado confirma que identificadores, anotações e associações necessárias ao histórico são preservados.
- O procedimento identifica responsáveis, passos de execução e verificação após restauração.
- As escolhas operacionais são registradas antes da adoção em produção.

### CAT-09 — Refinar escopo e preparar documentação do catálogo

**Tipo:** enabler de refinamento e documentação; não entrega funcionalidade diretamente ao usuário.

**Como** equipe do produto, **quero** revisar fluxos, dados, permissões e contratos do catálogo, **para** iniciar a implementação com decisões e critérios claros.

**Critérios de aceite**
- O pacote consolida escopo, atores, matriz de permissões e critérios de aceite.
- Os fluxos de publicação, consulta, edição, arquivamento/restauração, seleção por ditado, anotações e importação são documentados, incluindo erros e alternativas.
- O modelo ER cobre catálogo, autoria, arquivamento, observação pedagógica como campo do erro e associação ao ditado; não há entidade separada para observação.
- Os contratos da API e JSON incluem exemplos, validações e decisões de versionamento/duplicatas.
- Os esboços incluem estados de carregamento, vazio, sucesso, falha e acesso negado, com layout responsivo em computador e celular.
- As notas aplicáveis de segurança, privacidade, compatibilidade histórica, observabilidade e backup/restauração estão registradas.
- Pendências bloqueadoras têm decisão registrada ou risco explicitamente aceito.
- A documentação e os diagramas estão em `docs/epicos/` e `docs/diagramas/`, ligados à especificação global.

## Dependências e ordem sugerida

1. CAT-09 (refinamento dos contratos e nomes/descrições dos erros) antes das histórias de implementação.
2. CAT-01 e CAT-02 formam a base de consulta e publicação.
3. CAT-03 e CAT-04 dependem do modelo de autoria/estado do catálogo.
4. CAT-10 depende do arquivamento lógico implementado em CAT-04.
5. CAT-05 depende do catálogo ativo e da persistência da associação com ditado.
6. CAT-07 depende do contrato JSON, taxonomia e política de duplicatas.
7. CAT-08 depende das definições operacionais de backup e restauração.

## Decisões pendentes para refinamento

- Vocabulário inicial de nomes e descrições de erros e regras de identificação, sem categoria separada do nome.
- Campos obrigatórios e semântica dos exemplos/orientações; identificador/código estável.
- Contrato e versionamento do JSON do Marketplace e formato exato do relatório de itens rejeitados.
- Frequência, retenção, localização segura, criptografia e rotina de teste de restauração do backup.

## Revisão de cobertura para retomada

Esta revisão compara o rascunho com `ESPECIFICACAO_PG2.md`, especialmente as seções 2–10.5. As histórias CAT-01 a CAT-07 representam funcionalidades; CAT-08 é uma entrega operacional de backup/restauração; CAT-09 é um enabler de refinamento e documentação, não uma história funcional. O refinamento documental deve preceder as implementações.

### Lacunas e alinhamentos identificados

- CAT-01 restringe corretamente a consulta do catálogo ativo a professores e administradores; alunos não consultam o catálogo.
- A consulta e restauração de erros arquivados estão cobertas pela nova CAT-10: autor administra os próprios itens e administrador pode administrar qualquer item.
- CAT-02 usa o nome do erro como seu tipo; descrição, exemplos/orientações e regra executável opcional detalham como reconhecê-lo. Não criar dimensões/categorias separadas de natureza ou operação.
- A observação pedagógica e série/ano são campos do próprio erro e seguem as mesmas permissões; não há fluxo ou autorização separada para observações.
- CAT-07 permite importação por professores e administradores e rejeita códigos duplicados sem sobrescrever; o contrato/versionamento JSON ainda precisa ser refinado.
- CAT-03 impede alterações em erros já associados a ditados; para substituir uma definição usada, arquivar o registro e publicar outro, preservando referências históricas.
- CAT-08 permanece enabler operacional; frequência, retenção, localização segura, criptografia quando aplicável, controle de acesso e procedimento testado de restauração precisam ser definidos sem presumir valores.
- Aplicar o critério transversal de responsividade da seção 9 às histórias que envolvem interface.

### Ordem sugerida após o refinamento

1. Completar o enabler CAT-09 e registrar decisões bloqueadoras.
2. Implementar o modelo do catálogo e publicação (CAT-02), junto da consulta ativa (CAT-01).
3. Implementar edição, arquivamento, consulta e restauração com autorização e preservação histórica (CAT-03, CAT-04 e CAT-10).
4. Implementar seleção por ditado (CAT-05).
5. Implementar importação JSON (CAT-07) após estabilizar o contrato/versionamento e o formato do relatório de rejeições.
6. Entregar CAT-08 como procedimento operacional com restauração verificada.

As decisões acima permanecem pendentes; esta revisão não as converte em requisitos confirmados.

## Checkpoint CAT-09 — 6 nov 2026

Seleção por ditado mantida por decisão humana; CAT-05 continua requisito da COR-01. [Contrato em revisão](../../docs/epicos/catalogo-contrato.md) e [ER proposto](../../docs/diagramas/catalogo-er.md) detalham este refinamento sem autorizar implementação. PER-14 permanece In Progress; decisões bloqueadoras estão discriminadas no contrato.
