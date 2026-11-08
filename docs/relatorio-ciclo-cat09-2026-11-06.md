# Relatório do ciclo CAT-09 — 6 nov 2026

## Resultado e motivo

Proposta documental preparada para revisão, sem implementação funcional. A decisão humana de **manter a seleção de erros por ditado** foi registrada no Linear; COR-01/PER-22 permanece In Progress por depender de CAT-05. CAT-09/PER-14 saiu de Backlog para In Progress e não foi concluída.

## Arquivos e alterações

- `docs/epicos/catalogo-contrato.md`: escopo confirmado, evidências do código, modelo mínimo proposto, mapa explícito de regras/enum, permissões e histórico, contratos API/JSON, fluxos/esboços responsivos, segurança, compatibilidade, operação e critérios testáveis. Decisões abertas separadas de propostas para evitar implementação presumida.
- `docs/diagramas/catalogo-er.md`: ER Mermaid proposto com autoria, campos pedagógicos no próprio erro e associação N:N; sem reaproveitar categorias genéricas nem criar editor de regra.
- `Ditado/docs/ESPECIFICACAO_PG2.md`: corrigida somente a afirmação inconsistente de que todos os doze tipos são automáticos; explicitados os dois adiamentos e os três tipos de operação já decididos. Acrescentados links ao refinamento.
- `Ditado/docs/HISTORIAS_EPICO_CATALOGO.md`: checkpoint e links para contrato/ER, preservando conteúdo anterior.
- Este relatório: evidência do ciclo e limites. Os AGENTS.md previamente alterados pelo coordenador foram preservados; não são implementação deste ciclo.

## Linear e coordenação

Lidas PER-14, PER-7, PER-6, PER-10 e PER-22. PER-7 foi alinhada a nome=tipo sem dimensões extras; PER-6 continha a mesma inconsistência de natureza/tipo e recebeu correção equivalente, sem mudar escopo. Estados funcionais dessas histórias não foram avançados. Comentários em PER-14/PER-22 registram início, limites e decisão humana.

Solicitação concreta enviada via SQUAD aos consultores researcher/gerente_projetos para seleção vazia/ditados antigos, vínculo catálogo–regra e interpretação do filtro. O SQUAD devolveu os pareceres: researcher recomenda mínimo um item em novos e uma definição ativa por chave; gerente recomenda vazio permitido e múltiplos cadastros por chave, limitados a um por seleção. Divergências registradas sem escolha. Gerente recomenda vínculo homologado somente pelo sistema e publicação docente sem seletor de regra. Ambos confirmam filtro sem exceções, sem impacto na nota objetiva. O coordenador está ajustando a formulação das questões; não há confirmação de perguntas enviadas ao usuário; não houve consulta direta ou acesso aos pareceres integrais por este chat. Referências principais: especificação §§2/3.1/3.2/10.5/10.6, histórias CAT-01/02/05/09, Linear citado, código oficial ClassificadorTiposErro/TipoErro/DitadoService e índice `docs/referencias/README.md`. Não houve nova conclusão linguística nem consulta aos artigos integrais neste ciclo; o resumo dos pareceres não trouxe páginas adicionais de artigos.

## Validação e limites

Revisão documental do diff, ligações locais e consistência com o código consultado. Nenhum teste foi executado: entrega somente documental, sem alteração comportamental. Não houve execução de QA, renderização do Mermaid ou alegação de contrato implementado. Não houve modificação de app, frontend, dados, migrations, processos, serviços, branches, commit ou push; fixtures humanas e cópia antiga não rastreada preservadas. Não houve nova consulta ao remoto Git.

## Pendências e próximo passo

O usuário ainda decide seleção vazia/legado e política do vínculo catálogo–regra após pareceres. O texto confirmado de produto e o parecer técnico do gerente sustentam o filtro sem exceções, incluindo caixa e operações; não é nova pergunta ao usuário e a nota objetiva nunca depende da seleção. Formatos exatos, limites de campos e detalhes de importação/operação continuam propostas ou refinamentos de suas histórias, sem valores inventados. CAT-09 não está Done: requer revisão do pacote e resolução ou aceitação explícita das pendências pertinentes. Nenhuma história funcional inicia automaticamente.

## Retificação de responsabilidades e parecer técnico

Após correção explícita do usuário, researcher é consultor apenas dos PDFs e da pedagogia de português. A consulta anterior sobre seleção/cardinalidade excedeu esse papel; suas opiniões sobre esses temas não fundamentam decisões técnicas. O registro anterior acima descreve o checkpoint recebido naquele momento, não o parecer técnico vigente. Fullstack trata implementação e gerente_projetos orienta arquitetura/contratos/persistência/gestão; o usuário decide escopo.

