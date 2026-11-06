# Especificação técnica — Módulos planejados para PG2

**Status:** decisões de produto consolidadas; detalhes de implementação ainda sujeitos a refinamento.
**Escopo:** backend .NET, frontend Vue e banco MySQL.

## 1. Contexto atual

O backend está organizado em `Ditado.API`, `Ditado.Aplicacao`, `Ditado.Dominio` e `Ditado.Infra`, com EF Core/Pomelo e MySQL. A correção atual é feita em `DitadoService.SubmeterRespostaAsync`: comparação tradicional, cálculo de percentual de acertos, classificação heurística e persistência de `RespostaDitado`/`RespostaSegmento`. `TipoErro` já existe no domínio. `ProfessorService` agrega resultados de turma e aluno.

O frontend Vue/Vite já possui telas de realização e resultados, painéis de professor e aluno, serviços HTTP e rotas. O painel do professor possui atalho de relatórios ainda em desenvolvimento. Não foi localizada integração LLM existente.

## 2. Decisões confirmadas

- O catálogo de erros é compartilhado entre professores e administradores; alunos não consultam o catálogo.
- Professores e administradores podem publicar erros. Professores podem editar apenas erros próprios que ainda não estejam associados a ditados; administradores podem editar qualquer erro ainda não utilizado. Depois de associado a ditados, o registro fica imutável: para substituir sua definição, o autor ou administrador arquiva o registro e publica outro.
- Erros arquivados podem ser consultados e restaurados por seus autores e por administradores; administradores podem fazê-lo para qualquer autor.
- Exclusão de erro já utilizado será lógica/arquivamento, preservando referências e histórico.
- O professor escolhe quais erros/regras do catálogo se aplicam a um ditado; a seleção vale para o ditado inteiro.
- A observação pedagógica é um campo simples da definição do erro, em texto livre, com série/ano opcional; não é uma entidade separada nem recebe associação a turma/ditado.
- Erro recorrente é contado separadamente por aluno e por turma, tanto para o mesmo erro cadastrado quanto para o mesmo tipo de erro. O escopo é o histórico completo; três ocorrências caracterizam recorrência.
- Feedback é gerado para o aluno e para o professor, sobre o desempenho do aluno/turma. A geração ocorre somente quando solicitada, para economizar tokens.
- O aluno solicita seu feedback por botão. Se o LLM falhar, a correção padrão continua disponível e a interface informa que o feedback não pôde ser gerado.
- O professor pode solicitar: análise geral da turma, análises individuais de todos os alunos da turma em uma solicitação, ou análise de um aluno específico.
- O envio de uma resposta é iniciado uma única vez por acionamento: a interface mostra estado de carregamento e impede novo acionamento até receber a resposta.
- Feedbacks de aluno e professor têm retenção global, configurável por variável de ambiente, com padrão de dois meses.
- Métrica de desempenho: percentual de acertos.
- PDF é gerado no navegador e deve refletir os filtros e gráficos atualmente exibidos.
- Provedor/modelo LLM será escolhido após comparação com exemplos rotulados de português brasileiro, priorizando custo e depois latência.
- O catálogo deve ter backup em disco, com estratégia alinhada a boas práticas operacionais.
- Professores e administradores podem importar definições em JSON; itens com código estável duplicado são rejeitados sem sobrescrever definições existentes, e o resultado identifica os itens rejeitados.

## 3. Épicos e componentes afetados

### 3.1 Catálogo e gerenciamento de erros

**Frontend:** nova área de catálogo e formulários de criação/edição; seleção dos erros aplicáveis no cadastro/edição do ditado; lista de arquivados e ação de restauração para autores (itens próprios) e administradores (qualquer autor). Bloquear edição de erros já associados a ditados e orientar a substituição por arquivamento e novo cadastro. Integrar rota e navegação de professor/administrador.

**API/aplicação:** endpoints de consulta e CRUD/arquivamento/restauração; autorização por papel e autoria; bloqueio de edição após associação do erro a qualquer ditado; endpoint/contrato para associar erros escolhidos ao ditado. A observação pedagógica e a série opcional são campos do CRUD do erro e seguem suas mesmas permissões.

