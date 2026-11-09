# Coordenação do projeto

## Infraestrutura local de testes MySQL (Docker Desktop)

- Container existente para validação local: `ditado-mysql`.
- Imagem observada: `mysql:8.0.21`.
- Contexto Docker: `desktop-linux`.
- Mapeamento observado: `127.0.0.1:3306 -> 3306/tcp`.
- Estado confirmado em 07/11/2026: container `healthy` e Engine do Docker Desktop ativo.
- Usar o container existente para o gate do PER-37; não recriar, remover, resetar ou limpar dados sem backup e autorização explícita.
- Essas informações são apenas de desenvolvimento local; não registrar senhas, tokens ou valores de conexão neste arquivo.
- Priorizar scripts reutilizáveis existentes para operações repetitivas. Para reiniciar a API local, usar `powershell -ExecutionPolicy Bypass -File .\scripts\restart-api.ps1`; parâmetros úteis incluem `-Port`, `-Schema`, `-BuildOutput` e `-SkipBuild`.
- Para validar a compilação da API, usar `powershell -ExecutionPolicy Bypass -File .\scripts\build-api.ps1`; o script fixa o SDK .NET 9 do `global.json`, evita servidores persistentes e limita o MSBuild a um nó.
- Para execução containerizada, usar `powershell -ExecutionPolicy Bypass -File .\scripts\docker-up.ps1 -Build`, `docker-status.ps1`, `docker-logs.ps1` e `docker-down.ps1`; o Compose preserva o container MySQL existente `ditado-mysql`.

- Antes de levar uma dúvida de escopo ao usuário, consulte o responsável pelo assunto.
- Antes de encaminhar qualquer dúvida de escopo a um agente ou ao usuário, consulte primeiro `Ditado/docs/ESPECIFICACAO_PG2.md` (especificações normativas). Em seguida, confronte o caso com `docs/REFERENCIAL_PROJETO.md` e os contratos do épico. A maioria das decisões já está explicitada nas especificações; só encaminhe a dúvida quando a documentação não resolver o caso ou houver conflito explícito entre fontes.
- Consulte o agente `researcher` somente para dúvidas sobre o conteúdo dos três PDFs enviados e a parte pedagógica de língua portuguesa. Ele é o especialista professor de português dessas referências, guardadas em `docs/referencias/`.
- Questões técnicas da implementação ficam com o Fullstack 01. Arquitetura, contratos, persistência, dependências, ordem das histórias, versionamento e gerenciamento do projeto devem consultar o agente `gerente_projetos`.
- Catálogo, seleção de erros, validações de formulário e regras operacionais do produto não exigem consulta ao researcher por si só. Se uma questão técnica depender de um critério pedagógico, consulte-o apenas sobre esse critério e separe os pareceres.
- Envie aos consultores a dúvida concreta, as decisões anteriores e as fontes relevantes. Solicite alternativas, recomendação, justificativa e incertezas.
- Toda dúvida de escopo deve sempre ser encaminhada ao usuário para decisão, acompanhada dos pareceres pertinentes e de suas fontes.
- Os consultores apenas opinam: não decidem, aprovam nem encerram dúvidas de escopo. Uma recomendação não autoriza implementação nem mudança de decisão confirmada.
- Distinga decisões já confirmadas, evidências e recomendações. Aguarde a decisão do usuário antes de implementar o comportamento que depende da dúvida.
- Se um consultor não estiver acessível, informe a limitação antes de encaminhar a dúvida ao usuário; não afirme que houve consulta.
- Antes de consultar ou enviar qualquer dúvida a outro chat/agente em nome da documentação, perguntar primeiro ao usuário e aguardar autorização explícita. Só consultar outro agente sem nova pergunta quando o usuário já tiver autorizado claramente essa consulta para o caso específico.
- O Agente de Documentação Viva atua como receptor e registrador: deve atualizar relatório, changelog e diagramas a partir de decisões, pareceres e mudanças já produzidos, sem revisar tecnicamente, aprovar, opinar ou decidir escopo.
- Por decisão do usuário, a geração e atualização de artefatos derivados está pausada até segunda ordem: não gerar nem atualizar DOCX, PDF, PNG ou SVG. Durante este período, atualizar somente os arquivos Markdown e as fontes editáveis dos diagramas quando necessário.