O SQUAD comunicou revisão do gerente: recomendar pelo menos um erro em novos ditados; legados sem seleção mantêm nota e ficam sem classificação. A recomendação anterior de vazio permitido em novos foi substituída, sem decisão humana inferida. Para cardinalidade, o gerente recomenda vários cadastros por chave com no máximo um por seleção de ditado; para vínculo, somente sistema homologado, sem seletor docente inicial. Essas escolhas continuam pendentes da decisão humana. A proposta foi retificada para usar fontes de produto e parecer técnico pertinente, sem nova consulta pedagógica nem alteração funcional. PER-14 segue In Progress.

## Ajustes após revisão documental do QA

Fonte: revisão do QA oficial devolvida pelo SQUAD, 6 nov 2026. O pacote cobre os tipos de entregáveis, mas não foi aprovado para Done. As três perguntas ao usuário foram enviadas posteriormente ao checkpoint acima; não há respostas recebidas nesta revisão.

Corrigido o marco de imutabilidade confirmado: primeira associação a qualquer ditado, mesmo removida antes de qualquer tentativa. Contrato, ER e aceite agora explicitam associar→desassociar sem desbloquear edição; o marcador proposto não espera uma classificação.

Adicionada proposta testável de identidade: definição 41/Nome X/ACENTUACAO corresponderia a ID de catálogo 41 e enum 2, com nome cadastrado em estrutura nova e descrição do enum preservada na lista legada. ER representa referência opcional de RespostaSegmentoTipoErro a ErroCatalogo para preservar legados sem remapeamento. Isso é proposta pendente de parecer arquitetural/revisão; não há alteração de DTO, banco ou aprovação tácita.

Nova tentativa após arquivar erro já selecionado foi registrada como pendência explícita: preservar histórico não determina manter ou cessar execução futura. SQUAD consulta gerente sobre política e eventual decisão de escopo. Acrescentada matriz de responsáveis/destinos para escolhas humanas, identidade, arquivamento, campos/limites, JSON, operação/backup e revisão. Identificar CAT-07/CAT-08 como destino não aceita postergação: nenhum risco ou adiamento foi aceito pelo usuário neste checkpoint. CAT-09 continua In Progress.

Validação dos ajustes: conferidos contratos atuais de RespostaSegmentoTipoErro/ResultadoDitadoResponse e descrição de Acentuação; diff --check sem erros. Sem testes, app, migrations, serviços, commit ou push.

## Decisões e esclarecimento posteriores do usuário — estado vigente

O usuário aprovou exigir pelo menos um erro em novos ditados e preservar antigos sem seleção: realização/submissão/nota continuam, sem classificação até configurados, sem selecionar todas as regras nem recalcular histórico. Esta decisão substitui a pendência de seleção mínima nos checkpoints anteriores.

O usuário esclareceu que todos os erros, inclusive já programados, devem ser opcionais por item e disponíveis no mesmo **Marketplace**, termo de produto vigente para o catálogo. Não existe grupo obrigatório de regras fora da seleção; a nota continua objetiva e independente. Opcionalidade individual não revoga o mínimo aprovado. Contrato e título do ER foram alinhados; nomes técnicos ErroCatalogo/rotas ainda são propostas internas, sem renomeação de código.

As perguntas Q2/Q3 não foram aprovadas: o usuário pediu esclarecimento. A formulação pressupunha vínculo manual/duplicação ainda sem fluxo funcional confirmado. Foi retirada da tabela de decisões para revisão pelo gerente do mapeamento interno mínimo, sem inventar seletor, aliases ou cadastros alternativos. Nenhuma resposta incompreendida virou escolha.

Gerente deu parecer técnico favorável a identidade aditiva, FK nullable somente para legado, nova classificação integrada sempre vinculada e UI usando nome da definição; enum é compatibilidade, não categoria/tipo principal. Não é nova votação de PK/FK. Para nova tentativa após arquivamento, recomenda manter regra em ditados já vinculados e cessar por remoção da seleção, mas essa escolha de produto ainda não foi enviada ao usuário e continua pendente. Contrato, ER e critérios atualizados; CAT-09 permanece In Progress. Validação documental por diff --check, sem testes ou alterações funcionais.

## Consolidação do parecer final do gerente

SQUAD devolveu desenho mínimo: cada erro programado tem item preparado no Marketplace e vínculo interno ao executor; somente itens selecionados executam. Novos cadastros docentes não têm executor, sem inferência por nome/configuração e sem LLM nesta fatia. Q2/Q3 saíram da fila humana por tratarem cenários hipotéticos; não criar restrições globais de nome/cardinalidade, seletor de vínculo, aliases ou cópias executáveis. Contrato e ER consolidados nesses termos. Mínimo de um em novos permanece confirmado e não foi reaberto.