**Domínio/MySQL:** entidade de erro com autor, nome (que identifica o tipo do erro), descrição/código estável, regra de identificação quando aplicável, observação pedagógica em texto livre, série/ano opcional, status ativo/arquivado e datas; associação N:N entre ditado e erros; configurações EF e migration. Não há categoria separada do nome do erro nem entidade separada para observação.

### 3.2 Correção por código e regras do catálogo

**Frontend:** telas de resultado devem exibir correção, zero ou mais nomes de erros identificados e recorrência. Vários tipos podem ser apresentados para a mesma resposta quando regras distintas se aplicarem. O feedback opcional do aluno deve ter acionamento explícito e estado de falha sem ocultar correção/nota tradicionais.

**API/aplicação:** comparar resposta e esperado e calcular acerto/nota por código. Primeiro, aplicar regras de código dos erros selecionados no ditado. Retornar todos os nomes de erro cujas regras distintas corresponderem à ocorrência; não forçar um tipo único nem escolher um tipo principal. Se nenhuma regra corresponder com confiança, registrar como não classificada/revisável. O fallback LLM fica fora da COR-01 e pode ser tratado em história posterior. O LLM também pode gerar feedback pedagógico ao aluno/professor somente quando solicitado; não substitui o texto esperado nem altera a nota objetiva.

**Domínio/MySQL:** persistir resposta, esperado, acerto, zero ou mais nomes/referências de erros identificados e método/estado da classificação. Uma resposta pode ter vários tipos de erro, sem categoria separada ou tipo principal. Sem correspondência confiável, persistir a ocorrência como não classificada/revisável. Preservar histórico e, para futuras identificações feitas pelo LLM, metadados mínimos de modelo, confiança/justificativa estruturada e versão das instruções quando necessário para auditoria.

### 3.3 Feedback personalizado do aluno

**Frontend:** botão para solicitar/mostrar feedback na tela de resultado do aluno; apresentar resumo de acertos/erros, tipo mais frequente, dica ortográfica e exercício de reforço. Exibir mensagem de falha do LLM mantendo resultado tradicional acessível.

**API/aplicação:** endpoint sob demanda autenticado, vinculado à tentativa do aluno; montar prompt com resultado, erros estruturados e regras/contexto aplicáveis. Evitar chamadas repetidas quando houver feedback válido salvo, salvo se houver decisão posterior de permitir regeneração.

**Domínio/MySQL:** persistir conteúdo gerado, data, tentativa/aluno e metadados mínimos do provedor/modelo; aplicar retenção configurada.

### 3.4 Feedback personalizado do professor

**Frontend:** ações para solicitar análise geral da turma, análise individual de todos os alunos ou análise de um aluno. Exibir erros mais frequentes, hipótese pedagógica e intervenção sugerida.

**API/aplicação:** endpoints por turma/sessão, lote de alunos e aluno específico, com autorização do professor para a turma. Montar análises apenas após solicitação explícita; reduzir dados enviados ao provedor ao mínimo necessário.

**Domínio/MySQL:** persistir análises com escopo (turma, lote/sessão ou aluno), referências, datas e metadados mínimos, com a mesma retenção global.

### 3.5 Relatórios e dashboard

**Frontend:** página de relatórios com filtros, gráficos e exportação PDF local no navegador. Métricas: evolução percentual por aluno/turma, distribuição por tipo e palavra e comparação entre turmas.

**API/aplicação:** endpoints agregados para séries e distribuições, respeitando o acesso do professor às turmas. Reutilizar consultas de resultados onde possível; considerar paginação/filtros por período.

**Domínio/MySQL:** usar respostas, alunos, turmas, ditados e erros classificados; avaliar índices conforme consultas finais. O PDF deve exportar o estado filtrado e os gráficos visíveis, sem depender de geração no servidor.

## 4. Dependências

1. O catálogo e a associação de erros ao ditado fornecem regras executáveis ao código e descrições/exemplos ao classificador LLM de fallback.
2. A classificação por código/LLM e o cálculo de recorrência fornecem dados estruturados para os dois feedbacks e para distribuição por tipo nos relatórios.
3. O feedback do professor reutiliza os mesmos agregados do dashboard, mas é geração LLM sob demanda.
4. O dashboard pode começar com dados tradicionais existentes, porém os gráficos por classificação e erro cadastrado dependem da persistência definida para a correção por código.
5. A política de retenção dos feedbacks não deve remover respostas/sessões usadas nos relatórios; deve ser aplicada às entidades de feedback gerado.

