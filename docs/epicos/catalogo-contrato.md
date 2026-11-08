# CAT-09 — Contrato do Catálogo de erros para revisão

Estado: **proposta em revisão**, 6 nov 2026, FULLSTACK 01. PER-14 In Progress.
**Atualização de execução:** CAT-02 foi aprovada pelo usuário após o formulário revisado (“Ok, gostei”). Seu contrato vigente está em [marketplace-cat02.md](marketplace-cat02.md), incluindo exemplo estruturado opcional; prevalece sobre o rascunho CAT02-P1 abaixo. CAT-09 global continua In Progress. Código da única fatia CAT-02 está em implementação, sem antecipar demais histórias.
Versão documental: **CAT09-r3**, identificada por esta seção e pelo [relatório do ciclo](../relatorio-ciclo-cat09-2026-11-06.md); revisão local sem commit novo. CAT02-P1 abaixo aguarda aprovação humana.
Não autoriza implementação. Fonte global: [especificação](../../Ditado/docs/ESPECIFICACAO_PG2.md), §§2, 3.1–3.2, 10.5–10.6; [histórias](../../Ditado/docs/HISTORIAS_EPICO_CATALOGO.md).

### Alcance da revisão documental QA

QA oficial aceitou documentalmente os ajustes de identidade do resultado, marco na primeira associação e aviso de arquivamento, conforme retorno via SQUAD em 6 nov 2026. Não identificou novo bloqueador para **preparar** CAT-01/02. Isso não é validação funcional, aprovação de todos os campos/limites, autorização de implementação ou Done de CAT-09. Campos/limites de CAT-02, JSON/importação e backup continuam propostas onde indicado, sem aceite humano de adiamento. Destinos registrados não substituem aceite. O SQUAD consulta gerente sobre conduzir uma proposta concreta de CAT-02 sem alterar os critérios do enabler.

## Decisões confirmadas

- **Catálogo de erros** é o termo de produto vigente; “Marketplace” permanece apenas em nomes técnicos/documentos históricos. Todos os erros, inclusive os já programados, ficam disponíveis no Catálogo de erros e são opcionais individualmente. Nenhum tipo programado é obrigatório fora da seleção.
- Decisão humana: novos ditados exigem pelo menos um erro selecionado; antigos sem seleção continuam realização/submissão/nota e ficam sem classificação até configurados. Não selecionar todos nem recalcular histórico. A opcionalidade de cada item não revoga esse mínimo.
- O usuário confirmou **manter** a seleção de erros pelo professor em cada ditado. CAT-05/PER-10 continua requisito da COR-01/PER-22; não fechar COR-01 sem essa integração.
- Nome é o próprio tipo. Não criar categoria, natureza ou operação como dimensão separada. Omissão, Acréscimo e Substituição são três tipos.
- Catálogo compartilhado ativo para Professor/Administrador; aluno não consulta catálogo. Autoria e datas preservadas. Observação pedagógica em texto livre e série/ano opcionais no próprio erro, sem entidade/histórico independente.
- Seleção N:N vale para o ditado inteiro; somente regras dos erros selecionados participam da classificação. Múltiplos tipos independentes podem coexistir, sem tipo principal. Comparação objetiva e nota permanecem por código, sensíveis à caixa, ignorando somente espaços externos e analisando a lacuna.
- A diferença de caixa continua alterando acerto/nota independentemente da seleção. O filtro trata do rótulo de classificação Maiúsculas e pontuação, não da comparação objetiva.
- Transcrição da fala e Hipercorreção não recebem inferência automática nesta entrega. Sem regra confiável: não classificada/revisável, mantendo erro e nota. Nenhum fallback LLM agora.
- Não reutilizar Categoria/DitadoCategoria: são conceitos genéricos já existentes.

## Evidência atual e limite desta entrega

`Ditado.Dominio/Services/ClassificadorTiposErro.cs` devolve enums múltiplos; `Ditado.Aplicacao/Services/DitadoService.cs` aplica o classificador sem seleção persistida. `TipoErro.cs` preserva IDs legados e acrescenta 29–31. `RespostaSegmentoTipoErro` guarda enum, ainda sem referência ao catálogo. Não existem entidade/API de catálogo nem vínculo DitadoErro no código oficial.