## Execução do desenvolvimento

- O responsável pelo desenvolvimento é o chat **Fullstack 01 — Ditado**, ID `01a1aeeb-e805-7010-a967-afb23a988599`, host `local`. Ele substitui o antigo subagente `fullstack`; não atribuir desenvolvimento ao subagente anterior.
- A coordenação fica no chat **SQUAD DITADO**, ID `01a1aa39-f543-7cc1-9825-3bbda26ecd38`. O QA oficial fica no chat **QA — COR-01 e validação visível**, ID `01a1ad2c-2c50-7280-83ca-1db03679fb20`.
- Aplicar clean code, Clean Architecture e padrões úteis, considerando escalabilidade sem acrescentar complexidade desnecessária.
- Alterações de comportamento devem ter testes pertinentes e atualizar os testes afetados; coordenar a validação independente com o QA.
- Gate de validação: o Fullstack deve concluir a implementação, executar build/testes pertinentes e demonstrar o fluxo funcional ao coordenador antes de acionar o QA. O QA entra somente depois dessa confirmação, para validação independente; não usar o QA para investigar ou iterar uma implementação ainda quebrada.
- Todo endpoint novo ou alterado deve atualizar as anotações do Swagger/OpenAPI correspondentes, incluindo resumo, parâmetros/corpo, respostas HTTP, autorização e exemplos quando aplicável; validar a documentação gerada após recompilar a API.
- Manter o Linear atualizado e produzir relatório de ciclo com alterações, motivos, arquivos, validações e limitações.
- Após cada etapa concluída, atualizar a tarefa correspondente no Linear com o que foi feito, arquivos afetados, validação executada e eventuais limitações ou bloqueios.
- Não criar ou alterar tarefas por inferência quando a correspondência com o trabalho atual não for clara; registrar a etapa como comentário quando o status não precisar mudar.
- Trabalhar fora de `master` e `main`, usando branches `codex/` e preservando alterações existentes. Coordenar reinícios dos serviços antes de interromper testes manuais ou do QA.

## Documentação viva e fontes do projeto

- Este repositório é o backend da aplicação **Ditado**. A documentação evolutiva deve ser mantida em `docs/`; não foi encontrada uma pasta literal chamada `documentação` neste checkout.
- O relatório acadêmico/documentação principal deve ficar em `docs/documentacao/relatorio-aplicacao.md` quando criado ou atualizado. O histórico das mudanças documentais deve ficar em `docs/documentacao/changelog-documentacao.md`.
- Diagramas editáveis e suas renderizações devem ficar em `docs/documentacao/diagramas/`. PNG ou SVG acompanham a fonte editável; a imagem nunca substitui a fonte.
- O índice de diagramas deve ficar em `docs/documentacao/indice-diagramas.md`, registrando finalidade, versão, seção do relatório, fontes, premissas e status de validação.
- Antes de documentar uma afirmação, classifique-a como: implementada/confirmada, planejada, hipótese/recomendação, herdada de documentação legada ou desconhecida. Não transforme proposta em comportamento implementado.

### Hierarquia de fontes