## 5. Modelo de dados conceitual inicial

Os nomes abaixo são conceituais. No produto, cada erro cadastrado tem um nome que identifica o tipo do erro; não haverá uma categoria adicional separada. As distinções encontradas na literatura servem como referência para nomear e descrever erros, sem exigir dimensões ou categorias independentes no catálogo. A lista inicial também deve considerar erros morfossintáticos, como concordância.

- **ErroCatalogo:** identificador, nome do erro (também seu tipo), descrição, regra executável opcional, exemplos/orientação, observação pedagógica em texto livre, série/ano opcional, autor, ativo/arquivado, criação/alteração.
- **DitadoErro:** associação do ditado aos erros selecionados.
- **RespostaSegmento/ClassificacaoErro:** tentativa, segmento, resposta e palavra esperada existentes; zero ou mais tipos identificados por código ou LLM, com cada nome de erro como tipo; método/confiança/estado classificado-não classificado e dados de recorrência derivados ou consultáveis. Uma ocorrência pode corresponder a vários tipos sem eleger um principal.
- **FeedbackGerado:** público/alvo, escopo (aluno/turma), referência da tentativa/sessão, conteúdo, modelo/provedor, data de geração e expiração.

Recorrência pode ser calculada a partir das respostas persistidas, evitando contadores duplicados sujeitos a inconsistência. Se o volume exigir, introduzir agregação materializada posteriormente.

## 6. Sequência sugerida de entrega

1. **Preparar a base e o plano documental:** fechar os nomes e descrições iniciais dos erros e suas regras de identificação quando aplicáveis; levantar exemplos rotulados; revisar arquitetura, modelo ER e dependências globais; registrar pendências sem presumir respostas.
2. **Detalhar o épico do catálogo antes de implementá-lo:** documentar permissões, arquivamento, seleção por ditado, anotações, JSON do Marketplace, regras executáveis, descrições para análise LLM e backup/restauração. Implementar catálogo compartilhado e suas regras de acesso.
3. **Detalhar o épico de classificação antes de implementá-lo:** diagramar comparação objetiva, classificação por regras de código e fallback LLM para casos sem regra aplicável, além de resposta não classificada/revisável. Implementar com acerto e nota sempre calculados pelo código, e persistir método/estado da classificação e recorrência.
4. **Detalhar e implementar feedback do aluno:** geração LLM somente sob solicitação, retenção configurável e falha do provedor sem perda da correção tradicional.
5. **Detalhar e implementar feedback do professor:** os três escopos sob solicitação (geral da turma, análise individual de cada aluno da turma, ou aluno específico), com autorização e tratamento de falhas/lotes.
6. **Detalhar e implementar relatórios e dashboard:** consultas agregadas, métricas e filtros; começar por estatísticas descritivas e acrescentar evolução/comparações conforme o histórico amadureça; exportar PDF no navegador com os filtros e gráficos visíveis.

Para cada etapa, concluir e revisar primeiro o pacote de diagramas e documentação definido na seção 10.5; atualizar os artefatos durante a implementação quando contratos ou decisões mudarem. Não iniciar a implementação de um épico cujo fluxo, dados e critérios de aceite ainda não estejam documentados ou cujas pendências bloqueadoras não tenham destino acordado.

## 7. Decisões técnicas ainda abertas

