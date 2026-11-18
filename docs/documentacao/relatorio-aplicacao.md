# Ditado — Relatório vivo da aplicação

- **Versão** V0.27
- **Data** 18/11/2026
- **Status** Revisão técnica para incorporação ao documento acadêmico
- **Base analisada** Backend `424ed94` e frontend `14905a7`

> Para atualizar o PDF do MVP, use a seção 5 para os atores (item 2.3), a seção 8 para requisitos e regras de negócio (itens 2.4 a 2.6) e a seção 11 para os casos de uso (item 3.2). A seção 8.4 rastreia os identificadores legados. A referência é o código dos checkouts analisados, com evidências de validações anteriores registradas nas seções correspondentes.

## 1. Como ler este documento

As afirmações recebem uma destas classificações:

- **Implementado:** possui código no checkout analisado. A indicação **API** identifica operações cuja cobertura pela interface web não foi confirmada.
- **Validado:** há evidência de execução, com ambiente e data identificados.
- **Decisão confirmada:** regra acordada no projeto, cuja implementação e validação são registradas quando comprovadas.
- **Planejado:** descrito em especificação, história ou Linear, mas não comprovado como implementação atual.
- **Hipótese/recomendação:** alternativa técnica ou interpretação que ainda depende de decisão.
- **Legado:** informação preservada por compatibilidade ou documentação histórica.
- **Desconhecido:** lacuna que impede uma afirmação confiável.

O relatório deve ser atualizado quando uma mudança alterar qualquer seção, requisito, fluxo, contrato, dado, decisão ou diagrama aqui registrado.

## 2. Identificação do trabalho

- **Sistema:** Ditado.
- **Tipo:** aplicação web para gerenciamento e realização de atividades de ditado escolar.
- **Escopo técnico:** backend .NET, frontend Vue e banco MySQL.
- **Repositório documentado nesta versão:** backend em `Ditado - backend (1)`.
- **Frontend relacionado:** repositório `ditado-main`.
- **Instituição identificada na capa do MVP:** Universidade Federal do Estado do Rio de Janeiro (UNIRIO), Centro de Ciências Exatas e Tecnologia, Escola de Informática Aplicada.
- **Curso, integrantes atuais, orientação e disciplina:** **[PENDENTE DE CONFIRMAÇÃO]**.
- **Norma acadêmica e prazo de entrega:** **[PENDENTE DE CONFIRMAÇÃO]**.

## 3. Visão geral e propósito

O Ditado é uma aplicação web para criar e aplicar atividades escolares de escrita. O professor prepara um texto com lacunas, grava o áudio, seleciona definições do Catálogo de erros e atribui o ditado a turmas autorizadas. Após o envio, o aluno recebe nota e correção por lacuna. O professor consulta o resultado das aplicações e as ocorrências de erro da turma. A plataforma usa a escola como unidade de autorização nos fluxos com controle de escopo e mantém compatibilidade com registros do MVP.

## 4. Problema, justificativa e objetivos

### 4.1 Problema

O sistema busca reduzir o trabalho manual de criação, aplicação, correção e acompanhamento de ditados escolares, mantendo o registro das respostas e dos tipos de erro observados.

**Status:** confirmado em nível geral; a formulação acadêmica detalhada do problema ainda precisa ser validada pelos autores do trabalho.

### 4.2 Justificativa

Uma aplicação centralizada pode apoiar a organização de turmas e ditados, preservar o histórico das tentativas e fornecer informações para acompanhamento pedagógico.

**Status:** recomendação de redação; dados quantitativos, estudos de campo e resultados com usuários ainda não foram fornecidos.

### 4.3 Objetivo geral

Documentar e evoluir uma aplicação web capaz de gerenciar atividades de ditado, registrar respostas, calcular a correção objetiva e apoiar a análise do desempenho de alunos e turmas.

**Status:** formulação provisória para validação.

### 4.4 Objetivos específicos

- autenticar usuários e controlar permissões;
- organizar alunos e turmas;
- criar, visualizar, disponibilizar e realizar ditados;
- corrigir respostas e calcular nota;
- registrar operações observáveis e tipos de erro quando houver regra segura;
- estruturar um catálogo de erros;
- apoiar feedbacks e relatórios futuros;
- preservar histórico, rastreabilidade e integridade dos dados.

## 5. Usuários, atores e permissões

| Ator | Responsabilidades e acesso |
|---|---|
| Visitante | Solicitar cadastro como professor ou acesso como aluno, autenticar-se e iniciar a configuração do PIN de aluno. |
| Aluno | Consultar suas turmas e seus ditados atribuídos, ouvir o áudio, responder às lacunas e consultar resultados e tentativas próprias. |
| Professor | Gerenciar suas turmas e alunos, preparar ditados, usar o catálogo, atribuir atividades autorizadas e consultar resultados no escopo das turmas. |
| Administrador da Escola | Exercer as operações que sua função permite no escopo da escola. O vínculo escolar e o tipo de usuário são campos distintos. O professor que cria uma nova escola recebe um vínculo administrativo nela. |
| Administrador Geral/Técnico | Usar as operações globais que exigem a permissão explícita `AdministradorGeral`, como confirmação da limpeza de áudio via API. |

O servidor considera, conforme a operação, tipo de usuário, vínculo ativo, autoria, responsabilidade pela turma e visibilidade do recurso. Algumas consultas e atribuições ainda têm caminhos de compatibilidade com o legado. O contrato de multi-tenant descreve as regras normativas e as transições em andamento. [Referencial do projeto](../REFERENCIAL_PROJETO.md) · [MT-04](../epicos/multi-tenant-mt04.md)

## 6. Funcionalidades atuais evidenciadas

O fluxo principal cobre cadastro e entrada por perfil, turmas e chamada, elaboração e atribuição de ditados, submissão com correção, histórico de tentativas e consulta de resultados. O Catálogo de erros oferece consulta, publicação, edição autorizada e arquivamento ou restauração. Na COR-01, a seleção do catálogo passa a determinar quais regras programadas podem classificar uma resposta.

As rotas de contexto escolar, revogação de sessão, visibilidade e arquivamento de ditados, exportação CSV e retenção de áudio expõem operações do backend. As diferenças de cobertura entre API e interface estão registradas na seção 11.2. O catálogo usa `Marketplace*` nos nomes técnicos legados. [Ciclo COR-01](../relatorio-ciclo-cor01-2026-11-17.md) · [Ciclo PER-59](../relatorio-ciclo-per59-2026-11-16.md)

## 7. Escopo incluído e excluído

