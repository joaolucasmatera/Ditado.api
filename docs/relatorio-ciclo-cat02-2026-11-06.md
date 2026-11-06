# Ciclo CAT-02 / PER-7 — Publicação no Marketplace

Estado: implementação entregue para revisão e QA independente, **In Progress**. Branch dos dois repositórios: `codex/cat-02-publicacao-marketplace`. CAT-09 global permanece aberta. Aprovação humana do formulário: “Ok, gostei”, comunicada pelo SQUAD. Contrato: [marketplace-cat02.md](epicos/marketplace-cat02.md).

## Entrega e motivos

- Backend: `Ditado.Dominio/Entidades/ErroCatalogo.cs` e configuração EF própria preservam separação de categorias genéricas. `DitadoDbContext` registra o novo modelo. A migration `20261106051829_PublicarErroMarketplace`, designer e snapshot criam tabela, índices e FK de autoria com Restrict; não alteram ditados/respostas/enums nem implementam seleção.
- Aplicação: DTOs em `Ditado.Aplicacao/DTOs/Marketplace/` validam obrigatórios, limites técnicos e exemplo em par. Campos desconhecidos são rejeitados, incluindo metadados forjados e configuração de executor/prompt/modelo. `MarketplaceService` normaliza espaços externos, gera código estável único, autoria autenticada/datas UTC/estado ativo e persiste. Sem executor automático/LLM ou efeitos em notas.
- API: `ErrosCatalogoController` oferece POST e GET individual do recibo a Professor/Administrador; serviço registrado em Program. POST retorna201+Location; GET mínimo permite confirmar persistência, sem listagem CAT-01.
- Frontend: `src/views/PublicarErro.vue`, serviço HTTP, rota e menu staff entregam campos vazios, placeholders/labels/helpers aprovados, um exemplo opcional, validação, bloqueio durante envio e confirmação real. Falha preserva preenchimento. Não copia simulação do protótipo.
- Documentação: contrato funcional e SQL em `docs/epicos/`, ligação na proposta CAT-09 e nas histórias originais; roteiro QA separado preparado pelo QA. Alterações prévias de AGENTS/documentação e cópia antiga não rastreada foram preservadas.

## Validação executada

- Build da API passou em saída dedicada, com avisos preexistentes de SDK preview, conflito EF Relational e variável não usada.
- `npm run build` no frontend passou (avisos existentes de API CJS Vite e tamanho de bundle).
- `MarketplaceControllerTests`: **33 aprovados, 0 falhas**, em DLL atual, após build isolado. Cobre staff, recibo/persistência, autoria/estado/datas, obrigatórios nulos/vazios/brancos, opcionais, exemplo incompleto em ambos sentidos, nomes repetidos com IDs/códigos distintos, campos indevidos, limites, aluno403/anônimo401 e GET mínimo.
- Comando efetivo: host `C:\Program Files\dotnet\dotnet.exe` com `DOTNET_CLI_TELEMETRY_OPTOUT=1`, `DOTNET_ROLL_FORWARD=Major`, `vstest C:\Users\lucas\AppData\Local\Temp\ditado-cat02-test-build\Debug\net9.0\Ditado.Testes.dll /TestCaseFilter:FullyQualifiedName~MarketplaceControllerTests /Logger:console;verbosity=minimal`, fora do sandbox para loopback. DLL SHA256 `FF6BA9677BF02D99940CF74F999B30C63496E3ADF96A0E1376C1CD907F345A69`.
- Tentativa inicial dentro do sandbox compilou, mas testhost não conectou em90s; execução anulada não foi contada como aprovação. A execução fora do sandbox resolveu a restrição. Não é validação sob runtime9 nativo.
- EF `has-pending-model-changes` confirmou modelo coerente com migration. [SQL revisado](epicos/marketplace-cat02-migration.sql) contém somente CREATE da nova tabela/índices/FK e INSERT em histórico EF.
- `git diff --check` sem erros.

## Ambiente e preservação

Docker Desktop estava fechado; foi recuperado conforme coordenação. Container existente `ditado-mysql` e volume `ditado-mysql-data` preservados, sem recriação/reset. Migration anterior confirmada antes da aplicação; nova migration aplicada uma única vez via SQL revisado. `ErrosCatalogo` estava vazia antes do QA.

Snapshots privados de todos os registros em Ditados, DitadoSegmentos, RespostasDitados, RespostaSegmentos, RespostaSegmentoTiposErro, DitadoCategorias, TurmaDitados e Categorias foram idênticos antes/depois: SHA256 `29B57CA980D8B409BB0DA02418E33CED468567D44FE75B38DF3ED8369C8FF03C`. Incluem dados protegidos1/8. Primeiro dump falhou por nome incorreto RespostaDitados; corrigido para RespostasDitados antes de aplicar qualquer DDL. Não houve exclusão ou reclassificação.