- Provedor/modelo LLM e estratégia de configuração de credenciais. Critérios: custo prioritário, latência secundária, resposta estruturada, disponibilidade e tratamento de dados de menores.
- Momento da classificação LLM de fallback: chamada síncrona durante a submissão ou processamento posterior. Definir também limiar de confiança, limites de custo/volume e comportamento quando a análise não for conclusiva.
- Correspondência entre os resultados do classificador técnico existente (`TipoErro`) e os nomes de erros cadastrados no catálogo, quando necessária para integrar os dados históricos. Essa correspondência não cria categorias adicionais; decisões sobre regras de identificação e uso de LLM devem ser tratadas separadamente.
- Semântica da ocorrência: contar respostas erradas por palavra submetidas em cada sessão; cada envio iniciado pelo aluno é uma ocorrência, sem deduplicação de reenvios na interface, que bloqueia novo acionamento enquanto aguarda resposta.
- Contrato e versionamento do JSON do Marketplace, incluindo validações e resposta de importação; papéis e rejeição de códigos duplicados estão definidos.
- Comportamento de regeneração de feedback já armazenado e limite de chamadas simultâneas/em lote.
- Backup do catálogo: frequência, retenção, localização segura, criptografia quando aplicável e procedimento de restauração. A cópia deve ser versionada e não ser o único backup do banco.
- Nome da variável de ambiente de retenção e política quando o valor estiver ausente/inválido; proposta de nome: `FEEDBACK_RETENTION_DAYS`, padrão `60`.
- Definição de “sessão” nos relatórios: cada `RespostaDitado` (tentativa) ou agrupamento de ditados realizados numa atividade.
- Comparativo entre turmas deve respeitar permissões; confirmar se há algum perfil com acesso transversal além de administrador.

## 8. Segurança e operação

- Segredos do provedor LLM devem vir de configuração segura do ambiente, nunca do frontend ou do repositório.
- Endpoints de feedback devem validar identidade, papel e vínculo com turma/tentativa antes de chamar o modelo.
- Enviar ao LLM somente informações necessárias; evitar nomes completos e outros identificadores pessoais sempre que possível.
- Respostas do LLM são conteúdo não confiável: validar formato e limitar tamanho antes de persistir/exibir.
- Falha ou timeout do provedor não deve desfazer a gravação da resposta nem impedir acesso à correção tradicional.
- Arquivamento mantém integridade das associações históricas; erros arquivados não devem ser selecionáveis em novos ditados.
- Backups precisam de teste de restauração e controle de acesso; arquivos de backup contêm dados do catálogo e devem ter política operacional explícita.

## 9. Critérios de aceite transversais

- Todas as telas novas devem funcionar em navegadores de computador e celular, com layout responsivo e controles utilizáveis nos dois formatos.
- Acerto e nota são calculados por código e não dependem de disponibilidade de LLM; se o LLM de fallback estiver indisponível, a resposta continua corrigida e fica sem tipo específico até eventual revisão.
- Professor só altera/exclui erros próprios; administrador pode administrar todos; arquivamento preserva referências antigas.
- Recorrência é identificável por aluno e turma, para tipo e erro cadastrado, a partir de três ocorrências no histórico definido.
- Nenhum feedback é gerado sem solicitação explícita do aluno ou professor.
- Feedback de professor respeita o escopo escolhido e a autorização de acesso à turma.
- Feedback expirado deixa de ser servido conforme retenção configurada, sem remover resultados pedagógicos de origem.
- Relatórios exibem percentual de acertos e o PDF representa os filtros/gráficos visíveis no momento da exportação.

## 10. Complemento de pesquisa e planejamento

### 10.1 Taxonomia com foco no português brasileiro

Estudos de aquisição da escrita em crianças brasileiras usam termos como erros fonológicos, ortográficos e fonográficos, e analisam operações como transposições. Esses conceitos servem como referência acadêmica para definir nomes, descrições e exemplos dos erros no catálogo; não serão campos/categorias separados no produto. Incluir erros como concordância quando fizer sentido. “Escrita fonética” deve receber definição e exemplos próprios; não presumir que variação dialetal seja erro.