Este pacote propõe contratos e diagramas; não cria código, migration, seed, dados, serviços ou funcionalidade. Importação e backup são tratados apenas documentalmente; implementação segue histórias próprias. Não inclui editor de código, expressões livres, motor novo de regras ou LLM funcionando.

## Modelo mínimo proposto

Nomes e representação abaixo são proposta técnica, não novos requisitos aprovados.

| Campo de ErroCatalogo | Contrato proposto |
|---|---|
| id | Inteiro gerado, identidade interna |
| codigo | Texto estável único; não derivar de alterações do nome; duplicatas rejeitadas sem sobrescrever |
| nome | Obrigatório, identifica o tipo |
| descricao | Texto que explica o padrão; proposta: obrigatório não vazio |
| exemplos / orientacao | Conteúdo pedagógico opcional; não executável |
| observacaoPedagogica / serieAno | Textos opcionais no próprio registro |
| regraCodigo | Chave opcional de regra homologada; nunca código ou linguagem natural executável |
| autorId | Obtido da autenticação, não escolhido pelo cliente |
| ativo | Ativo/arquivado; publicação inicia ativo |
| criadoEm / alteradoEm | Datas controladas pelo servidor |

### Cadastro CAT-02 — campos confirmados versus proposta

“Presente no modelo” não significa “digitação obrigatória no formulário”. As fontes confirmam os atributos abaixo; ainda não há lista humana fechada de validações, comprimentos ou formatos de entrada. Não introduzir valores por pressuposto.

| Grupo | Campo / estado vigente |
|---|---|
| Identificação confirmada | `nome` identifica o próprio tipo; `codigo` estável deve existir e ser único; autoria/datas/estado devem existir. São invariantes do registro. Não há categoria separada |
| Entrada obrigatória confirmada | `nome` é necessário para identificar a definição. A lista completa de campos obrigatórios do formulário ainda será fechada em CAT-02; o critério atual remete a campos “definidos para o catálogo” |
| Opcionais confirmados | Observação pedagógica e série/ano; exemplos/orientações podem existir. Regra executável não é requisito para publicação; novos registros docentes não recebem executor |
| Conteúdo previsto, obrigatoriedade ainda proposta | `descricao` integra a definição/consulta; exigir texto não vazio no cadastro é proposta. Estrutura exata de exemplos/orientações e todos os limites ainda não foram aprovados |
| Gerados/controlados pelo servidor — desenho técnico | `id`, `autorId` autenticado, datas, estado inicial ativo e marco da primeira associação; não pedir autoria/datas/marco ao professor. Identidade interna não é decisão de UI |
| Código estável — entrada versus geração ainda a detalhar | Existência/unicidade confirmadas; o rascunho JSON usa `codigo`, mas isso não confirma digitação manual obrigatória no formulário nem regra de geração. Fullstack prepara proposta simples e gerente revisa antes de implementar |
| Mapeamento interno dos itens programados | Item preparado pelo sistema aponta para executor existente. Sem campo de seletor/vínculo para professor; nenhum alias ou cópia executável proposto |

Base para próxima proposta simples, **não aprovação**: apresentar ao usuário formulário centrado em nome/descrição, observação e série opcionais; esclarecer código estável como detalhe de identidade e fechar somente obrigatoriedade/validações necessárias a CAT-02. Sem reabrir edição/arquivamento/filtro ou cenários de vínculo abandonados. Não antecipar escolhas operacionais de importação/backup.

### CAT02-P1 — proposta única de publicação para aprovação

Parecer final do gerente recebido via SQUAD: refinamento progressivo por história, preservando todos os critérios de CAT-09 global em In Progress. **Proposta ainda não aprovada pelo usuário.** Não iniciar código nem alterar CAT-02 para In Progress antes da resposta. Aprovação desta fatia não fecha CAT-09 nem implica aceite de risco/adiamento de importação ou backup.

Fluxo vertical proposto: Professor/Administrador abre **Publicar erro** no Catálogo de erros → preenche **Nome** e **Descrição** obrigatórios → opcionalmente preenche **Observação pedagógica** e **Série/ano** → publica → servidor persiste registro ativo → formulário apresenta confirmação com o item criado. Descrição pode conter exemplos/orientações em texto livre, sem campos extras dedicados nesta fatia. Novo erro não possui executor automático; tratamento por LLM é futuro, sem chamada nesta entrega.

