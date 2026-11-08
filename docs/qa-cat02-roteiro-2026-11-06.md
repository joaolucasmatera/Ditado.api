# Roteiro QA da publicação de erro no Marketplace

Preparado em 6 de novembro de 2026 para CAT02/PER7. **Planejamento, ainda não executado.** Aguardar entrega do Fullstack 01, confirmação da versão ativa e coordenação de migração/reinício pelo SQUAD DITADO. Não confundir maquete aprovada com persistência implementada.

## Base e limites

Decisão humana encaminhada pelo SQUAD e critérios atuais lidos em PER7: Nome e Descrição obrigatórios; um grupo Exemplo opcional com Escrita esperada e Resposta do aluno, ambas preenchidas quando o grupo é informado, sem lista repetível; Observação pedagógica e Série/ano opcionais. Tudo inicia vazio, com placeholders cinza, labels e helpers visíveis, conforme maquete aprovada. Servidor gera ID/código estável único, autoria autenticada, datas e estado ativo. Publicação docente nova não tem executor, não infere regra e não aciona classificação, feedback ou LLM.

Maquete de referência: `C:\Users\lucas\.codex\visualizations\2026\11\05\01a1aa39-f543-7cc1-9825-3bbda26ecd38\formulario-erro-marketplace.html`. A aprovação desta vertical não conclui CAT09 global.

Somente publicação e confirmação/consulta mínima do item criado. Não testar como requisito desta entrega listagem ampla CAT01, importação, edição, arquivamento, seleção por ditado ou integração da classificação. Preservar integralmente Ditado1/Resposta1 e ditado humano8, suas respostas e vínculos. Não aplicar migration nem reiniciar serviços pelo QA.

## Matriz de validação

| ID | Camada e cenário | Resultado esperado |
| --- | --- | --- |
| QA01 | Abrir formulário como professor | Nome, Descrição, duas entradas do Exemplo, Observação e Série/ano vazios; exemplos apenas nos placeholders, nunca como valores enviados |
| QA02 | Rótulos e ajuda | Labels identificam cada campo mesmo após digitar; helpers legíveis; Nome/Descrição indicados obrigatórios e demais opcionais; indicação esperado → resposta sem inverter sentido |
| QA03 | Desktop e celular | Conferir visualmente em largura desktop e móvel; leitura e ação Publicar acessíveis, campos/erros sem corte ou sobreposição; placeholders cinza legíveis e campos não dependem só deles para identificação |
| QA04 | Professor publica só obrigatórios pela UI | Sucesso real, registro ativo confirmado com ID/código; opcionais ausentes ou vazios não impedem criação; recibo corresponde ao digitado |
| QA05 | Administrador publica pela UI | Mesmo fluxo funcional, com autoria do administrador autenticado |
| QA06 | Publicação com todos os campos | Nome, Descrição, par de exemplo, observação e série persistem e reaparecem corretamente no recibo e consulta mínima |
| QA07 | POST direto como professor e administrador | Resposta201 e identidade do item; persistência comprovada, não somente mensagem de sucesso ou objeto temporário |
| QA08 | Nome ausente, vazio ou somente espaços | Servidor recusa sem criar registro; UI informa campo, mantém os outros valores e não anuncia sucesso |
| QA09 | Descrição ausente, vazia ou somente espaços | Mesmo comportamento, independentemente de validação HTML/frontend |
| QA10 | Todos opcionais omitidos, nulos ou vazios | Publicação válida conforme representação do contrato entregue; não exigir preenchimento automático nem salvar placeholder |
| QA11 | Exemplo completo | Duas partes persistidas sem troca de sentido; ausência do grupo inteiro continua válida |
| QA12 | Exemplo com uma única parte, nas duas direções | Recusar com mensagem pertinente sem criar item nem perder o outro conteúdo; ambas as partes são exigidas somente quando o grupo é informado, conforme AC atual de PER7. Grupo totalmente vazio permanece válido |
| QA13 | Aluno chama POST diretamente | HTTP403 e nenhum item criado; formulário de publicação não acessível pelo papel |
| QA14 | Anônimo chama POST diretamente | HTTP401 e nenhum item criado; ausência de formulário autorizado |
| QA15 | Cliente tenta forjar ID/código/autor/datas/ativo | Rejeitar ou ignorar campos não aceitos conforme contrato técnico, mas nunca persistir identidade/autoria/datas escolhidas pelo cliente ou publicar inativo por esse payload |
| QA16 | Cliente tenta enviar regra/enum/executor | Publicação nova não ganha executor nem permissão de vincular regra. Aceitar ignorando ou recusar conforme contrato, sem inferência por nome/descrição/exemplo |
| QA17 | ID/código gerados e leitura posterior | Identidade e código únicos entre itens de teste e estáveis na leitura posterior/recarregamento; não exigir nome único ou formato de código não aprovado |
| QA18 | Persistência no modelo próprio | Registro no Marketplace/ErroCatalogo conforme schema entregue; não criar/reutilizar Categoria ou DitadoCategoria; campos retornados correspondem ao banco |
| QA19 | Envio em andamento e clique repetido | Estado de carregamento e ação desabilitada; sequência de duplo clique da UI não dispara publicações simultâneas. Não presumir idempotência global da API nem unicidade de nome |
| QA20 | Falha real de validação e correção | Erro visível sem recibo falso; preenchimento preservado; corrigir e repetir produz sucesso real |
| QA21 | Falha de rede e nova tentativa | Somente no contexto isolado QA, simular rede indisponível do navegador, sem mudar serviços ou respostas de negócio; formulário preservado, sem sucesso falso; restaurar rede e repetir contra API real. Identificar claramente esse cenário no relatório |
| QA22 | Registro sem executor e efeitos laterais | Confirmação indica ausência de regra automática; publicar não altera notas/respostas/seleções nem dispara endpoint/provedor LLM ou feedback |
| QA23 | Consultar minimamente/recarregar item criado | Item permanece legível como salvo; não exigir listagem ampla, busca ou filtro CAT01 |
| QA24 | Preservação dos dados protegidos | Snapshots antes/depois de ditados1/8, segmentos, respostas e tipos idênticos; nenhuma limpeza ou reclassificação de histórico |

