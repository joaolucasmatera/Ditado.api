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