Servidor gera código estável e ID; obtém autor da autenticação e controla datas/estado inicial ativo. Código não é digitado no formulário. Requisição ilustrativa proposta: `{"nome":"Exemplo de erro","descricao":"Descrição com exemplos e orientações livres.","observacaoPedagogica":null,"serieAno":null}`. `POST /api/ErrosCatalogo` devolve 201 com recibo do item criado, incluindo identidade/código/nome/descrição/autor/datas/estado e indicação de ausência de executor. Contrato exato é detalhe técnico a consolidar, mantendo compatibilidade e autorização. A consulta mínima do item ou uso da resposta POST serve somente à confirmação da publicação; não entrega a listagem ampla CAT-01.

**Aceites propostos:** professor/admin conseguem publicar e recebem confirmação do registro **ativo persistido**, com ID/código estável gerados e autoria correta; recarregar/consultar minimamente o item comprova persistência. Nome/descrição ausentes ou vazios são recusados no servidor e informados no formulário; opcionais podem ficar vazios. Aluno/anônimo são recusados pela API. Em falha, manter preenchimento e não mostrar sucesso; durante envio indicar carregamento e impedir acionamento duplicado. Item confirmado aparece explicitamente sem executor, sem classificação ou LLM disparados.

**Limite da fatia:** persistência/API de publicação, formulário e confirmação; sem listagem ampla/filtros CAT-01, edição, arquivamento, importação, associação ao ditado, cópias de executores ou execução LLM. Próxima história recomendada: CAT-01, sem início automático. Publicar confirma disponibilidade do registro no Catálogo de erros, mas não autoriza alegar CAT-01 entregue.

Detalhes de rota/JSON, índices e tamanhos técnicos razoáveis são responsabilidade Fullstack com parecer arquitetural aplicável, sem votação individual. Não introduzir limite de quantidade de erros, unicidade global de nomes, séries fechadas ou outra restrição de negócio. Limites técnicos escolhidos devem ser documentados e validados na implementação autorizada. As propostas genéricas de API/modelo neste pacote não ampliam o escopo CAT02-P1.

`DitadoErro(ditadoId, erroCatalogoId)` tem chave composta e FKs, sem duplicatas. A remoção de uma seleção não elimina definições ou respostas. O detalhamento técnico de identidade abaixo recebeu parecer do gerente como decorrência do requisito confirmado; segue revisão do pacote, sem migration nesta entrega.

**Marco confirmado de imutabilidade: primeira associação a qualquer ditado**, mesmo sem resposta ou classificação. Associar e desassociar antes de qualquer tentativa continua bloqueando edição. Proposta técnica: marcador persistido `utilizadoEm` preenchido na primeira associação, na mesma transação da seleção, nunca apagado por desassociação/arquivamento/restauração. Não é data da primeira resposta. Não introduzir histórico independente da observação; representação técnica ainda sujeita à revisão.

### Identidade do resultado — detalhamento técnico revisado pelo gerente

Estender `RespostaSegmentoTipoErro` com `erroCatalogoId` opcional para compatibilidade histórica, mantendo `TipoErro` existente. Na integração futura, cada classificação nova oriunda de uma definição selecionada grava o ID dessa definição e o enum homologado correspondente; o nome apresentado é o nome do registro associado. Não localizar definição pelo nome, nem atribuir catálogo retroativamente a resultados legados. Proposta de resposta aditiva: manter a lista legada `tiposErro` e acrescentar lista estruturada `classificacoes` com `erroCatalogoId`, `codigo`, `nome` e `tipoErro` numérico. Validar compatibilidade do DTO antes de implementar.

Exemplo testável proposto: definição 41, código `ACENTO_41`, nome `Nome X`, chave `ACENTUACAO`, selecionada; café→cafe gera `TipoErro=2` e `erroCatalogoId=41`. Resposta estruturada: `{"erroCatalogoId":41,"codigo":"ACENTO_41","nome":"Nome X","tipoErro":2}`. O campo legado `tiposErro` mantém a descrição histórica do enum (`Acentuação`) para clientes atuais; a UI integrada utiliza o nome cadastrado `Nome X` a partir da lista estruturada. Resultado antigo sem ID de catálogo continua apresentando a descrição legada, sem fabricar ID/código/nome cadastrado. Como definições já associadas são imutáveis, renomear não muda o nome dos resultados vinculados; arquivar preserva a consulta histórica. Estratégia de snapshot/versionamento só seria necessária se essa imutabilidade fosse alterada, fora desta proposta.