- `docs/REFERENCIAL_PROJETO.md` é o índice consolidado das fontes normativas, decisões aceitas, pendências e sequência de planejamento. Consulte-o antes de abrir histórias ou revisar contratos.
- `Ditado/docs/ESPECIFICACAO_PG2.md` é a fonte principal para decisões técnicas globais, dependências, segurança, operação e critérios transversais.
- `Ditado/docs/HISTORIAS_EPICO_CATALOGO.md` é a fonte para o mapa CAT-01 a CAT-10 e seus critérios de aceite.
- `docs/epicos/` contém contratos, refinamentos, propostas e relatórios específicos. Cada arquivo deve ser lido com atenção ao status: aceite, proposta, pendência ou legado.
- `README.md` descreve a visão introdutória do produto, tecnologias, execução e estrutura. `Ditado/DOCUMENTACAO.md` fornece contexto arquitetural e de atores, mas deve ser confrontado com as decisões multi-tenant e com o código atual antes de ser tratado como normativo.
- `docs/referencias/README.md` identifica os três PDFs pedagógicos e resume o que eles permitem afirmar. Consulte o agente `researcher` somente para dúvidas de conteúdo dessas referências ou de pedagogia de língua portuguesa.

### Estado de referência identificado no levantamento

- A solução é uma API ASP.NET Core Web API em .NET 9, com Entity Framework Core 9, MySQL 8, autenticação JWT Bearer, Swagger/OpenAPI, xUnit e FluentAssertions; o README também registra CI/CD para Azure App Service.
- A estrutura de camadas documentada é `Ditado.API`, `Ditado.Aplicacao`, `Ditado.Dominio`, `Ditado.Infra` e `Ditado.Testes`, seguindo Clean Architecture/N-Tier.
- O produto base cobre autenticação, usuários, turmas, ditados, realização por alunos, correção/classificação, resultados e relatórios; confirme o código e os testes antes de declarar qualquer item como implementado em uma versão específica.
- As decisões aceitas de multi-tenant tratam cada escola como tenant, limitam professor e aluno a uma escola por conta, distinguem Administrador da Escola de Administrador Geral/Técnico e exigem escopo e auditoria para consultas/exportações. A fonte completa é `docs/REFERENCIAL_PROJETO.md`; não duplicar regras sem indicar a fonte.
- `docs/documentacao/diagramas/catalogo-er.md` é um modelo ER proposto e explicitamente marcado como revisão pendente; ele não representa o schema implementado e não deve ser usado como prova de persistência.
- Os arquivos em `docs/relatorio-ciclo-*.md` e `docs/qa-*.md` são evidências de ciclos específicos. Ao documentar o estado atual, registrar a data/ciclo e separar validação daquele momento de comportamento permanente.

### Processo de atualização da documentação

1. Resumir a mudança e indicar a fonte: código, teste, contrato, decisão do usuário, relatório de ciclo ou referência pedagógica.
2. Registrar o status da informação e qualquer conflito com documentos antigos.
3. Atualizar somente as seções e diagramas impactados; manter rastreabilidade entre requisito, implementação, teste e diagrama.
4. Registrar em `docs/documentacao/changelog-documentacao.md` a data, mudança, motivo, arquivos afetados, validações, premissas e pendências.
5. Quando uma decisão de escopo estiver ambígua, consultar primeiro o responsável técnico pertinente e encaminhar a decisão ao usuário com os pareceres separados. Recomendação de agente não autoriza implementação.

### Inventário técnico para documentação

- Runtime/API: `Ditado.API/Program.cs` e controllers em `Ditado.API/Controllers/` (`Usuarios`, `Turmas`, `Ditados`, `Categorias`, `Professores`, `Alunos` e `ErrosCatalogo`).
- Casos de uso e serviços: `Ditado.Aplicacao/Services/` e DTOs em `Ditado.Aplicacao/DTOs/`.
- Domínio: entidades em `Ditado.Dominio/Entidades/` e enums em `Ditado.Dominio/Enums/`.
- Persistência: `Ditado.Infra/Data/DitadoDbContext.cs`, `Ditado.Infra/Configurations/` e `Ditado.Infra/Migrations/`.
- Testes e infraestrutura de testes: `Ditado.Testes/`, especialmente `Infra/CustomWebApplicationFactory.cs`, `TesteIntegracaoBase.cs` e os testes de controllers/services.
- O README registra .NET 9, ASP.NET Core Web API, EF Core 9, MySQL 8, JWT Bearer, Swagger/OpenAPI, xUnit e FluentAssertions. Confirmar versões no projeto antes de citar uma versão como estado de uma entrega específica.