API iniciada inicialmente a partir do output de testes encerrou por ausência de EFRelational9.0.11 (output escolheu9.0.0). Build dedicado da API resolveu sem alterar dependências dos projetos; DLLRelational9.0.11 conferida. API ativa PID2744, assembly `C:\Users\lucas\AppData\Local\Temp\ditado-cat02-api-final\Debug\net9.0\Ditado.API.dll`, cwd/contentRoot oficial Ditado.API, Development/roll-forward Major. Swagger5031 confirmado com as duas rotas novas, inclusive na retomada com probe terminante. Vite PID31980, frontend5173 HTTP200. PIDs são checkpoints, não autorização para parar sem reinspeção.

Artefatos privados: `C:\Users\lucas\.codex\visualizations\2026\11\06\01a1aeeb-e805-7010-a967-afb23a988599`, snapshots SQL e logs de API/Vite; não publicar dados/credenciais. Nenhum segredo incluído neste relatório.

## Pendências

QA oficial precisa concluir validação visível/API/MySQL conforme [roteiro](qa-cat02-roteiro-2026-11-06.md); seus24 cenários são planejamento, não resultado executado. Não há aprovação funcional independente registrada neste checkpoint nem Done. Nenhum commit/push executado até aqui; arquivos funcionais, testes e documentos estão disponíveis para revisão local. Nenhuma listagem ampla/seleção/edição/arquivamento/importação/LLM entregue nesta história.

## Ajuste de apresentação após parecer UX e autorização humana

Usuário autorizou pacote via Squad ditado 02. Alterado somente PublicarErro.vue no frontend: cabeçalho e espaçamentos conforme telas de cadastro existentes, campos Vuetify outlined/comfortable com labels/helpers e placeholders mantidos; ações do padrão do tema, recibo hierarquizado, código ERR_... removido apenas da renderização e ID como “Número do registro”. Texto do recibo agora é o aprovado: “O cadastro foi salvo. Por enquanto, ele ainda não identifica erros nas respostas automaticamente.” Removida menção técnica de LLM/prazo ao usuário. Script de publicação, validação, guarda de reentrada e payload preservados; nenhum arquivo backend alterado por este ajuste.

Contrato funcional atualizado e roteiro QA recebeu apenas adendo da expectativa visual, sem reescrever evidências anteriores. Build Vue passou (684 módulos, 8,62s; avisos existentes CJS/tamanho de bundle). Diff --check sem erros. QA headed focado aguarda execução/retorno real. Testes backend já aprovados não foram repetidos pois DTO/API/storage continuam iguais. Sem commit/push até coordenação final.
## Revisão final do protótipo UX

Aplicado o protótipo aprovado em `docs/prototipos/cat02-publicacao-recibo.html` sem incorporá-lo como tela de produção. `PublicarErro.vue` agora agrupa o formulário em Dados do erro, Exemplo e Orientação pedagógica, com helpers atualizados, exemplo em duas colunas no desktop e empilhado no mobile. O recibo foi agrupado em Publicação, Exemplo e Orientação pedagógica; exibe Número do registro, oculta ERR_... e usa o texto: “No momento, os erros cadastrados ainda não entram na correção automática.” API, DTO, storage, validações, payload, estados e guarda de envio único permaneceram inalterados.

Build Vue passou (684 módulos, 6,44s; avisos existentes CJS/tamanho de bundle). QA headed focado confirmou grupos, vazios/placeholders, par de exemplo, foco, responsividade mobile e bloqueio do aluno. A única limitação aceita é não ter republicado registro apenas para reabrir o recibo em runtime; os textos exatos permanecem confirmados no componente e nos testes existentes. Sem repetir testes backend, pois não houve mudança nessa camada; sem commit/push até coordenação final.

## Conferência somente leitura no MySQL

Em 06/11/2026, o container existente `ditado-mysql` foi consultado em modo somente leitura, sem criação, edição ou exclusão. `ErrosCatalogo` contém cinco registros (IDs 1–5), todos `Ativo=1`, `AutorId=2` e `DataCriacao=DataAlteracao`. O ID 1 não possui campos opcionais; o ID 2 persiste o par `também`/`tanbém`, observação pedagógica e série `3º ano do Ensino Fundamental`; os IDs 3–5 também persistem os opcionais nulos. Cada registro possui código técnico `ERR_...` único, nome e descrição coerentes com as publicações observadas na UI. A primeira consulta referenciou uma coluna inexistente (`UsuarioAutorId`); após `DESCRIBE`, a leitura foi corrigida para `AutorId`/`DataAlteracao`, sem qualquer alteração no banco. Credenciais e conteúdo desnecessário não foram registrados.