Parecer técnico do gerente recebido via SQUAD: FK nullable para legado e estrutura aditiva são detalhes decorrentes do requisito, sem nova votação de chave primária ao usuário. Na classificação nova integrada, erroCatalogoId é sempre preenchido e a UI usa classificacoes[].nome; tiposErro é fallback legado. Enum não é categoria nem tipo principal. A imutabilidade dispensa snapshot duplicado do nome. Destino: integração CAT-05→COR-01/PER-22 após revisão; não autoriza alterar DTO ou banco neste ciclo.

## Mapeamento interno Catálogo de erros–classificador (detalhamento em revisão)

Separar `codigo` da definição de `regraCodigo` da implementação: renomear um erro não cria nem altera algoritmo. Um registro sem vínculo executável é válido e sua ausência é exibida; sua seleção não produz classificação por código. Descrição e exemplos poderão apoiar LLM em história futura, sem promessa de execução atual.

Desenho técnico mínimo conforme parecer final do gerente: cada erro já programado tem um item preparado no Marketplace com vínculo interno ao executor existente; o professor somente seleciona itens e somente os selecionados executam. Novos erros publicados pelo professor não têm executor; não inferir pelo nome ou por configuração. Tratamento por LLM é futuro. Não criar restrições globais novas de nomes/cardinalidade para resolver cenários hipotéticos abandonados. Não há editor, seletor de vínculo, aliases ou cópias executáveis nesta fatia. Não há requisito humano de UI para vincular regra, aliases ou cadastros alternativos para uma mesma regra. O gerente consolidou esse desenho mínimo; a tabela abaixo documenta correspondências técnicas, não escolhas de regra no formulário. Não aceitar enum arbitrário enviado pelo cliente nem resolver pela semelhança do nome. Não oferecer Transcrição/Hipercorreção como regras automáticas.

| Chave proposta | Enum existente |
|---|---|
| ACENTUACAO | Acentuacao (2) |
| TROCA_GRAFEMAS_SURDA_SONORA | TrocaGrafemasSurdaSonora (19) |
| REGRA_CONTEXTUAL | RegraContextual (20) |
| GRAFIA_ARBITRARIA_IRREGULAR | GrafiaArbitrariaIrregular (21) |
| NASALIZACAO | Nasalizacao (22) |
| COMPLEXIDADE_SILABICA | ComplexidadeSilabica (24) |
| JUNTURA_SEGMENTACAO | JunturaSegmentacao (26) |
| CONCORDANCIA_FLEXAO | ConcordanciaFlexao (27) |
| MAIUSCULAS_PONTUACAO | MaiusculasPontuacao (28) |
| OMISSAO / ACRESCIMO / SUBSTITUICAO | OmissaoLetra (29) / AcrescimoLetra (30) / SubstituicaoLetra (31) |

IDs 18 e 25 permanecem históricos, sem emissão automática; 23 é agrupamento legado, sem atribuição nova. A tabela não renumera nem amplia padrões delimitados da §10.6. Reutilizar o classificador atual; o contrato deve garantir saída apenas dos vínculos selecionados. Não exigir fluxo paralelo.

## Permissões e histórico confirmados

| Ação | Professor | Administrador | Aluno/anônimo |
|---|---|---|---|
| Consultar ativos / publicar | Sim | Sim | Não |
| Editar definição nunca usada | Própria | Qualquer autor | Não |
| Editar definição já usada | Não | Não | Não |
| Arquivar | Própria | Qualquer autor | Não |
| Consultar arquivados / restaurar | Próprios | Qualquer autor | Não |
| Selecionar erros no ditado | Conforme autorização existente do ditado | Conforme autorização existente do ditado | Não |

Arquivamento é lógico, mantém IDs, associações e resultados antigos; impede novas inclusões. Restaurar preserva IDs e permite novas seleções. Substituir definição usada exige arquivar e publicar outro registro. Não introduzir filtro novo de autoria no ditado. Histórico já classificado nunca é recalculado por editar seleção.

