# Relatório do ciclo PER-25 — 6 nov 2026

## Contrato restaurado

O modal existente na biblioteca já chama `GET /api/Ditados/{id}/visualizar` e espera `titulo`, `descricao`, `categorias`, `audioBase64` e `segmentos` com `ordem`, `tipo` e `conteudo`. A visualização de staff precisa incluir o gabarito nas lacunas. O contrato `/realizar` continua separado e oculta o conteúdo esperado para alunos.

## Alterações

* `Ditado.Aplicacao/DTOs/DitadoCompletoDto.cs` — DTO completo compatível com o modal e DTO de segmentos ordenados.
* `Ditado.Aplicacao/Services/DitadoService.cs` — consulta apenas ditados ativos, inclui autor/categorias/segmentos e converte áudio para data URI; não adiciona filtro de propriedade novo.
* `Ditado.API/Controllers/DitadosController.cs` — rota `GET {id}/visualizar`, restrita a Administrador e Professor; respostas 401/403/404 documentadas.
* `Ditado.Testes/Controllers/DitadosVisualizacaoControllerTests.cs` — integração cobrindo retorno completo para admin, acesso de professor, bloqueio de aluno, usuário anônimo e ID inexistente.

O frontend não precisou mudar: já chama a rota correta e reconstrói o texto com colchetes a partir dos segmentos. Não houve alteração de esquema ou de dados. Os ditados reais 1 e 8 foram preservados.

## Validação e ambiente

* `dotnet test Ditado.Testes\Ditado.Testes.csproj --no-restore --disable-build-servers -m:1 --filter 'FullyQualifiedName~DitadosVisualizacaoControllerTests|FullyQualifiedName~DitadosControllerTests' -p:BaseOutputPath='C:\Users\lucas\AppData\Local\Temp\ditado-per25-test-build\'` — **20/20 passaram** (5 novos + 15 do controller).
* `dotnet build Ditado.API\Ditado.API.csproj --no-restore --disable-build-servers -m:1 -p:BaseOutputPath='C:\Users\lucas\AppData\Local\Temp\ditado-per25-api-build\'` — passou, com warnings preexistentes de conflito EF Core e variável `ex` não usada.
* API ativa em `http://127.0.0.1:5031`, PID 40276; assembly `C:\Users\lucas\AppData\Local\Temp\ditado-per25-api-build\Debug\net9.0\Ditado.API.dll`; diretório de trabalho/conteúdo oficial `Ditado.API`. Swagger JSON respondeu HTTP 200 e documenta `/api/Ditados/{id}/visualizar`.
* Ambiente iniciado com `DOTNET_ROLL_FORWARD=Major`, pois o host não tem runtime .NET 9 instalado. Vite e MySQL foram mantidos ativos.
* QA headed final: **15/15 checks passaram** e a suíte atual de controller passou **20/20** em execução independente. Testou professor/admin nos quatro botões (lupa/olho), modal com gabarito e áudio, aluno 403, anônimo 401, inexistente 404 e realização do aluno 200 com lacunas ocultas. Autor, categorias e hash do áudio coincidiram com o banco; snapshots dos dados/segmentos de Ditado 1 e Ditado humano 8 foram idênticos antes/depois. Sem erros no console/browser. Evidência detalhada em [qa-per25-2026-11-06.md](qa-per25-2026-11-06.md).

## Conclusão

QA aprovou a PER-25 e moveu a issue para **Done** conforme autorização explícita do usuário. Esta conclusão cobre somente o bug de visualização; PER-22/COR-01 e a dependência CAT-05 não foram alteradas neste ciclo.