Referências iniciais: [estudo sobre erros gráficos nos anos iniciais (SciELO)](https://www.scielo.br/j/edur/a/Z3trLgDyBBZWB466FLHFXtz/?lang=pt), [estudo sobre transposições na escrita infantil](https://revistas.gel.org.br/estudos-linguisticos/article/view/3307) e [avaliação ortográfica de escolares do 2º ao 5º ano](https://www.scielo.br/j/jsbf/a/Y68KLnGKkVvCBmPvckWnwbj/?lang=pt).

### 10.2 Comparação preliminar de provedores

Preços públicos consultados em 2026-11-04. Estimativa comparável: 1.000 chamadas com 1.000 tokens de entrada e 200 de saída por chamada (1 milhão de tokens de entrada e 0,2 milhão de saída). Valores USD, sem impostos, retries ou tokens adicionais de raciocínio.

| Provedor/modelo | Entrada / saída por 1M tokens | Estimativa / 1.000 chamadas | Observações |
|---|---:|---:|---|
| Alibaba Qwen 3.7 Flash (global, até 32K tokens) | $0.028 / $0.110 | ~$0.05 | Menor preço estimado; conferir região e cota contratada. [Tabela oficial](https://www.alibabacloud.com/help/en/model-studio/model-pricing) |
| DeepSeek Flash | $0.15 / $0.60 fora do pico; $0.30 / $1.20 no pico | ~$0.27 fora do pico; ~$0.54 no pico | Tem JSON output e modo sem raciocínio; tarifas variam por janela horária. [Tabela oficial](https://api-docs.deepseek.com/quick_start/pricing/) |
| OpenAI GPT-6 Luna | $0.10 / $0.50 | ~$0.20 | Modelo eficiente para alto volume e structured outputs. [Modelo oficial](https://developers.openai.com/api/docs/models/gpt-6-luna) |
| Anthropic Claude Haiku 5.5 | $0.10 / $0.50 | ~$0.20 | Posicionado pelo fornecedor para classificação rápida de alto volume. [Modelo oficial](https://platform.claude.com/docs/en/models/haiku-5-5/overview) |
| Moonshot Kimi K3.1 Pro | $2.00 / $10.00 | ~$4.00 | Suporta JSON Schema estrito, mas é flagship, sempre usa modo de raciocínio e custa bem mais para esse caso. [Modelo e preço](https://platform.kimi.ai/docs/guide/kimi-k3-1-pro-quickstart), [preços](https://platform.kimi.ai/docs/pricing/chat) |

Kimi K2.5 não deve ser usado como candidato de implementação: a documentação oficial informa descontinuação em agosto de 2026 e fim da série K2 até o fim de dezembro de 2026; o modelo atual da série K3 é K3.1 Pro. O recurso K3.1 Pro Highspeed prioriza velocidade com tarifa maior. [Lista oficial de modelos](https://platform.kimi.ai/docs/models), [início rápido K3.1 Pro](https://platform.kimi.ai/docs/guide/kimi-k3-1-pro-quickstart).

Não foi localizada avaliação independente direta comparando esses modelos na classificação pedagógica de erros de escrita infantil em português brasileiro. Portanto, os preços e descrições dos fornecedores servem para selecionar candidatos, mas não para declarar vencedor. A etapa de seleção deve medir classificação, explicação apropriada à série, latência e custo no mesmo conjunto local de exemplos rotulados.

### 10.3 Escopo inicial do dashboard

Mesmo com poucos dados, a primeira versão pode apresentar estatísticas descritivas: percentual de acertos, participação/conclusão, contagem e proporção de erros, distribuição por aluno/turma/palavra/regra, e recorrência após três ocorrências. Mostrar sempre o número de respostas/oportunidades junto à proporção. Comparações de palavras devem usar denominadores (erros por oportunidades), não apenas totais.

Evolução temporal, comparação entre turmas e efeitos de intervenções ficam progressivamente mais informativos com histórico. Quando a amostra for pequena, indicar o volume e evitar apresentar tendências ou hipóteses como conclusões. O primeiro release não depende de uma quantidade mínima de dados para mostrar resultados individuais e contagens.

### 10.4 Plano validado

1. Documentar a arquitetura e o modelo de dados PG2; fechar o vocabulário em português brasileiro e preparar exemplos rotulados por série.
2. Documentar e depois implementar o catálogo compartilhado, permissões, arquivamento, seleção por ditado, anotações ligadas aos erros, regras executáveis e importação JSON do Marketplace; incluir a estratégia de backup/restauração.
3. No conjunto rotulado, comparar candidatos de LLM e registrar provedor/configuração escolhidos. O LLM é fallback de classificação quando nenhuma regra de código aplicável identificar o tipo; pode analisar descrições de erro do Marketplace sem regra executável.
4. Documentar e implementar a classificação: o código mantém comparação objetiva e cálculo da nota; persistir método/estado e deixar sem classificação específica quando não houver resultado confiável. Resolver ou aceitar explicitamente antes do desenvolvimento se o fallback ocorrer de forma síncrona ou assíncrona.
5. Documentar e implementar feedback LLM do aluno e do professor somente sob demanda, com retenção global configurável de 60 dias por padrão.
6. Documentar e entregar dashboard descritivo primeiro; acrescentar tendências/comparações conforme o histórico amadureça e exportar PDF no navegador conforme filtros e gráficos. Cada épico deve ter seus diagramas e documentação revisados antes do início da implementação, conforme a seção 10.5.

### 10.5 Preparação de diagramas e documentação por épico

Antes de iniciar a implementação de cada épico, reservar uma etapa curta de detalhamento e revisão dos artefatos. A documentação deve refletir as decisões confirmadas neste arquivo; decisões técnicas ainda abertas devem aparecer como pendências, sem serem tratadas como requisitos já decididos. Atualizar os artefatos junto com mudanças relevantes na implementação.

**Artefatos compartilhados do PG2**

- Diagrama de contexto/arquitetura mostrando Vue, API .NET, camadas da aplicação, MySQL e provedor LLM, incluindo os limites de confiança e os fluxos de dados.
- Diagrama ER atualizado com entidades atuais e propostas, cardinalidades, arquivamento, retenção de feedback e relações entre ditado, erros selecionados, respostas/classificações e feedbacks; observação pedagógica e série/ano são campos do erro, sem entidade separada.
- Mapa de dependências e sequência dos épicos, destacando quais dados e contratos cada entrega fornece à seguinte.
- Registro de decisões técnicas (ADR ou seção equivalente nesta especificação) para decisões que afetem arquitetura, privacidade, operação ou contratos.

**Pacote mínimo para cada épico**

1. Objetivo, escopo incluído e excluído, atores/permissões e critérios de aceite.
2. Fluxo principal e fluxos alternativos/erro em diagrama de sequência ou atividade, cobrindo front, API, domínio/banco e integrações quando aplicável.
3. Diagrama de domínio/dados da parte afetada, com campos, relações e regras de integridade; indicar migrações necessárias.
4. Contratos de API e eventos/operações relevantes, com exemplos de requisição/resposta e códigos de erro, se aplicável.
5. Esboço das telas/estados de interface relevantes, incluindo carregamento, vazio, sucesso, falha e autorização negada; especificar e conferir o comportamento responsivo em navegador de computador e celular.
6. Notas de segurança, privacidade, observabilidade, retenção/backup e compatibilidade com dados existentes que se apliquem ao épico.
7. Estratégia de implementação incremental, dependências, riscos e pendências que possam bloquear o início.

**Foco dos diagramas por épico**

| Épico | Diagramas e documentação específicos |
|---|---|
| Catálogo e gerenciamento de erros | Fluxo de publicação/edição/arquivamento por papel e autoria; modelo ER do catálogo com observação e série/ano no próprio erro, mais associação ao ditado; formato JSON e validações para importação em lote/Marketplace; fluxo de backup e restauração. |
| Correção por código e fallback LLM | Sequência da submissão com botão bloqueado durante carregamento, comparação objetiva, regras de código, fallback LLM para casos não resolvidos, resposta não classificada, persistência do método e recorrência por aluno/turma. |
| Feedback personalizado do aluno | Fluxo sob demanda do botão até resposta armazenada/exibida, autorização, reutilização/regeneração e falha; contrato do endpoint e ciclo de retenção. |
| Feedback personalizado do professor | Diagramas dos três escopos (geral da turma, análises individuais de toda a turma, aluno específico), autorização por turma, processamento em lote e resultados parciais/falhas; contrato de persistência e retenção. |
| Relatórios e dashboard | Fluxo de filtros para consultas agregadas e exportação PDF no navegador; definição de denominadores e métricas; mapa entre filtros, endpoints, gráficos e dados de origem. |

**Pronto para implementação quando:** o pacote mínimo do épico estiver revisado, as dependências estiverem identificadas, os contratos necessários às equipes estiverem estáveis e cada pendência estiver resolvida ou explicitamente aceita como risco. Guardar os arquivos em `docs/diagramas/` (preferir Mermaid versionado junto ao Markdown) e a documentação detalhada em `docs/epicos/`, com links cruzados para esta especificação. Não duplicar decisões: esta especificação continua sendo a fonte das decisões globais; páginas de épico detalham apenas o escopo e as decisões locais.

### 10.6 Erros da correção por código e objeto `tipoErroDitado`

Esta tabela define a taxonomia de tipos de erro. O nome do erro é o próprio tipo; os exemplos ilustram ocorrências, mas não autorizam inferir a causa do erro. A correção por código implementa somente regras observáveis e delimitadas, cobertas por testes. Uma resposta pode receber mais de um nome quando regras independentes se aplicarem; não há tipo principal. Quando não houver regra segura, a ocorrência fica sem classificação; isso é um estado, não um tipo de erro. Os termos e exemplos registram a síntese das pesquisas sobre escrita escolar em português e não constituem diagnóstico do aluno.

**Contrato executável desta entrega:** o classificador recebe somente o texto da lacuna esperado e digitado; não analisa a frase inteira. A comparação objetiva ignora apenas espaços externos e diferencia maiúsculas de minúsculas. Regras delimitadas devem ser testadas com exemplos positivos e negativos próximos; regras independentes retornam todos os tipos que se aplicarem. Uma regra não coberta deixa a resposta sem classificação. Nesta entrega, não se infere transcrição da fala por troca de vogais nem hipercorreção por alteração `-il`→`-io`: esses dois tipos exigem interpretação da motivação e ficam adiados para eventual cadastro do professor e tratamento futuro por LLM.

O enum que agrupava omissão/acréscimo/substituição e outros IDs legados permanecem disponíveis para leitura de registros históricos; novas respostas recebem os tipos operacionais separados e não há reclassificação em massa.

| Tipo | Regra delimitada implementada |
|---|---|
| Transcrição da fala / escrita fonética | Adiada na correção automática desta entrega. Não inferir esse tipo por uma troca de vogais; sem regra observável confirmada, manter apenas o erro objetivo identificável ou não classificar. |
| Troca de grafemas / surda-sonora | Substituição única entre `p/b`, `t/d` ou `f/v`; `s/z` no início da palavra; `c/g` antes de `a/o/u`, mantendo o restante igual. |
| Regra contextual | Inserção ou supressão de um dos dois `r` de `rr` entre vogais, como `carro/caro`, inclusive dentro de uma palavra maior. |
| Grafia arbitrária / irregular | Troca `s/z` no fim ou entre vogais; troca `c/s` antes de `e/i`; inserção ou supressão de `h` inicial. |
| Nasalização | Inserção, supressão ou troca de `m/n` em coda silábica (antes de consoante ou no fim da palavra), `nh` junto a vogal, ou diferença `a/ã` e `o/õ`. |
| Omissão | A resposta omite uma letra esperada; uma resposta vazia também é omissão. |
| Acréscimo | A resposta acrescenta uma letra que não existe no esperado. |
| Substituição | A resposta troca uma letra esperada por outra. Pode coexistir com um tipo específico quando outra regra independente se aplicar. |
| Complexidade silábica | Supressão ou acréscimo de uma consoante em encontro `br`, `cr`, `dr`, `fr`, `gr`, `pr`, `tr`, `bl`, `cl`, `fl`, `gl` ou `pl`, antes de vogal. |
| Hipercorreção / supergeneralização | Adiada na correção automática desta entrega. Não inferir intenção de hipercorrigir a partir de `-il`→`-io`; poderá ser cadastrada pelo professor para tratamento futuro. |
| Acentuação | Diferença explicada somente por acento gráfico; pode coexistir com nasalização quando o par também muda uma vogal nasal. |
| Juntura / segmentação | A sequência de letras é igual e a diferença está na separação em espaços entre palavras. |
| Concordância/flexão | Pares de forma verbal com radical idêntico e terminações `-aram/-ou`, `-eram/-eu` ou `-iram/-iu`. Sem esse padrão no par isolado, não classificar. |
| Maiúsculas e pontuação | Diferença explicada somente por caixa e/ou pontuação. A resposta continua errada para a nota quando caixa difere. |

| Nome do erro (tipo) | Descrição | Exemplo em frase (esperado → resposta) |
|---|---|---|
| Transcrição da fala / escrita fonética | Grafia influenciada pela pronúncia percebida. | “A bolacha quebrou.” → “A bulacha quebrou.” |
| Troca de grafemas / surda-sonora | Substituição de letra que pode refletir confusão entre sons ou entre grafias possíveis. | “A faca caiu.” → “A vaca caiu.” |
| Regra contextual | Escolha depende da posição ou das letras vizinhas, como `r/rr` e nasalização contextual. | “O carro parou.” → “O caro parou.” |
| Grafia arbitrária / irregular | A pronúncia não determina sozinha a grafia convencional, como alternativas para o som /s/ ou uso de `h`. | “A casa é azul.” → “A caza é azul.” |
| Nasalização | Omissão, acréscimo ou troca de marcas de nasalidade (`m`, `n`, `nh`, vogal nasal). | “A nuvem passou.” → “A nuve passou.” |
| Omissão | Falta uma letra ou segmento esperado. | “O gato dormiu.” → “O gto dormiu.” |
| Acréscimo | Sobra uma letra ou segmento na resposta. | “O gato dormiu.” → “O gatto dormiu.” |
| Substituição | Uma letra esperada foi trocada por outra. | “O gato dormiu.” → “O cato dormiu.” |
| Complexidade silábica | Erro relacionado a encontros consonantais ou estruturas silábicas complexas. | “O prato está limpo.” → “O pato está limpo.” |
| Hipercorreção / supergeneralização | Aplicação excessiva de uma regra ou alteração feita na tentativa de escrever de forma mais correta. | “O tecido é frágil.” → “O tecido é frágio.” |
| Acentuação | Acento obrigatório omitido, acrescentado ou colocado incorretamente. | “Ele tomou café.” → “Ele tomou cafe.” |
| Juntura / segmentação | União ou separação indevida de palavras, possivelmente influenciada pelo fluxo da fala. | “Ele foi embora.” → “Ele foi em bora.” |
| Concordância/flexão | Incompatibilidade de gênero, número, pessoa ou tempo verbal. | “As meninas chegaram.” → “As meninas chegou.” |
| Maiúsculas e pontuação | Uso diferente do esperado de maiúsculas, ponto, vírgula ou outros sinais. | “Maria chegou cedo.” → “maria chegou cedo” (ou sem ponto final). |

**Modelo conceitual do tipo de erro:**

```json
{
  "schemaVersion": 1,
  "codigo": "JUNTURA_SEGMENTACAO",
  "nome": "Juntura / segmentação",
  "descricao": "A separação em espaços da palavra digitada difere do esperado.",
  "exemploEmFrase": {
    "fraseEsperada": "Ele foi embora.",
    "respostaDoAluno": "Ele foi em bora."
  },
  "ativo": true,
  "versao": 1
}
```

O objeto conceitual guarda o nome do erro como seu tipo; não há categoria separada nem campos de dica de professor ou observação de IA. Nesta entrega, todos os doze tipos da tabela são avaliados por regras de código. Uma regra em linguagem natural sem correspondência executável não classifica a resposta; ela permanece sem classificação para revisão. Não há fallback de LLM.

O comparador objetivo e a nota pertencem ao código. Neste fluxo, a classificação não recebe contexto frasal: concordância/flexão só é identificada quando o próprio par de palavras corresponde a uma regra delimitada acima; sem correspondência, fica sem classificação. A classificação não altera a decisão objetiva nem substitui a resposta esperada.

**Fontes consultadas na pesquisa complementar:** [Miranda, estudo sobre erros (orto)gráficos nos anos iniciais](https://www.scielo.br/j/edur/a/Z3trLgDyBBZWB466FLHFXtz/?lang=pt); [estudo sobre regras contextuais e morfossintáticas](https://www.scielo.br/j/ptp/a/vtGPJM5CVrBPLzfvGKMJLGw/?lang=pt); [estudo sobre fonemas surdos/sonoros, juntura e segmentação em ditados](https://www.scielo.br/j/acr/a/MxLcXCHDQcPYkrjCrfJ5ryL/); [estudo sobre arbitrariedade e regras contextuais no português brasileiro](https://periodicos.ufsm.br/LeC/article/view/28546); [brochura Aprender Ortografia, Direção-Geral da Educação](https://www.dge.mec.pt/sites/default/files/EInfancia/documentos/brochura_aprender_a_escrever.pdf). A síntese dos três PDFs enviados pelo usuário inclui termos usados por Carraher (1985), Gandra e no enquadramento GEALE discutido por Saggiomo (2018); essa diversidade da literatura não implica categorias separadas no produto.