**Reforço humano já confirmado:** o erro só pode ser editado enquanto não utilizado; uso começa na primeira associação a ditado. Tentativa de exclusão de erro utilizado deve informar que ele possui uso/histórico e será **arquivado**, preservando definição, vínculos e resultados, sem exclusão física. Isso consolida a política existente, sem reabrir decisão de edição/exclusão/arquivamento.

Esboço do aviso: título “Arquivar erro utilizado”; texto “Este erro já foi associado a um ditado e possui referências que precisam ser preservadas. Ele não pode ser editado nem apagado fisicamente. Ao continuar, será arquivado e deixará de estar disponível para novas seleções. As associações e os resultados existentes serão preservados.” Ações “Cancelar” e “Arquivar erro”. Exibir somente ação autorizada: professor para item próprio, administrador para qualquer autor; a API verifica as mesmas permissões. Cancelar fecha sem requisição de escrita; confirmar envia apenas arquivamento lógico. Não alegar no aviso se o executor continuará ou cessará nas novas tentativas, lacuna operacional distinta com destino CAT-04/CAT-05→COR-01, a definir antes de seu consumo.

Fonte humana recebida via SQUAD, 6 nov 2026, registrada literalmente: “Nós já definimos isso, nesse caso arquivamos o erro. E o erro só pode ser editado enquanto não é usado, na exclusão deve avisar isso para o usuario.” A resposta não escolheu expressamente execução ou cessação da regra em novas tentativas; não inferir opção por silêncio.

**Pendência: nova tentativa em ditado que já selecionava um erro posteriormente arquivado.** Preservar associação e leitura histórica não decide se a regra ainda executa para a nova tentativa. Alternativas: manter execução da definição originalmente selecionada ou cessar sua classificação após arquivamento. Nenhuma foi escolhida neste pacote. O gerente recomenda manter execução nos ditados já vinculados até remoção da seleção, sem permitir novos vínculos. Resposta recebida reforça edição/exclusão; o efeito operacional nas novas tentativas não foi especificado. Não atribuir nenhuma alternativa ao usuário. Parecer final do gerente: destinar a lacuna a CAT-04/CAT-05→COR-01 antes de consumir essa política; não bloqueia a preparação do modelo, consulta e publicação CAT-01/02. Isso não é aceite humano de risco, não define integralmente a integração e não autoriza concluir CAT-09 nem iniciar CAT-02. Resultados já gravados permanecem intactos em qualquer alternativa.

## Contratos mínimos de API propostos

Não existem ainda; rotas/payloads são para revisão. Seguir autorização no servidor e convenções atuais de erro. 401 anônimo, 403 papel/autoria insuficiente, 404 recurso inexistente; proposta: 400 campos/referências inválidos e 409 código duplicado/edição de definição usada. Listagens não expõem e-mail, credenciais ou dados pessoais além da autoria necessária.

| Operação | Proposta |
|---|---|
| Consulta CAT-01 | `GET /api/ErrosCatalogo` retorna ativos e `GET /api/ErrosCatalogo/{id}` detalha conforme permissão |
| Publicação CAT-02 | `POST /api/ErrosCatalogo` retorna 201, registro e Location |
| Edição CAT-03 | `PUT /api/ErrosCatalogo/{id}`; não altera identidade/código/autoria; bloqueia usado |
| Arquivar / restaurar CAT-04/10 | `POST /api/ErrosCatalogo/{id}/arquivar` e `/restaurar`; consulta `?estado=arquivado` aplica autoria |
| Seleção CAT-05 | Acrescentar `erroIds` nos requests existentes de criar/editar ditado e `errosSelecionados` na consulta autorizada; sem nova cópia da correção |

Publicação exemplificativa sem regra (proposta):

```json
{
  "codigo": "HIPERCORRECAO_PROF_01",
  "nome": "Hipercorreção / supergeneralização",
  "descricao": "Definição pedagógica para análise futura.",
  "observacaoPedagogica": "Considerar o contexto antes de interpretar a motivação.",
  "serieAno": null,
  "regraCodigo": null
}
```

