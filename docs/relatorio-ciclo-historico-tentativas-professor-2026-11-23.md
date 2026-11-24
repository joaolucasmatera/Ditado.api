# Histórico de tentativas na tela de resultados do professor

Na tela `/turmas/:turmaId/ditados/:ditadoId/resultados`, o professor abre o aluno e escolhe uma tentativa por data e nota. O modal abre na mais recente. Cada lacuna mostra a resposta esperada, o texto escrito, os acertos e as classificações por Código e LLM. Os estados da análise aparecem por lacuna, inclusive pendência, revisão e falha. O resumo continua usando a melhor nota de cada aluno, com desempate pela entrega mais antiga e pelo ID.

O novo GET em `/api/Professores/turmas/{turmaId}/alunos/{alunoId}/ditados/{ditadoId}/tentativas` usa a mesma leitura do resultado do aluno e devolve a data e o resultado já persistidos. O professor precisa ser responsável pela turma e ter vínculo ativo quando a turma tem escola. A aplicação e a participação identificam tentativas atuais e preservam o acesso ao histórico de participações revogadas. Uma resposta legada sem ID de aplicação entra quando existe vínculo do aluno com a turma e a data e a escola permitem identificar uma única atribuição legada. Registros ambíguos ficam fora da consulta. O endpoint devolve HTTP 404 para histórico indisponível e só projeta os dados pedagógicos do resultado.

## Arquivos

- Backend em `ProfessoresController.cs`, `ProfessorService.cs`, `AlunoService.cs`, `ResultadoTentativa.cs` e `TentativaAlunoProfessorDto.cs`.
- Frontend em `ResultadoDitado.vue`, `ResultadoAluno.vue`, `resultadosDitado.js` e `ditadoService.js`.
- Testes em `ProfessorHistoricoControllerTests.cs`, `ProfessorServiceAplicacaoTests.cs`, `Cor01ControllerTests.cs` e `tests/e2e/historico-professor.e2e.js`.

## Validação

Os builds do backend e do frontend passaram. A suíte focada teve 43 aprovações, incluindo Swagger, histórico legado, separação entre turmas com o mesmo ditado e HTTP 404 para professor de outra escola. O teste de integração também verificou a melhor nota no resumo, a resposta 403 para aluno e a resposta 401 sem sessão.

Na seleção ampliada, 62 testes passaram e 12 falharam. As mesmas 12 falhas foram reproduzidas por nome em cópia isolada da base `db8031f`. Consulte `work/logs/historico-professor-amplo-final.trx` e `work/logs/historico-professor-base.trx`.

O teste Playwright usou API na porta 5065, frontend na 5196 e schema MySQL sintético `ditado_per65_histnov23`. O aluno entregou uma primeira tentativa de 100% e outra de 50%. O professor viu 100% no resumo, abriu a de 50% com as classificações de Código e LLM e selecionou a de 100% no mesmo modal. A consulta por um professor de outra escola recebeu HTTP 404. A LLM da demonstração foi simulada.

Os registros locais estão em `work/logs/historico-professor-build.log`, `work/logs/historico-professor-foco.trx` e `work/logs/historico-professor-playwright.log`. O navegador pode repetir o fluxo com `node tests/e2e/historico-professor.e2e.js` usando `E2E_API_URL` e `E2E_BASE_URL` de um ambiente sintético.

## Correção do quadro Erros por Tipo e publicação local

O diagnóstico inicial encontrou no frontend ativo o modal anterior e uma API sem a rota de histórico. No MySQL local, a tentativa 4 já tinha nota 66,67 e o achado LLM de concordância verbal na lacuna de “chegaram”. A inspeção foi somente leitura.

O quadro usa os achados persistidos da melhor tentativa de cada aluno. O agrupamento distingue Código e LLM. Um registro legado e um achado de Código do mesmo executor contam uma só vez por lacuna. Um achado contextual da LLM pode aparecer mesmo quando a resposta da lacuna coincide com o gabarito. As lacunas incorretas sem achado exibem contadores de pendência ou ausência de classificação. O quadro também diferencia uma turma sem entregas.

O mesmo mapeamento de Código e LLM atende o detalhe e o quadro. A API acrescenta origem e ID de catálogo ao DTO de agrupamento e documenta os novos contadores no Swagger.

O build da API, os builds das imagens Docker e os 43 testes focados passaram. No Playwright com MySQL sintético, o resumo exibiu a melhor nota, o quadro e o detalhe exibiram Código e LLM, a lacuna incorreta sem achado teve contador próprio, o modal alternou entre duas tentativas e o acesso de outra escola recebeu HTTP 404. Evidências em `work/logs/historico-quadro.trx` e `work/logs/historico-quadro-playwright.log`.

Foram publicadas as imagens `ditado-backend1-api:historico-quadro-nov23` e `ditado-backend1-frontend:historico-quadro-nov23`. A validação de publicação encontrou a rota no Swagger e o chunk `ResultadoDitado-DPYyuYLP.js` no endereço ativo. A configuração da API foi conferida antes e depois, incluindo o schema `ditado_local_20261119` e a configuração de sessão. O banco manteve os sete registros de sessão e as cinco tentativas da aplicação do ditado 2 à turma 2. Evidência em `work/logs/historico-quadro-publicacao.log`.

O serviço atualizado leu o schema ativo em modo somente leitura. O histórico retornou 6, 5, 4, 3 e 2. O resumo manteve a melhor nota de 66,67 e contou uma ocorrência de concordância entre sujeito e verbo escritos, origem LLM, na tentativa 4. A lacuna “chegaram” estava incorreta e com análise concluída. A rota pelo proxy do frontend devolveu HTTP 401 sem credenciais. A aba de conferência abriu o login. A demonstração autenticada no navegador usou o ambiente sintético, e a conferência da tentativa real usou o serviço em leitura. Evidência em `work/logs/historico-quadro-real.log`.