### Estado implementado versus planejamento

- Confirmado pelo inventário do Fullstack: o código atual possui autenticação JWT, usuários, turmas, ditados, categorias, realização/correção, resultados, relatórios e catálogo técnico de erros; os nomes internos `Marketplace*` permanecem por compatibilidade.
- Não declarar como implementados sem nova evidência: entidade/escola/tenant, `EscolaId`, visibilidade Público geral/Instituição/Turmas, autorização server-side multi-tenant, versões imutáveis, sessões revogáveis, auditoria, importação CSV/XLSX, QR/PIN ou presença.
- `docs/epicos/multi-tenant-mt01.md`, `multi-tenant-mt02.md` e `multi-tenant-mt03.md` são contratos/decisões parciais de planejamento, não prova de implementação.
- `docs/documentacao/diagramas/catalogo-er.md` é proposta ER do catálogo e contém elementos ainda não totalmente implementados.
- `docs/epicos/marketplace-cat02-migration.sql` é referência de DDL/histórico da migration CAT-02; não substitui o contrato atual do produto.
- Nomes técnicos legados (`MarketplaceService`, DTOs/namespace `Marketplace`, testes `MarketplaceControllerTests`, rota frontend `/marketplace/publicar`, migration `PublicarErroMarketplace` e arquivos `marketplace-cat02*`) devem ser preservados por compatibilidade até decisão explícita de renomeação. Na documentação de produto, usar **Catálogo de erros**.

### Referências consultadas no Linear, somente leitura

- Projeto Linear **Ditado**: https://linear.app/joaomoutinho/project/ditado-29348d0fee1d.
- `PER-14` registra o refinamento documental do CAT-09, os três níveis de visibilidade, os artefatos esperados em `docs/epicos/` e `docs/diagramas/`, e a regra de não transformar pendências em implementação.
- `PER-26` registra decisões de multi-tenant e também propostas históricas incompatíveis; a seção mais recente/confirmada deve prevalecer e o restante deve ser marcado como legado ou pendente.
- `PER-27`, `PER-28` e `PER-29` devem ser consultadas para MT-01, MT-02 e MT-03; não presumir a existência de MT-04 como issue separada até localizá-la.
- `PER-13` é a referência de planejamento para backup/restauração integral; parâmetros operacionais finais ainda dependem do ambiente universitário.
- `PER-22` é a referência de COR-01; `PER-25` e seus relatórios/QA registram a correção de visualização de ditado.
- A busca Linear e as descrições das issues são fontes de coordenação e evidência, não substituem código, testes ou decisão explícita do usuário. Consultas são somente leitura por padrão neste agente.

### Consultores e rastreabilidade

- Para levantamento técnico, foram consultados o Fullstack 01 (`01a1aeeb-e805-7010-a967-afb23a988599`), o gerente de projetos (`01a1b2b7-20e0-7462-87e9-fc134e0040d2`) e a coordenação/SQUAD (`01a1aa39-f543-7cc1-9825-3bbda26ecd38`). O `researcher` é um subagente consultor do SQUAD, não um ponto focal independente; dúvidas pedagógicas devem ser encaminhadas pela coordenação.
- Pareceres de agentes são evidências auxiliares e recomendações. Não substituem código/testes nem decidem escopo.
- Se relatos locais, Linear e código divergirem — por exemplo, status de CAT-02 — registrar a divergência com as fontes e verificar a issue/status atual antes de atualizar a documentação como fato.
- Não publicar nem copiar `docs/usuarios-de-teste.md`, credenciais ou artefatos privados de visualização. Evidências Playwright em diretórios de visualização devem ser referenciadas apenas quando necessário e sem expor segredos.