Resposta exemplificativa: `{"id":42,"codigo":"HIPERCORRECAO_PROF_01","nome":"Hipercorreção / supergeneralização","regraCodigo":null,"temRegraExecutavel":false,"autorId":2,"ativo":true}`; resposta real deve incluir descrição e datas. Datas e autor não são campos aceitos do cliente. Exemplo não cria esse registro.

Seleção em edição autorizada: `{"erroIds":[41,42]}` junto aos campos existentes; consulta retorna `errosSelecionados` com id, código, nome, estado e disponibilidade de regra. Proposta: campo omitido na edição preserva seleção; lista fornecida substitui conjunto atomicamente após validar IDs e permissões. Não transformar campo ausente em seleção de todos. Decisão humana: novos ditados exigem ao menos um erro selecionado. Legados sem seleção continuam realização, submissão e nota, sem classificação até configurados; não preencher todas as regras nem recalcular histórico. O comportamento de apagar uma seleção já configurada deve respeitar esse contrato, sem inferir exceção para novos. Seleção de IDs inexistentes ou arquivados novos falha sem gravar parte do conjunto. Registros arquivados já associados permanecem identificáveis; não apagá-los implicitamente.

## Fluxos e esboços

Catálogo: carregar → lista ativa (nome, descrição, indicação com/sem regra) ou vazio → publicar → validar → sucesso e item ativo; falha mantém formulário e mensagem. Edição só aparece quando autorizada e nunca usada; usada orienta arquivar e criar definição nova. Arquivados têm lista separada conforme autoria, com restauração autorizada. Falhas da API não podem ser apresentadas como sucesso.

Ditado: formulário existente → carregar catálogo → escolher itens ativos → salvar conjunto → reabrir e conferir seleção. Item sem regra informa que não gera classificação automática nesta entrega. Item arquivado já associado aparece como histórico, sem ser oferecido como inclusão nova. Não esconder seleção por falha de carregamento nem salvar um conjunto vazio involuntário.

Esboço desktop: lista com nome, descrição, estado de regra e ações; formulário lateral/página de publicação. Celular: cartões e formulário em uma coluna, seleção com rótulos legíveis, ações acessíveis sem depender de hover. Ambos: carregamento, lista vazia, validação, sucesso, erro recuperável e acesso negado. Sem catálogo disponível, informar estado e preservar preenchimento; novos ditados não podem ser criados sem ao menos um erro selecionado, sem invalidar realização/submissão de legados sem seleção.

## Importação e operação — limites do refinamento

CAT-07: Professor/Administrador importa JSON versionado; código duplicado é rejeitado, nunca sobrescrito; resultado discrimina criados e rejeitados por item/campo. Proposta de envelope: `{"schemaVersion":1,"erros":[{"codigo":"EXEMPLO","nome":"Exemplo","descricao":"...","regraCodigo":null}]}` e retorno `{"criados":[{"indice":0,"id":42}],"rejeitados":[{"indice":1,"campo":"codigo","motivo":"CodigoDuplicado"}]}`. Estrutura/versão inválida rejeita arquivo; proposta de validação item a item e transação por item para resultado explícito. Autoria/datas/IDs são do servidor; importação não libera regras desconhecidas. Limites de tamanho, obrigatoriedade exata e semântica de exemplos ainda precisam de refinamento antes de CAT-07, sem bloquear artificialmente a proposta mínima de CAT-02.

CAT-08: preservar identificadores, campos e associações no backup/restauração; backup em disco não substitui cópia do banco. Frequência, retenção, local, acesso, criptografia aplicável, responsável e teste de restauração permanecem escolhas operacionais a revisar antes da execução. Nenhum valor foi presumido aqui.

Segurança/observabilidade: autorização em chamada direta; validação no servidor; texto pedagógico tratado como texto; não registrar tokens, áudio nem respostas infantis em logs de diagnóstico. Proposta: logs de ação, recurso, resultado e correlação, com dados mínimos. Compatibilidade: preservar enums/DTOs legados e leituras históricas; não converter categorias genéricas, semear associações antigas ou alterar fixtures humanas. Limites exatos de campos são detalhes de contrato a fechar antes da implementação, sem inventar valores neste rascunho.

## Questões técnicas e decisões de produto pendentes