Cada linha pode gerar subcasos no roteiro final; não declarar um total de testes executados com base nesta matriz. Código, rotas, DTOs e schema efetivos serão lidos após a entrega antes de fixar seletores ou contagens.

## Evidências e execução futura

Usar Playwright com Chromium visível, API real e frontend real, contextos isolados de professor, administrador e aluno. Não usar route/fulfill, mocks de sucesso ou troca temporária de API. O caso de rede indisponível é falha real de transporte somente no contexto QA, sem desligar a API ou afetar outras sessões; todas as publicações finais bem-sucedidas devem chegar à API real. Se isso não for viável sem atrapalhar usuário, registrar o subcaso como não executado, sem alegar aprovação.

Criar somente registros descartáveis separados, com identificação QA e sufixo de execução para rastreio. Essa marca não pressupõe unicidade de nome como regra do produto. Registrar IDs realmente criados e não excluir registros ao final sem instrução. Nenhuma gravação deve atingir as entidades humanas protegidas. Não presumir que retry após resposta perdida tenha exatamente uma gravação, pois isso exigiria contrato de idempotência não aprovado; o teste de retry será feito antes de envio chegar ao servidor, enquanto offline.

Conferir consulta por uma nova requisição e banco, incluindo código/autor/datas/ativo e executor ausente. Capturar telas vazias, validações, preenchimento, carregamento quando observável e confirmação em desktop e celular; coletar POST/GET/status e console/pageerror relevantes, sem tokens ou credenciais nos relatórios. Capturar valor real dos inputs separadamente do placeholder.

Revisar os testes entregues para identificar quais casos cobrem domínio/aplicação/controller e quais exigem navegador/SQL. Executar os testes pertinentes com comando e assembly da versão entregue, separando limitações de ambiente dos resultados. Não inventar comprimentos máximos, regras de séries, cardinalidade de exemplos ou nome/código manual; qualquer limite técnico novo deve ser identificado no contrato, não presumido a partir da maquete.

Pré-requisitos para execução: rotas/DTO final, representação persistida do par de exemplo, nomes das tabelas/migration aplicada pela equipe responsável, assembly ativo identificado e comando/assembly dos testes atuais. Confirmar se há teste manual em andamento antes de coordenação de reinício. CAT02/PER7 só recebe recomendação de conclusão após a publicação real e persistência verificadas; nenhum status é alterado por este planejamento.

## Adendo de expectativa visual — ajuste aprovado após validação inicial

Este adendo não altera resultados ou evidências anteriores do QA. Autorizado pelo usuário via Squad ditado 02: verificar apresentação em PublicarErro.vue com componentes Vuetify outlined/comfortable, cabeçalho/card/pa-6 e botões do padrão existente, labels/placeholders/helpers preservados. No recibo, ID numérico é “Número do registro”; código técnico ERR_... não aparece na UI, embora continue no POST/GET/banco para verificações de identidade. Texto exato esperado: “O cadastro foi salvo. Por enquanto, ele ainda não identifica erros nas respostas automaticamente.” Não esperar menção LLM/prazo.

Reteste focado: formulário inicialmente vazio e ajuda legível, obrigatórios e par opcional, publicação real/recibo sem código visível, valores preservados em falha, envio bloqueado em andamento e layout desktop/mobile. Não requer repetir toda a suíte backend sem alteração. Novos resultados são registrados pelo QA, não por este adendo do Fullstack.