Cronologia vigente: SQUAD enviou somente a questão real de arquivamento ao usuário: manter em ditados já vinculados até remoção da seleção (recomendação gerente) ou cessar nas novas tentativas. Sem resposta recebida; ambas alternativas preservadas. PER-14 In Progress, sem aceite tácito de risco ou Done. Apenas ajustes documentais; diff --check sem erros, sem testes/app/migrations/commit/push.

## Reforço humano sobre exclusão de erro utilizado

Resposta recebida posteriormente via SQUAD: “Nós já definimos isso, nesse caso arquivamos o erro. E o erro só pode ser editado enquanto não é usado, na exclusão deve avisar isso para o usuario.” Registrada literalmente no contrato e neste histórico. Consolida política já confirmada: primeira associação marca uso; depois disso não editar nem apagar fisicamente definição/vínculos/resultados. Não se trata de nova indecisão sobre exclusão/arquivamento.

Acrescentado esboço de aviso com ações Cancelar/Arquivar erro e aceite testável: informar uso/histórico, cancelar sem escrita, confirmar arquivamento lógico preservando referências e aplicar autoria/papel na UI e API. Não há implementação de modal ou API neste ciclo documental.

A resposta não escolheu expressamente se o executor continua ou cessa em novas tentativas; nenhuma dessas alternativas foi marcada aprovada. SQUAD consulta gerente sobre como explicitar política sem repetir pergunta desnecessária. CAT-09 segue In Progress até revisão. Sem novas perguntas diretas, testes, código, dados, migration ou commit/push.

## Parecer final sobre alcance da resposta de arquivamento

Consolidado o estado vigente: resposta recebida reforça edição/exclusão; efeito operacional nas novas tentativas não foi especificado. Não registrar como ausência de resposta humana nem atribuir opção de execução/cessação. Edição antes da primeira associação e exclusão de usado como arquivamento com aviso/histórico estão fechadas, sem nova pergunta.

Gerente recomenda que a lacuna estreita de novas tentativas tenha destino CAT-04/CAT-05→COR-01, definida antes do consumo da política. Não bloqueia preparar modelo, consulta e publicação CAT-01/02. Contrato/ER/critério e matriz de donos/destinos atualizados; isso não significa aceite humano de risco, integração plenamente definida, conclusão de CAT-09 ou autorização de CAT-02. Aguarda-se revisão documental dos ajustes QA. Sem código, testes, serviços, migrations, commit ou push.

## Revisão QA recebida — versão CAT09-r2

QA oficial aceitou documentalmente os três ajustes: identidade de resultado, primeira associação como marco e aviso de arquivamento. Retorno recebido via SQUAD; não surgiu novo bloqueador para preparação de CAT-01/02. Não é aprovação funcional, lista completa de campos ou conclusão global do enabler.

Contrato identificado como CAT09-r2 (revisão local, sem commit novo). Acrescentada tabela de cadastro distinguindo invariantes confirmadas, entrada necessária, opcionais, conteúdo com obrigatoriedade proposta, campos gerados/controlados pelo servidor e código estável cuja forma de entrada/geração ainda precisa detalhamento. Nome=tipo e código estável/único estão confirmados; exigir descrição não vazia e limites de campos continuam proposta. Não inventar obrigatoriedade de digitar código por causa do exemplo JSON.

CAT-09 permanece In Progress: campos/limites CAT-02, JSON/importação e backup não tiveram aceite humano implícito. SQUAD consulta gerente para levar proposta simples de cadastro ao usuário, sem mudar sozinho os critérios do enabler, antecipar operação ou criar nova issue. Nenhuma implementação/teste/status Done.

## CAT09-r3 / CAT02-P1 — proposta para aprovação humana

Acrescentada proposta vertical única conforme parecer final do gerente: professor/admin publica erro no Marketplace com Nome+Descrição obrigatórios (ainda proposta), descrição incluindo exemplos/orientações livres sem campos extras; Observação e Série/ano opcionais. Servidor gera código/ID, autoria autenticada, datas e estado ativo. POST/persistência/formulário/recibo devem confirmar registro ativo persistido e sem executor; LLM fica futuro. A leitura mínima é suporte à confirmação, não entrega CAT-01.

Fora da proposta: listagem ampla/filtros, edição/arquivamento, importação, seleção por ditado e execução LLM. Próxima história sugerida CAT-01, sem início automático. Detalhes técnicos razoáveis serão definidos pelo Fullstack quando autorizado; não criar limites de negócio ou votação de cada campo técnico.

SQUAD levará a proposta inteira à aprovação; ainda não há resposta recebida. CAT-02 não foi iniciada nem teve status alterado. Refinamento progressivo proposto preserva CAT-09 In Progress e seus critérios globais intactos, sem aceite implícito de risco/adiamento para importação/backup. Apenas rascunho documental CAT09-r3, sem código/testes/serviços/migrations/commit/push.