Conforme correção explícita do usuário e AGENTS.md vigente, researcher é consultado somente sobre os três PDFs e pedagogia de língua portuguesa. Seleção, validações de formulário, cardinalidade e vínculo técnico não exigem seu parecer por si só. Fullstack trata implementação; gerente_projetos é consultado sobre arquitetura, contratos, persistência e gestão. O usuário decide escopo. Eventual critério pedagógico deve ser separado da questão técnica antes da consulta.

Os pareceres técnicos abaixo foram devolvidos pelo SQUAD DITADO, chat `01a1aa39-f543-7cc1-9825-3bbda26ecd38`, em 6 nov 2026. Não houve consulta direta ou acesso ao parecer integral por este chat. **Recomendações não são decisões humanas; as escolhas pendentes não serão implementadas.**

| Questão | Alternativas para decisão | Parecer técnico atual do gerente / incerteza |
|---|---|---|
| Seleção mínima | Confirmada pelo usuário: novos exigem ≥1; legados sem seleção seguem realização/submissão/nota, sem classificação até configurados | Não preencher todas nem recalcular histórico. “Opcionais” por item não revoga o mínimo aprovado |
| Catálogo de erros inclui erros programados | Confirmado pelo usuário: todos os erros, inclusive já codificados, são itens disponíveis e opcionais no mesmo Catálogo de erros | Nenhum tipo universal obrigatório; nota objetiva independente da classificação |
| Perguntas antigas sobre vínculo manual e duplicação | Q2/Q3 retiradas da fila humana: pressupunham cenários hipotéticos fora do fluxo confirmado; não foram aprovadas | Gerente consolidou mapeamento interno dos itens programados; sem seletor, aliases, cópias executáveis ou novas restrições globais de nomes/cardinalidade |
| Nova tentativa após arquivar item já selecionado | Gerente recomenda continuar em ditados já vinculados, cessando apenas com remoção da seleção; alternativa: cessar classificação nas tentativas futuras | Resposta recebida reforça edição/exclusão; efeito nas novas tentativas não especificado. Destino CAT-04/CAT-05→COR-01 antes de consumir política, sem aceite de risco |
| Interpretação do filtro | Somente regras selecionadas, incluindo rótulos de caixa e operações | Decisão já confirmada na especificação; não reabrir nem atribuir aprovação aos consultores |

Fontes de produto/técnicas: especificação §§2/3.2/10.6, CAT-02/05/09, PER-7/10/22 e código citado. A justificativa textual para mínimo um item é CAT-05 em HISTORIAS_EPICO_CATALOGO.md. Não há fundamento pedagógico atribuído aos PDFs para políticas de seleção, cardinalidade ou permissão.

Registro de correção: opiniões anteriores do researcher sobre vazio/cardinalidade foram recebidas antes do ajuste de papel; não fundamentam estas decisões técnicas nem constituem autoridade pedagógica sobre elas. O [índice dos artigos](../referencias/README.md) permanece fonte somente para conteúdo linguístico/pedagógico pertinente. A revisão atual não formula nova dúvida pedagógica.

## Critérios testáveis para revisão e implementação posterior