O recorte atual contempla a aplicação web operando com banco compartilhado e escopo escolar. A importação de lista de chamada auxilia o cadastro de alunos. Seu refinamento, incluindo extração de matrícula, exclusão de linhas inválidas e mapeamento de colunas, está na [PER-43](https://linear.app/joaomoutinho/issue/PER-43/importacao-de-chamada-melhorar-extracao-de-matricula-e-edicao-de).

O planejamento PG2 contempla classificação híbrida com LLM, feedback personalizado e evolução de relatórios. A importação JSON do catálogo tem tela, mas a rota correspondente não está presente na API analisada. OCR, recomendações automáticas de exercícios, presença por QR, aplicativo móvel e comparativos institucionais permanecem fora do escopo funcional comprovado neste recorte. [Especificação PG2](../../Ditado/docs/ESPECIFICACAO_PG2.md) · [Contrato COR-01 e PER-18](../epicos/cor-01-classificacao-hibrida.md)

## 8. Requisitos e regras de negócio

### 8.1 Requisitos funcionais

| ID | Requisito verificável | Caso de uso |
|---|---|---|
| RF01 | O sistema deve exibir ao professor autorizado a conclusão e as notas das aplicações de suas turmas, com agregações de erros por palavra e por tipo quando disponíveis. | CU12 |
| RF02 | O aluno deve poder abrir um ditado disponibilizado para sua turma, reproduzir o áudio de leitura, preencher as lacunas e enviar as respostas. | CU10 |
| RF03 | O professor deve poder criar um ditado com título, texto com lacunas marcadas por colchetes e gravação de áudio de leitura. | CU06 |
| RF04 | O professor autorizado deve poder consultar o resumo das aplicações e os resultados por aluno em cada turma. | CU12 |
| RF05 | Após a submissão, o aluno deve ver nota, totais de acertos e erros, resposta fornecida, resposta esperada e classificações disponíveis por lacuna. | CU10 |
| RF09 | O professor responsável deve poder cadastrar alunos na turma informando nome e número da chamada, com matrícula opcional. | CU04 |
| RF13 | O pessoal autorizado deve poder consultar os ditados disponíveis, visualizar áudio e gabarito e editar título, descrição e categorias. A edição da seleção segue a RN09. | CU06 |
| RF17 | O professor ou administrador autorizado deve poder gerenciar categorias e associá-las ao ditado. | CU06 |
| RF19 | A correção deve aplicar às respostas incorretas as regras programadas correspondentes às definições selecionadas e registrar todas as classificações aplicáveis. | CU10 |
| RF24 | O sistema deve autenticar professor ou administrador com login e senha e aluno com matrícula ou código e PIN configurado. Deve recusar credenciais inválidas ou conta inativa. | CU01 |
| RF25 | O sistema deve limitar a administração de usuários e a concessão da permissão global conforme o tipo de conta, os vínculos ativos e os privilégios do operador. A alteração da permissão global está exposta na API. | CU02 |
| RF26 | O usuário deve poder sair da interface. A API deve permitir revogar a sessão atual e rejeitar o token revogado nas operações autenticadas. | CU03 |
| RF27 | O pessoal autorizado deve poder atribuir um ditado à turma com data-limite, atualizar essa data e remover a atribuição. | CU09 |
| RF30 | O sistema deve calcular e persistir o resultado de cada submissão bem-sucedida, com data, nota e detalhes das respostas, e manter tentativas individuais. | CU10, CU11 |
| RF31 | O visitante deve poder solicitar cadastro como professor criando uma escola ou informando uma escola existente. O primeiro caminho cria vínculo administrativo ativo. O segundo grava conta e vínculo inativos. A conclusão da aprovação nesse segundo caminho depende da limitação descrita no CU02. | CU02 |
| RF32 | O pessoal autorizado deve poder pré-visualizar listas de chamada em CSV, XLS e XLSX, revisar linhas e confirmar o cadastro dos alunos. O cadastro é enviado por aluno e pode concluir parcialmente. | CU04 |
| RF33 | O professor ou administrador autorizado deve poder criar e atualizar turmas da escola, definir o responsável e associar ou remover alunos. O aluno deve poder consultar suas turmas. | CU05 |
| RF34 | O pessoal autorizado deve poder listar definições ativas e arquivadas do catálogo, abrir o detalhe de uma definição ativa, publicar, editar dentro das regras de uso, arquivar e restaurar. | CU07 |
| RF35 | A API deve permitir configurar a visibilidade de ditados e definições como Público geral, Instituição ou Turmas e verificar a publicação vigente para operações que exigem escopo. | CU07, CU08, CU09 |
| RF36 | Ao criar um ditado, o professor deve selecionar pelo menos uma definição ativa e acessível. Na atribuição, a API deve validar a seleção para a escola e a turma de destino. | CU06, CU09 |
| RF37 | A API deve permitir arquivar ou restaurar ditados mediante autorização. Ditado arquivado fica inativo no fluxo atual. | CU08 |
| RF38 | O aluno deve poder consultar ditados atribuídos, prazo, indicação de atraso, quantidade de tentativas e melhor nota. A API deve disponibilizar as tentativas próprias. | CU11 |
| RF39 | A API deve permitir exportar registros de tentativas vinculadas a aplicações em CSV, por escola autorizada, com cursor e sem inclusão de áudio. | CU13 |
| RF40 | A API deve permitir ao Administrador Geral listar candidatos à limpeza de áudio e confirmar a remoção com nova verificação de referências. | CU14 |
| RF41 | No primeiro acesso, o aluno deve poder verificar a matrícula ou o código e configurar seu PIN uma única vez. | CU01 |
| RF42 | O usuário autenticado deve poder editar seu próprio nome. A API também aceita alterações da matrícula e da credencial nos campos previstos pelo contrato. | CU02 |

### 8.2 Requisitos não funcionais

| ID | Atributo | Requisito e evidência |
|---|---|---|
| RNF03 | Persistência e integridade | O sistema deve persistir os dados em banco relacional com chaves, associações e migrações de schema versionadas. Implementado com MySQL 8 e EF Core 9. O bootstrap em banco vazio teve ressalvas na COR-01 e no PER-59. |
| RNF04 | Usabilidade em tamanhos de tela | A interface web deve acomodar diferentes dimensões de tela. Vue e Vuetify oferecem componentes e layouts responsivos. A validação sistemática em celulares e tablets permanece pendente. |
| RNF05 | Compatibilidade web | Os fluxos web, inclusive gravação com permissão de microfone e reprodução de áudio, devem funcionar nos navegadores homologados. Há evidência pontual de QA em navegador. A matriz de navegadores, dispositivos e versões ainda precisa de homologação. |
| RNF07 | Transporte e autenticação | A aplicação deve proteger o tráfego autenticado com HTTPS e JWT Bearer. A API configura redirecionamento HTTPS, valida assinatura e expiração do token e consulta a sessão no servidor. A configuração TLS e a cobertura HTTPS da implantação institucional permanecem a verificar. |
| RNF09 | Proteção de credenciais | O backend deve persistir apenas o derivado de senhas e PINs configurados. O código usa PBKDF2 com SHA-256, salt aleatório por credencial e comparação em tempo constante. |
| RNF10 | Autorização e isolamento | O backend deve restringir operações por identidade, função, vínculo e escopo do recurso. As validações de isolamento registradas cobrem cenários específicos do PER-37, PER-59 e COR-01. A cobertura integral das rotas legadas permanece a verificar. |
| RNF11 | Rastreabilidade | O sistema deve registrar eventos nas operações auditadas, incluindo decisões de contexto escolar, revogação, mudança da permissão global, exportação e limpeza de áudio. A trilha registrada é parcial. |
| RNF12 | Limites de processamento | A prévia de chamada deve aceitar até 10 MB e 1.000 linhas. A exportação deve aplicar limites de linhas, bytes e tempo da geração, permitir paginação por cursor e tratar cancelamento. Os valores padrão são 1.000 linhas, 5.000.000 bytes e 3.000 ms. |
| RNF13 | Recuperabilidade | A estratégia operacional prevê cópia integral do banco, rotação após verificação e ensaio de restauração. Há evidência de restauração isolada no PER-37. Frequência, retenção, destino e objetivos de recuperação em produção aguardam definição e validação institucional. |

O MVP registrava cadastro em menos de 10 s (RNF02), disponibilidade de 98% (RNF06) e correção em até 5 s (RNF08). Essas metas históricas carecem de medição e ratificação para a versão atual. A nota e a correção são retornadas na submissão, sem meta de latência aferida neste levantamento. A janela de 24 horas do CU14 é uma regra de elegibilidade para limpeza de áudio.

### 8.3 Regras de negócio

| ID | Regra e alcance |
|---|---|
| RN01 | O aluno encontra atividades pela atribuição à turma. A data-limite sinaliza atraso no painel. O bloqueio por vencimento não aparece no serviço atual de submissão. |
| RN02 | O contrato de realização omite o gabarito nas lacunas. O retorno da submissão apresenta a correção, e a visualização completa é reservada ao pessoal autorizado. |
| RN03 | Uma turma recebe uma aplicação do mesmo ditado. Cada tentativa concluída fica registrada individualmente. O relatório principal usa a maior nota por aluno e, no empate, a tentativa mais antiga. |
| RN04 | A nota é a proporção de acertos nas lacunas contabilizadas, em escala de 0 a 100. O comparador ignora espaços externos e considera caixa e acentuação. Uma resposta incorreta pode ter zero, uma ou várias classificações sem alterar a lógica da nota. |
| RN05 | O cadastro de professor utiliza nome, e-mail, senha e opção de escola. |
| RN06 | No cadastro manual, aluno tem nome, turma, número de chamada e matrícula opcional. O sistema gera o código de acesso. |
| RN07 | Novas turmas exigem escola ativa e responsável vinculado. A turma pode ser criada sem alunos. |
| RN08 | O cadastro de professor recusa login duplicado. O cadastro manual de aluno recusa número de chamada repetido na turma. A solicitação de acesso por matrícula também valida duplicidade. |
| RN09 | A primeira resposta congela a seleção de erros do ditado. Antes dela, mudanças autorizadas validam o destino e sincronizam as aplicações ativas. O contrato atual de edição não permite alterar o texto ou o áudio. |
| RN10 | Uma definição fica protegida contra edição após o primeiro uso em seleção. O arquivamento impede novo uso. Uma aplicação já atribuída que conserve a definição arquivada com regra executável continua a utilizá-la nas novas tentativas autorizadas. |
| RN11 | A seleção de definições e as novas atribuições verificam a publicação vigente e o escopo. Público geral abrange usuários autenticados autorizados à operação. Instituição restringe à escola. Turmas restringe às turmas-alvo. |
| RN12 | Em ditado legado sem seleção, uma nova tentativa recebe nota e registra erros como revisáveis. Classificações históricas permanecem disponíveis nos relatórios. |

### 8.4 Correspondência com o MVP

| Referência do MVP | Tratamento nesta versão |
|---|---|
| RF01–RF05, RF09, RF13, RF17, RF19, RF24–RF27 e RF30 | Mantêm o identificador com redação baseada no fluxo vigente. RF17 passa a tratar as categorias do ditado. RF27 passa a tratar atribuição e prazo. |
| RF06 e RF07 | A consulta ao texto de um ditado específico faz parte do RF13. O banco e o histórico global de palavras descritos no MVP permanecem sem fluxo confirmado. |
| RF08, RF20 e RF21 | O relatório por aplicação corresponde aos RF01 e RF04. Comparativos institucionais, emissão mensal e agrupamentos pedagógicos automáticos permanecem sem fluxo confirmado. |
| RF10, RF15, RF16, RF18 e RF23 | Recomendações, sugestões semânticas, rubrica de dificuldade, pronúncia individual pós-correção e dicas de ortografia seguem como itens do MVP sem implementação confirmada. |
| RF12 | Acesso administrativo passa a ser descrito por escopo e permissões nos RF25, RF35 e RF40. |
| RF14 | Consolidado no RF05, que descreve o retorno da correção ao fim da atividade. |
| RF22 e RF29 | As duas referências a OCR permanecem como escopo histórico. |
| RF28 | RF19 cobre a classificação por regras selecionadas. O uso de modelos e a revisão humana estão no planejamento da PER-18. |
| RF11 | A numeração do PDF salta de RF10 para RF12. |
| RNF01 | A gravação e a reprodução são funções dos RF03 e RF02. A compatibilidade dos navegadores é tratada no RNF05. |
| RNF02, RNF06 e RNF08 | Metas históricas sem comprovação de atendimento, registradas na seção 8.2. |
| RNF03–RNF05 e RNF07 | Preservados com escopo e evidências atualizados. |

Fontes dos requisitos e regras [API](../../Ditado.API/Controllers/) · [Serviços da aplicação](../../Ditado.Aplicacao/Services/) · [Contrato COR-01](../epicos/cor-01-classificacao-hibrida.md) · [Relatório PER-59](../relatorio-ciclo-per59-2026-11-16.md) · [QA do PER-37](https://linear.app/joaomoutinho/issue/PER-37/mt-047-testes-mysql-e-qa-independente#comment-f0763d68-4676-4a22-a0e1-529d097ede81)

### 8.5 Requisitos funcionais planejados para PG2

Os requisitos RF43 a RF49 e os casos CU15 a CU17 descrevem o plano aprovado em 18/11/2026. A COR-01/PER-22 concluiu a nota objetiva e as regras selecionadas. A PER-63 reúne as etapas PER-64 e PER-65 da classificação híbrida. A PER-66 trata da revisão humana e da exportação para pesquisa. [Contrato e sequência aprovados](../epicos/cor-01-classificacao-hibrida.md#entrega-híbrida-planejada--per-63)

| ID | Requisito planejado e critério verificável | Caso de uso e entrega |
|---|---|---|
| RF43 | Registrar todas as lacunas apresentadas em cada tentativa, incluindo acertos e lacunas sem resposta, preservando a resposta original e as referências da coleta. | CU10, CU15 · PER-64 |
| RF44 | Gravar com a tentativa a tarefa de análise e as referências versionadas dos candidatos selecionados sem executor. O processamento assíncrono deve permitir sugestões LLM e achados por código na mesma lacuna. | CU15 · PER-64 |
| RF45 | Permitir consultar o resultado autorizado após recarregar a página, mostrando a nota objetiva, a origem dos achados e o estado da análise assíncrona. | CU11, CU15 · PER-64 |
| RF46 | Processar os candidatos pela cadeia configurável de Groq, Cohere, Gonka Broker e OpenRouter. Acionar o próximo provedor em timeout, falha de rede, indisponibilidade ou limite informado pelo provedor, incluindo HTTP 429. | CU15 · PER-65 |
| RF47 | Validar integralmente a saída antes de gravar sugestões. Aceitar somente candidatos selecionados e recusar IDs desconhecidos ou duplicados e valores fora do contrato. Uma saída válida sem correspondência encerra a análise sem sugestão. | CU15 · PER-64, PER-65 |
| RF48 | Permitir ao revisor autorizado registrar decisões humanas por amostra ou exceção, preservando sugestões automáticas, evidências e origem. O perfil e o alcance dessa autorização aguardam definição. | CU16 · PER-66 |
| RF49 | Exportar, mediante autorização e auditoria, dois CSVs pseudonimizados ligados pelo ID da lacuna. O arquivo de respostas deve conter acertos e lacunas vazias. O arquivo de classificações deve indicar origem e versão aplicável. | CU17 · PER-66 |

### 8.6 Requisitos técnicos e de qualidade planejados para PG2

| ID | Atributo | Requisito planejado e validação prevista |
|---|---|---|
| RNF14 | Continuidade do processamento | Persistir a fila no MySQL, retomar tarefas após reinício e manter o processamento idempotente. Demonstrar retomada e ausência de sugestões duplicadas em ambiente isolado. |
| RNF15 | Independência da correção | Retornar a nota sem aguardar o worker. Preservar resposta original, gabarito, nota objetiva e achados por código durante falhas ou indisponibilidade dos provedores. |
| RNF16 | Configuração e rastreabilidade | Configurar ordem, modelos e limites de tempo e uso por ambiente. Registrar cada tentativa com provedor, modelo, versão das instruções, desfecho e motivo sanitizado da troca. Saída inválida, autenticação e erro de contrato devem seguir para falha ou revisão conforme o contrato. |
| RNF17 | Proteção e autorização dos dados | Manter credenciais em variáveis de ambiente ou gerenciador de segredos e verificar o escopo escolar na consulta dos resultados. A ativação com respostas reais exige definir os dados permitidos para envio externo. A pesquisa depende de perfis, permissões e escopo de acesso definidos. |
| RNF18 | Verificação da entrega | Demonstrar a PER-64 com provedor simulado e dados sintéticos em MySQL isolado, incluindo origem, estado, recarga e isolamento entre escolas. Na PER-65, comparar os quatro modelos na mesma amostra rotulada, em avaliação separada da rotação operacional. Executar build, testes pertinentes e demonstração antes do QA independente e atualizar Swagger quando o contrato da API mudar. |

Modelos, ordem por ambiente, limites, critério de aceite da amostra e dados permitidos para envio externo permanecem pendentes para ativação com respostas reais. Os critérios da amostra precisam ser definidos antes de afirmar qualidade de classificação. O feedback pedagógico sob demanda segue o planejamento específico da PG2. [Especificação PG2](../../Ditado/docs/ESPECIFICACAO_PG2.md) · [Contrato da classificação híbrida](../epicos/cor-01-classificacao-hibrida.md)

## 9. Arquitetura e tecnologias

### 9.1 Camadas

- `Ditado.API`: entrada HTTP, controllers, autenticação e Swagger;
- `Ditado.Aplicacao`: serviços, DTOs e orquestração dos casos de uso;
- `Ditado.Dominio`: entidades, enums e regras de domínio;
- `Ditado.Infra`: DbContext, configurações, persistência e migrations;
- `Ditado.Testes`: testes unitários e de integração.

### 9.2 Tecnologias registradas

- C# / .NET 9;
- ASP.NET Core Web API;
- Entity Framework Core 9;
- MySQL 8;
- JWT Bearer;
- Swagger/OpenAPI;
- xUnit e FluentAssertions;
- Vue/Vite no frontend relacionado;
- GitHub Actions e Azure App Service conforme README.

### 9.3 Componentes principais

- controllers em `Ditado.API/Controllers/`;
- serviços em `Ditado.Aplicacao/Services/`;
- entidades em `Ditado.Dominio/Entidades/`;
- enums em `Ditado.Dominio/Enums/`;
- persistência em `Ditado.Infra/Data/` e `Configurations/`;
- migrations em `Ditado.Infra/Migrations/`;
- testes em `Ditado.Testes/`.

## 10. API, persistência e modelo de dados

O modelo persistido inclui usuários, escolas e vínculos, sessões, eventos de auditoria, turmas, ditados com segmentos ordenados e áudio, categorias, publicações com escopo, aplicações e participações, respostas e definições do catálogo. `AplicacaoDitado` identifica a atribuição e pode ser referenciada por várias `RespostaDitado`. Há consultas de compatibilidade com `TurmaDitado` e tentativas anteriores. A migration da COR-01 acrescenta o vínculo da definição selecionada ao ditado e ao resultado de classificação e disponibiliza 12 definições públicas vinculadas a regras existentes. [Contexto de persistência](../../Ditado.Infra/Data/DitadoDbContext.cs) · [Migration COR-01](../../Ditado.Infra/Migrations/20261117050554_IntegrarCatalogoCor01.cs)

## 11. Casos de uso

O [diagrama editável](diagramas/casos-de-uso.md) relaciona os atores aos casos abaixo. A referência para autorização é a API, inclusive quando o frontend esconde uma ação.

### 11.1 Casos de uso e fluxos

| ID e objetivo | Ator e pré-condição | Fluxo principal e resultado | Alternativas ou exceções |
|---|---|---|---|
| CU01 — Autenticar-se e configurar primeiro acesso | Visitante com conta ou identificador de aluno cadastrado. | Professor ou administrador envia login e senha. O aluno usa matrícula ou código e PIN. No primeiro acesso, informa o identificador, confirma que pode configurar o PIN e o cadastra. O login válido cria uma sessão e encaminha ao painel do perfil. | Credencial inválida ou conta inativa impede entrada. Um PIN já configurado não pode ser definido por esse fluxo. |
| CU02 — Cadastrar e administrar contas | Visitante no cadastro, usuário autenticado no próprio perfil e professor ou administrador na gestão autorizada. | O professor informa seus dados e escolhe criar escola ou informar uma existente. Na gestão, operadores consultam e aprovam solicitações elegíveis ou administram contas conforme a autorização. O usuário autenticado pode alterar o próprio nome. A API prevê concessão e revogação auditadas da permissão global. | Login duplicado ou escola inválida é recusado. O cadastro em escola existente e a edição de credenciais pelo próprio perfil têm as limitações da seção 11.2. |
| CU03 — Sair ou revogar a sessão | Usuário autenticado. | Sair pela interface limpa a autenticação local e retorna ao login. A chamada autenticada à rota de revogação invalida a sessão no servidor. | Após revogação no servidor, o mesmo token perde acesso às rotas protegidas. A saída da interface não chama essa rota. |
| CU04 — Cadastrar alunos e importar chamada | Professor responsável ou administrador autorizado, com turma ativa. | Seleciona uma turma e cadastra uma ou mais linhas com nome e chamada. Se usar arquivo, a API gera uma prévia, a interface apresenta pendências e o operador confirma as linhas a cadastrar. O sistema vincula os alunos à turma e gera códigos de acesso. | Arquivo inválido ou acima dos limites é recusado. Matrícula é opcional. Chamadas duplicadas são recusadas e cadastros anteriores a uma falha no lote podem permanecer. |
| CU05 — Gerenciar turmas | Professor ou administrador autorizado. Aluno para consulta das próprias turmas. | O operador informa escola, responsável e dados da turma, salva e gerencia a associação de alunos. O aluno consulta a lista de turmas disponíveis no seu perfil. | A criação recusa escola inexistente ou inativa e responsável sem vínculo adequado. Operações sobre turma fora do escopo podem ser negadas pelo servidor. |
| CU06 — Preparar e consultar ditado | Professor ou administrador autorizado, com microfone disponível para gravação. | Define título e texto, marca lacunas, grava o áudio completo e escolhe categorias opcionais e ao menos um erro do catálogo acessível. Salva o ditado. Na consulta autorizada, visualiza o gabarito e edita metadados e seleção quando permitido. | Falha de gravação, seleção vazia ou erro fora de escopo impede concluir o cadastro. A seleção fica congelada a partir da primeira resposta. |
| CU07 — Gerenciar o Catálogo de erros | Professor ou administrador na interface, sujeito ao escopo da ação. | Lista ativos ou arquivados, consulta detalhes e publica, edita, arquiva ou restaura conforme a autorização. O registro publicado aparece aos usuários autorizados. O formulário oferece Público geral e Instituição. A API também aceita Turmas e permite alterar a publicação vigente. | Publicação com dados ou escopo inválidos é recusada. Definição arquivada ou já usada não aceita edição de conteúdo. Uma definição nova cadastrada pelo professor fica sem regra executável. |
| CU08 — Publicar, arquivar ou restaurar ditado | Professor ou administrador autorizado pela API. | Configura o alcance como Público geral, Instituição ou Turmas e informa os alvos quando exigidos. Arquivar inativa o recurso. Restaurar reativa. | Escopo inválido ou falta de autorização impede a alteração. Enquanto inativo, o serviço atual também impede novas submissões, inclusive para aplicações anteriores. |
| CU09 — Atribuir ditado à turma | Professor ou administrador autorizado. Ditado ativo com publicação vigente compatível. | Seleciona turma e data-limite. O servidor valida escola, turma, alcance e erros selecionados e cria uma aplicação com os participantes e a seleção autorizada. A API permite atualizar o prazo ou desativar a atribuição. | Ditado arquivado, aplicação duplicada ou escopo incompatível é recusado. Alterar o prazo sinaliza atraso no painel. |
| CU10 — Realizar ditado e receber correção | Aluno autenticado com atividade disponível. | Abre o ditado, ouve a gravação, preenche as lacunas e envia. O servidor calcula a nota, classifica as respostas compatíveis com as regras selecionadas, persiste a tentativa e retorna os detalhes. A interface apresenta o resultado. | O formulário requer todas as lacunas preenchidas. A API recusa submissão fora do escopo nos cenários COR-01 validados. Resposta errada sem regra correspondente fica revisável. |
| CU11 — Consultar tentativas próprias | Aluno autenticado. | O painel indica prazo, atraso, número de tentativas e melhor nota. O endpoint de tentativas detalha os registros próprios associados ao ditado. | Ditados sem tentativa aparecem sem melhor nota. A consulta detalhada tem cobertura de API. |
| CU12 — Acompanhar resultados da turma | Professor ou administrador com acesso à aplicação. | Abre a lista de ditados atribuídos e seleciona uma turma e um ditado. Consulta conclusão, notas por aluno e agregações de erros por palavra e tipo quando disponíveis. | Turma fora do escopo é recusada. O relatório principal usa a melhor tentativa por aluno e conserva as demais tentativas no histórico. |
| CU13 — Exportar tentativas | Professor ou administrador autorizado pela API. | Informa as escolas-alvo e, quando necessário, cursor e limites. A API retorna CSV com identificadores, data e nota das tentativas vinculadas, sem áudio, e indica o próximo cursor. A operação tem auditoria. | Escolas fora do escopo, parâmetros inválidos, estouro de limites de tamanho ou tempo e cancelamento interrompem a geração. O CSV contém identificadores internos. |
| CU14 — Remover áudio elegível | Administrador Geral autenticado pela API. | Lista áudios não vazios de ditados criados há pelo menos 24 horas e sem referência. Confirma os candidatos. O serviço revalida a elegibilidade antes de limpar e registra a operação. | Áudio vinculado a aplicação, atribuição ou resposta é preservado. Requisições sem permissão são recusadas. |

### 11.2 Diferenças entre contrato, interface e comportamento observado no código

- **Cadastro em escola existente (CU02).** O cadastro grava `Professor` inativo e vínculo inativo. A listagem e a rota de aprovação filtram `AcessoSolicitado`. A interface informa que o usuário deve aguardar aprovação, mas esse caminho de aprovação não está fechado pelos métodos analisados. Para escola nova, a mensagem de confirmação também pede aguardar, embora o backend ative a conta. [Serviço de usuários](../../Ditado.Aplicacao/Services/UsuarioService.cs)
- **Edição do perfil (CU02).** A tela `PerfilUsuario.vue` envia `email` e `senha`, mas o contrato de atualização não edita o login e exige `senhaAtual` e `senhaNova` para trocar a credencial. O requisito de tela confirmado no código cobre a edição do nome. [DTO de atualização](../../Ditado.Aplicacao/DTOs/Usuarios/AtualizarUsuarioRequest.cs) · [Serviço de usuários](../../Ditado.Aplicacao/Services/UsuarioService.cs)
- **Publicação e atribuição (CU06, CU08 e CU09).** O formulário de ditado cria o cadastro e o backend exige publicação vigente para atribuir. O caminho confirmado de publicação do ditado é pela API. O ciclo COR-01 também utilizou publicação e participantes com vínculos preparados para o cenário de teste. [Controller de ditados](../../Ditado.API/Controllers/DitadosController.cs) · [Ciclo COR-01](../relatorio-ciclo-cor01-2026-11-17.md)
- **Vínculo do aluno (CU04, CU09 e CU10).** O cadastro manual adiciona o aluno à turma. O trecho que cria o aluno não cria um `VinculoEscolar` explícito, que o fluxo de submissão com seleção exige em aplicações escolares. A passagem de uma conta recém-cadastrada por todo esse fluxo permanece a verificar. Ingressos posteriores à atribuição também precisam de verificação nos caminhos que dependem de participação na aplicação. [Serviço de usuários](../../Ditado.Aplicacao/Services/UsuarioService.cs) · [Serviço de turmas](../../Ditado.Aplicacao/Services/TurmaService.cs) · [Serviço de ditados](../../Ditado.Aplicacao/Services/DitadoService.cs)
- **Arquivamento do ditado (CU08 e CU10).** O contrato MT-04 prevê novas tentativas em aplicações anteriores. O código atual inativa o ditado e a submissão exige o estado ativo. O arquivamento da definição de erro segue a regra distinta do RN10. [MT-04](../epicos/multi-tenant-mt04.md) · [Serviço de visibilidade](../../Ditado.Aplicacao/Services/VisibilidadeService.cs) · [COR-01](../epicos/cor-01-classificacao-hibrida.md)
- **Importação do catálogo (CU07).** O frontend oferece a tela `ImportarErros.vue` e chama `POST /api/ErrosCatalogo/importar`. A API analisada não oferece essa rota. A importação de chamada de alunos usa um endpoint próprio no CU04. [Controller do catálogo](../../Ditado.API/Controllers/ErrosCatalogoController.cs) · [Controller de usuários](../../Ditado.API/Controllers/UsuariosController.cs)
- **Exportação (CU13).** O controller monta um CSV temporário e o remove no bloco de finalização. O referencial do MT-04 prevê download direto sem arquivo temporário. O CSV documentado aqui é o da API operacional. A exportação pseudonimizada para pesquisa está no planejamento da PER-66. [Controller de exportações](../../Ditado.API/Controllers/ExportacoesController.cs) · [Referencial do projeto](../REFERENCIAL_PROJETO.md) · [Planejamento de pesquisa](../epicos/cor-01-classificacao-hibrida.md#sequência-da-entrega)

### 11.3 Casos de uso planejados para PG2

| ID e objetivo | Participantes e pré-condição planejados | Fluxo planejado e resultado | Alternativas ou exceções |
|---|---|---|---|
| CU15 — Consultar resultado com análise híbrida | Aluno ou pessoal autorizado no escopo escolar da tentativa. A submissão dispara o processamento de candidatos selecionados sem executor. | Registra todas as lacunas e a tarefa com os candidatos versionados. Retorna a nota objetiva. O worker processa a fila no MySQL e valida a saída antes de gravar sugestões. Ao consultar ou recarregar o resultado, o usuário acompanha o estado e a origem dos achados. | Falhas técnicas permitem avanço na cadeia configurada. Saída válida sem correspondência encerra sem sugestão. Saída inválida, autenticação ou erro de contrato são registrados para falha ou revisão. Nota e achados por código são preservados. |
| CU16 — Revisar classificações | Revisor com permissão e escopo ainda a definir para a PER-66. | Acessa uma amostra ou exceção autorizada, consulta sugestões e evidências e registra a decisão humana com sua origem, preservando as sugestões automáticas. | O acesso depende da definição das permissões. A liberação requer validação do escopo e da rastreabilidade da decisão. |
| CU17 — Exportar dados de pesquisa | Operador de pesquisa com perfil, permissão e escopo ainda a definir para a PER-66. | Solicita a exportação autorizada. O sistema registra auditoria e gera os CSVs pseudonimizados de respostas e classificações ligados pelo ID da lacuna, incluindo acertos, lacunas vazias, origem e versão aplicável. | A liberação depende das definições de acesso de pesquisa. O envio a provedores externos depende das condições de ativação registradas na seção 8.6. |

O fluxo de classificação será demonstrado primeiro com provedor simulado na PER-64 e validado com os quatro provedores na PER-65. Revisão e exportação seguem na PER-66. [Sequência e critérios de aceite](../epicos/cor-01-classificacao-hibrida.md#sequência-da-entrega)

## 12. Fundamentação pedagógica

Os três PDFs em `docs/referencias/` sustentam discussões sobre erros ortográficos, fonologia, escrita infantil e categorias de análise.

O relatório deve separar:

- diferença observável entre resposta e esperado;
- operação textual, como substituição, omissão ou acréscimo;
- interpretação pedagógico-linguística sobre a possível motivação do erro.

Uma troca de letras, isoladamente, não prova transcrição da fala. A classificação causal deve ser tratada como hipótese quando não houver evidência suficiente.

## 13. Segurança, privacidade e operação

O backend pode receber por configuração de ambiente os dados de conexão e assinatura de tokens. O ambiente local possui Docker Compose e scripts de build e operação. O acesso a dados de alunos e as exportações dependem da autorização de cada operação. O código do CSV operacional inclui IDs internos e usa o tratamento de arquivo descrito na seção 11.2. [Configuração da API](../../Ditado.API/Program.cs) · [Operação local](../../README.md) · [PER-36](https://linear.app/joaomoutinho/issue/PER-36/mt-046-audio-e-exportacao-segura)

O desenho de recuperação prevê cópia integral e coordenação de arquivos externos quando existirem. Os parâmetros de operação e segurança dependem do ambiente universitário. As metas do MVP e a validação pendente das características de qualidade estão na seção 8.2. [Backup e restauração](https://linear.app/joaomoutinho/issue/PER-13/cat-08-backup-e-restauracao-integral-do-banco-de-dados-enabler)

## 14. Diagramas

O índice completo está em [`indice-diagramas.md`](indice-diagramas.md).

### Diagramas existentes

- [`diagramas/catalogo-er.md`](diagramas/catalogo-er.md): modelo ER proposto do catálogo; status: revisão pendente, não representa integralmente o schema implementado.
- [`diagramas/casos-de-uso.md`](diagramas/casos-de-uso.md) relaciona atores e casos da seção 11 no estado observado em 17/11/2026.

### Diagramas previstos

- contexto e arquitetura geral;
- fluxo de realização e correção;
- sequência de submissão de resposta;
- modelo ER atual e planejado, separados quando necessário;
- arquitetura de componentes e implantação;
- fluxos de catálogo, feedback, relatórios e multi-tenant após validação dos contratos.

## 15. Metodologia, desenvolvimento e validação

O projeto é organizado por épicos e histórias no Linear. O fluxo de trabalho exige documentação e diagramas antes da implementação de épicos com dependências ou decisões bloqueadoras.

### Sequência planejada do MT-04

1. baseline, documentação, ER e contratos;
2. inventário e backup/restauração em cópia;
3. fundação de `EscolaId`, vínculos e integridade;
4. autorização, sessões e auditoria;
5. aplicação única por turma/ditado e tentativas;
6. visibilidade e arquivamento;
7. limpeza de áudio após 24 horas e exportação direta sem áudio;
8. testes MySQL;
9. QA;
10. UX e protótipos antes das telas.

Dependências e riscos registrados: autorização antes de rotas tenant-aware; aplicação antes de CAT-05/COR-01; backup e armazenamento antes da limpeza de áudio; isolamento entre escolas, preservação do histórico, revogação, uso de disco, validação dos contratos e protótipos UX antes da implementação das telas. A sequência não autoriza implementação por si só.

### Decomposição Linear do MT-04

- **PER-31:** inventário e baseline;
- **PER-32:** fundação de `EscolaId`;
- **PER-33:** autorização, sessões e auditoria;
- **PER-34:** aplicações, participação e tentativas;
- **PER-35:** visibilidade e arquivamento;
- **PER-36:** áudio e exportação;
- **PER-37:** testes MySQL e QA;
- **PER-38:** protótipos UX.

Esta decomposição registra a sequência inicial. Os marcos abaixo são históricos, com data e ambiente próprios.

### Baseline registrado do PER-31

- branch de trabalho: `codex/mt04-foundation-tenant`;
- inventário somente leitura confirmou que `Escolas` e `VinculosEscolares` ainda não existem;
- histórico do MySQL conferido até `20261106051829_PublicarErroMarketplace`;
- backup lógico privado pré-migration concluído com `mysqldump --single-transaction --no-tablespaces --routines --triggers`;
- nenhuma migration, dado ou serviço foi alterado neste baseline;
- implementação permanece em andamento, com bloqueio técnico de `pending model changes` por divergência entre assembly de inicialização/API e snapshot EF;
- fundação e migration permanecem locais até a validação.

### Atualização do PER-31 e PER-32

Em 07/11/2026, PER-31 e PER-32 foram registrados como concluídos. A migration `20261107044724_AdicionarFundacaoTenant` foi aplicada uma vez após backup e validação do modelo. As tabelas `Escolas` e `VinculosEscolares` foram criadas vazias; FKs, índices e restrições únicas foram validados, e os dados legados foram preservados. API e frontend responderam HTTP 200. O Fullstack registrou `MarketplaceControllerTests` com 33/33 aprovados. O QA confirmou schema e regressão sem achado funcional, com limitação do runner por ausência de `testhost.dll`. O PER-33 foi iniciado.

Durante o PER-33 ocorreu um incidente controlado de migration: `dotnet ef migrations remove --no-build --force` utilizou um assembly anterior e reverteu localmente apenas as tabelas novas vazias `Escolas`/`VinculosEscolares` e o respectivo histórico. Nenhum dado legado foi alterado. Em seguida, a migration `20261107050300_AdicionarFundacaoTenant` foi aplicada após backup e revisão SQL. Foram criadas `AuditoriaEventos`, `Escolas`, `SessoesUsuarios` e `VinculosEscolares`, todas com zero registros; não houve `DROP`, `UPDATE` ou backfill. O histórico e `has-pending-model-changes` foram confirmados. A suíte/build atual não foi concluída por limitações ambientais do runner/SDK, incluindo `SecurityException` de telemetria; não foram reportados erros de código. O QA oficial foi acionado e o PER-34 permanece bloqueado até validação.

Na evolução do PER-33, foi incluído `GET /api/usuarios/contexto-escolar`, com validação server-side por vínculo e escola ativa, além de auditoria de decisões permitidas e negadas. Foram preparados testes InMemory isolados para serviço e revogação. O QA revisou os testes, mas o runner/SDK não conseguiu executá-los por falha de workloads e saída vazia, resultando em zero testes executados. Permanecem lacunas de E2E autenticado, controller/claims, escola inativa e concorrência. O PER-33 continua In Progress e o PER-34 permanece bloqueado.

Fechamento posterior do PER-33: o QA aceitou a entrega após confirmação do schema/migration e aprovação de 6/6 testes isolados de serviço e controller/header. O build .NET 9 ficou estável, com zero erros e apenas avisos preexistentes. Permanecem como limitações futuras, sem bloqueio, a cobertura runtime específica para claims/JTI inválido, expiração e concorrência. No Linear, PER-33 foi movido para Done e PER-34 está In Progress.

### Decisão confirmada do PER-34

Foi adotada compatibilidade temporária: o legado `TurmaDitado`/`TurmaAluno`/`RespostasDitados` permanece preservado e legível; novas escritas ocorrerão exclusivamente no modelo `AplicacaoDitado`/`Participacao*`, sem escrita dupla. A limpeza do banco não está autorizada. Qualquer migration depende de adaptadores e testes.

Decisão aprovada no PER-35: `Turma.EscolaId` será inicialmente nullable. Não haverá backfill automático das turmas legadas; novas turmas e novas publicações exigirão escola definida. Registros antigos com valor nulo permanecerão legíveis até que exista mapeamento explícito.

Após o gate de `Turma.EscolaId`, com migration aplicada e aceitação do QA, o PER-35 entrou na implementação funcional de visibilidade e arquivamento para Ditados e Catálogo de erros. Permanecem aguardando os arquivos, testes, revisão do QA e novo gate de backup/migration.

No PER-35, a implementação foi corrigida para comportamento fail-closed: vínculo, papel e ator são obrigatórios; Administrador sem vínculo é negado; restauração exige autorização; e concorrência é controlada. A validação final está em andamento. Permanece pendente a definição explícita da capability/claim do Administrador Geral/Técnico; a ausência de vínculo não deve conceder essa permissão por inferência.

Decisão aprovada: o Administrador Geral/Técnico será identificado por capability explícita persistida, desativada por padrão. Acesso global não será inferido pela ausência de vínculo escolar. Implementação e validação permanecem pendentes.

Atualização de implementação: `Usuario.AdministradorGeral` foi criado com valor padrão `false`, com claim JWT e guards fail-closed. Nenhuma conta existente foi promovida. Uma migration aditiva foi gerada, mas ainda não aplicada. Foram registrados 6/6 testes aprovados e build sem erros; permanecem pendentes QA e gate de backup.

O PER-35 foi encerrado após QA e gate pós-DDL. O PER-36, dedicado a áudio e exportação segura, foi iniciado. Permanecem válidas as decisões de limpeza em duas fases após 24 horas, preservação de áudios referenciados e exportação sem áudio com limites.

### Evidências consultadas na revisão de 17/11/2026

- **PER-37, 07 a 08/11.** O QA registrou em MySQL isolado verificação de escopo, revogação, exportação, retenção de áudio e auditoria. Registrou também a execução containerizada local e a limitação na repetição da suíte pelo runner de QA. PER-36 e PER-37 constam como concluídas. [Aceite do PER-37](https://linear.app/joaomoutinho/issue/PER-37/mt-047-testes-mysql-e-qa-independente#comment-f0763d68-4676-4a22-a0e1-529d097ede81)
- **PER-59, 16/11.** QA independente aprovou publicação, listagem e detalhe do catálogo em MySQL isolado e navegador, incluindo visibilidade entre duas escolas. A issue consta como concluída. [Relatório PER-59](../relatorio-ciclo-per59-2026-11-16.md)
- **COR-01, 17/11.** O ciclo registrou builds da API e do frontend e demonstração por HTTP em MySQL isolado. No backend `424ed94`, os 140 testes pertinentes passaram em TestServer com EF InMemory. A suíte completa teve 257 aprovações em 278 testes, com as mesmas 21 falhas preexistentes. A coordenação recompilou a API, aprovou 50 testes direcionados e encaminhou a PER-22 para QA independente em navegador e MySQL isolado. A issue está em revisão. [Relatório COR-01](../relatorio-ciclo-cor01-2026-11-17.md) · [Registro da coordenação](https://linear.app/joaomoutinho/issue/PER-22/cor-01-integrar-erros-mapeados-a-correcao-existente#comment-0c852385-ab91-49eb-a5e3-10698e324f7e)
- **COR-01, 18/11.** A PER-22 consta como concluída no Linear após o reteste independente da correção por código. A classificação por LLM segue como entrega planejada da PER-63. [PER-22](https://linear.app/joaomoutinho/issue/PER-22/cor-01-integrar-erros-mapeados-a-correcao-existente) · [Contrato da classificação híbrida](../epicos/cor-01-classificacao-hibrida.md)

A revisão documental conferiu o código e essas evidências de ciclo. A aplicação institucional e os ambientes de teste não foram executados novamente nesta revisão.

## 16. Decisões, riscos e pendências

### Decisões confirmadas

- correção objetiva e nota permanecem independentes de LLM;
- tipos de operação podem ser múltiplos e separados;
- transcrição da fala e hipercorreção foram retiradas da inferência automática desta entrega;
- Catálogo de erros é o termo de produto vigente;
- decisões de multi-tenant devem tratar escola como unidade organizacional, conforme referencial consolidado.

### Pendências

- Confirmar os dados acadêmicos e a composição atual da equipe.
- Validar as diferenças de comportamento da seção 11.2 e as rotas legadas que permanecem em uso.
- Concluir a homologação de navegadores, desempenho e disponibilidade.
- A PER-18 prevê classificação assíncrona em duas fatias, primeiro o fluxo com provedor simulado (PER-64) e depois a validação da cadeia com Groq, Cohere, Gonka Broker e OpenRouter (PER-65). A PER-66 prevê revisão humana e exportação pseudonimizada para pesquisa. A ativação com dados reais depende de modelos, limites, critério de aceite da amostra e dados autorizados para envio externo. [Contrato e sequência da entrega](../epicos/cor-01-classificacao-hibrida.md)
- Integrar a tela de importação JSON do catálogo com a API e definir a operação institucional de backup e restauração, conforme as histórias correspondentes.

### Riscos e dívida técnica

- nomenclatura técnica legada `Marketplace*`;
- documentação histórica com propostas incompatíveis;
- dependência entre catálogo, classificação, feedbacks e relatórios;
- rotas com compatibilidade legada e cobertura desigual de validação por escola;
- três migrações legadas de cadastro, PIN e chamada não descobertas pelo EF no banco vazio dos ciclos PER-59 e COR-01;
- 21 falhas preexistentes na suíte completa registrada no ciclo COR-01;
- risco de transformar inferência pedagógica em regra automática sem evidência.

## 17. Histórico desta documentação

| Versão | Data | Alteração |
|---|---|---|
| V0 | 07/11/2026 | Criação da primeira prévia do relatório vivo, com estado atual, planejamento, fontes, lacunas e diagramas previstos. |
| V0.1 | 07/11/2026 | Inclusão do refinamento planejado do MT-04 sobre persistência, isolamento, histórico, aplicações entre escolas, sessões e auditoria. |
| V0.2 | 07/11/2026 | Registro da decisão confirmada de aplicação única por turma, novas tentativas por aluno e referência à mesma aplicação/revisão imutável. |
| V0.3 | 07/11/2026 | Registro da decisão confirmada de imutabilidade do ditado aplicado/iniciado e substituição por novo cadastro quando houver correção de conteúdo. |
| V0.4 | 07/11/2026 | Inclusão do rastreamento do MT-04 na issue Linear PER-30, ainda em refinamento técnico e sem implementação. |
| V0.5 | 07/11/2026 | Registro do parecer técnico incremental do MT-04, separado de decisões confirmadas e sem implementação. |
| V0.6 | 07/11/2026 | Registro da decisão sobre arquivamento, novas tentativas e remoção de aluno da turma no MT-04. |
| V0.7 | 07/11/2026 | Registro das decisões confirmadas sobre MySQL/InnoDB, isolamento por EscolaId, retenção de áudios e limites de exportação do MT-04. |
| V0.8 | 07/11/2026 | Registro da sequência planejada do MT-04, suas dependências e riscos, sem autorização automática de implementação. |
| V0.9 | 07/11/2026 | Refinamento da sequência do MT-04 com backup/restauração, EscolaId, autorização, dependências de CAT-05/COR-01 e limpeza de áudio. |
| V0.10 | 07/11/2026 | Registro da decomposição do MT-04/PER-30 em PER-31 a PER-38, com status e dependências planejadas. |
| V0.11 | 07/11/2026 | Registro factual do baseline do PER-31, backup lógico, estado do schema e bloqueio técnico de validação. |
| V0.12 | 07/11/2026 | Registro da conclusão documentada do PER-31/PER-32, aplicação da migration, validações e início do PER-33. |
| V0.13 | 07/11/2026 | Registro do incidente controlado de migration no PER-33, impacto limitado às tabelas novas vazias e gate de QA antes do PER-34. |
| V0.14 | 07/11/2026 | Atualização do PER-33 após aplicação da migration 20261107050300, criação das tabelas vazias, validações e bloqueio do PER-34 por QA. |
| V0.15 | 07/11/2026 | Registro do endpoint de contexto escolar, autorização server-side, auditoria, testes preparados e limitações de execução do PER-33. |
| V0.16 | 07/11/2026 | Registro do fechamento do PER-33 pelo QA, 6/6 testes aprovados, build estável e transição do PER-34 para In Progress. |
| V0.17 | 07/11/2026 | Registro da compatibilidade temporária confirmada no PER-34, sem escrita dupla e sem limpeza do banco. |
| V0.18 | 07/11/2026 | Registro do ajuste dos relatórios do professor para melhor resultado principal, preservação das tentativas e investigação posterior no PER-40. |
| V0.19 | 07/11/2026 | Registro do fechamento do PER-34, validações do QA, preservação do legado e transição do PER-35 para In Progress. |
| V0.20 | 07/11/2026 | Registro da decisão do PER-35 sobre EscolaId nullable, ausência de backfill automático e exigência para novos registros. |
| V0.21 | 07/11/2026 | Registro do início da implementação funcional de visibilidade e arquivamento no PER-35 após o gate de Turma.EscolaId. |
| V0.22 | 07/11/2026 | Registro da correção fail-closed do PER-35 e da pendência explícita sobre capability/claim do Administrador Geral/Técnico. |
| V0.23 | 07/11/2026 | Registro da decisão aprovada de capability persistida e desativada por padrão para Administrador Geral/Técnico. |
| V0.24 | 07/11/2026 | Registro da implementação da capability AdministradorGeral, claim/guards fail-closed, migration ainda não aplicada e gates pendentes. |
| V0.25 | 07/11/2026 | Registro do encerramento do PER-35 e início do PER-36 sobre áudio e exportação segura. |
| V0.26 | 17/11/2026 | Atualização dos atores, requisitos, regras e casos de uso do MVP com rastreabilidade e evidências do código atual. |
| V0.27 | 18/11/2026 | Incorporação do planejamento aprovado da PER-63 a PER-66 nos requisitos funcionais, técnicos e casos de uso, com pendências de ativação e acesso de pesquisa. |

## 18. Referências principais

- [`README.md`](../../README.md)
- [`docs/REFERENCIAL_PROJETO.md`](../REFERENCIAL_PROJETO.md)
- [`Ditado/docs/ESPECIFICACAO_PG2.md`](../../Ditado/docs/ESPECIFICACAO_PG2.md)
- [`Ditado/docs/HISTORIAS_EPICO_CATALOGO.md`](../../Ditado/docs/HISTORIAS_EPICO_CATALOGO.md)
- [`Ditado/DOCUMENTACAO.md`](../../Ditado/DOCUMENTACAO.md) — contexto legado, sujeito a confronto com o código atual
- [`docs/referencias/README.md`](../referencias/README.md)
- [`docs/epicos/`](../epicos/)
- [`docs/relatorio-ciclo-cat02-2026-11-06.md`](../relatorio-ciclo-cat02-2026-11-06.md)
- [`docs/relatorio-ciclo-cor01-2026-11-05.md`](../relatorio-ciclo-cor01-2026-11-05.md)
- [`docs/relatorio-ciclo-per25-2026-11-06.md`](../relatorio-ciclo-per25-2026-11-06.md)
- [`docs/relatorio-ciclo-per59-2026-11-16.md`](../relatorio-ciclo-per59-2026-11-16.md)
- [`docs/relatorio-ciclo-cor01-2026-11-17.md`](../relatorio-ciclo-cor01-2026-11-17.md)
- Frontend local em `14905a7`. Fontes principais em `src/views/`, `src/services/`, `src/stores/auth.js` e `src/router/index.js`.
- `Cópia de Template PI2_2025.2_TG.docx (2).pdf`, versão inicial de setembro de 2025. Atores e requisitos nas páginas 7 a 11 do arquivo. Casos de uso nas páginas 12 a 17.
- Projeto Linear Ditado: https://linear.app/joaomoutinho/project/ditado-29348d0fee1d

## 19. Validação acadêmica pendente

- Confirmar identificação acadêmica, autoria, padrão institucional e redação dos objetivos.
- Revisar o diagrama de casos de uso para a edição acadêmica.
- Conferir os diagramas e protótipos restantes do PDF do MVP quando forem incorporados à próxima revisão.