1. Publicar por Professor/Administrador gera código único, autor autenticado e datas; aluno/anônimo são recusados. Sem regra permanece explícito e não executa inferência.
2. Consulta ativa compartilhada retorna nome=tipo e exclui arquivados, sem campos pessoais desnecessários. CAT-02 e CAT-01 permanecem histórias distintas.
3. Associar definição a ditado e desassociar antes de qualquer resposta continua impedindo edição por professor e administrador. O marco é a primeira associação, não primeira classificação. Autor/admin arquivam sem apagar vínculos e consultam/restauram conforme matriz.
4. Seleção autorizada persiste N:N sem duplicatas; reconsulta reproduz conjunto; inclusão de arquivado novo ou ID inexistente falha atomicamente. Alteração preserva classificações anteriores.
5. Após integração futura, selecionar apenas Acentuação em café→Cafe não autoriza nome de regra não selecionada; nota continua errada por caixa. Selecionar regras independentes correspondentes permite múltiplos tipos. Sem regra aplicável mantém não classificada e nota objetiva.
6. Sem LLM, Transcrição/Hipercorreção nunca são inferidas automaticamente. IDs históricos não são renumerados; 23 não é emitido em resultados novos.
7. Criar novo ditado sem seleção é recusado; com ao menos um erro selecionado pode prosseguir conforme demais validações. Legado vazio continua realização/submissão/nota e não classifica até configurado. Não selecionar todos automaticamente nem recalcular resultados antigos. Nenhum tipo individual é obrigatório.
8. Revisão confirma links, ER, contratos, permissões e destino de cada pendência; QA revisa critérios posteriormente. CAT-09 só termina com pacote revisado e bloqueadores resolvidos ou risco aceito explicitamente.
9. Na implementação futura da identidade detalhada, o exemplo definição 41→Nome X→ACENTUACAO persiste 41/2, devolve nome cadastrado na estrutura nova e mantém descrição do enum no campo legado. Resultado antigo sem vínculo não recebe ID inventado.
10. Antes de implementar o consumo da política em CAT-04/CAT-05→COR-01, registrar o resultado esperado para nova tentativa com erro já selecionado e depois arquivado. A resposta humana recebida fecha edição/exclusão, mas não especifica esse efeito. Não presumir execução nem cessação. A preparação de modelo/consulta/publicação pode ser revisada independentemente; CAT-09 continua sem aprovação global/aceite humano de risco.
11. Em tentativa autorizada de excluir erro utilizado, a interface avisa que existe uso/histórico e oferece arquivar preservando referências. Cancelar não envia escrita e não muda estado/dados. Confirmar arquiva logicamente, preservando definição, associações e resultados. Professor de outro autor/aluno/anônimo não conseguem arquivar, inclusive por chamada direta; professor autor e administrador seguem a matriz. A definição continua não editável após arquivar/restaurar ou remover a seleção.

## Registro de pendências, responsáveis e destinos

Este registro identifica destino, **não aceita adiamento nem risco implicitamente**. Nenhuma linha tem aceite humano de postergação registrado. CAT-09 continua In Progress; o coordenador deve obter decisão ou aceite explícito do risco pertinente antes de concluir o enabler.

| Pendência | Responsável pela preparação / decisão | Destino e condição |
|---|---|---|
| Mapeamento interno dos itens programados no Catálogo de erros | Fullstack detalha conforme parecer final do gerente; QA revisa | CAT-09 revisão técnica; Q2/Q3 retiradas da fila humana. Itens programados preparados com executor interno; publicação docente sem executor. Sem fluxo extra ou restrições globais novas |
| Identidade persistida/devolvida da classificação | Fullstack detalha; gerente já deu parecer técnico; QA revisa contrato | CAT-09 revisão, implementação futura COR-01/PER-22; sem nova decisão de produto sobre PK/FK |
| Nova tentativa após arquivar erro já selecionado | SQUAD coordena definição antes do consumo, com parecer técnico gerente; usuário decide eventual escopo | CAT-04/CAT-05→COR-01; resposta sobre edição/exclusão recebida, efeito operacional não especificado. Não bloqueia preparar modelo/consulta/publicação; sem aceite humano de risco ou conclusão global CAT-09 |
| Campos obrigatórios, limites e exemplos/orientações exatos | Fullstack prepara contrato, gerente revisa; usuário decide escopo que restar | Fechar base de CAT-02 antes de sua implementação; qualquer transferência da definição exige aceite explícito para concluir CAT-09 |
| JSON: versão, limites de arquivo e erros por item | Fullstack/gerente preparam; usuário decide escopo pendente | Refinamento CAT-07 antes da implementação; eventual adiamento a CAT-07 precisa aceite explícito para CAT-09 |
| Frequência/retenção/local/acesso/criptografia aplicável e responsável operacional de backup | Gerente prepara com responsável operacional a designar pelo usuário | CAT-08 executa e testa restauração; decisões/adiamento não aceitos implicitamente por CAT-09 |
| Revisão do pacote e critérios | QA documental identifica lacunas; SQUAD coordena; usuário resolve escopo/aceita riscos | CAT-09; revisão recebida com ajustes, sem aprovação final nem Done |

Diagrama: [modelo proposto](../diagramas/catalogo-er.md). A próxima história sugerida é CAT-02, **somente após preparação e autorização**, seguida de CAT-01, CAT-05 e integração da COR-01; a sequência é recomendação, não início automático.
