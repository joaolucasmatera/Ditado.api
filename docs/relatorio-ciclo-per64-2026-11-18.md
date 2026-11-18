# PER-64 · Classificação híbrida com provedor simulado

Entrega local de 18/11/2026. [Contrato e critérios de aceite no Linear](https://linear.app/joaomoutinho/issue/PER-64/classificacao-hibrida-ponta-a-ponta-com-provedor-simulado).

- `DitadoService` grava cada lacuna apresentada na mesma transação da tentativa. A resposta digitada é preservada, e `RespostaAusente` distingue campo omitido de texto vazio enviado. Uma lacuna omitida conta como erro. A nota conserva a comparação objetiva e usa duas casas decimais, como o MySQL.
- Para aplicações escolares autorizadas com seleção, a submissão enfileira cada lacuna e congela ID, código, nome, descrição e versão de publicação das definições selecionadas sem executor. A classificação das regras selecionadas e a nota retornam na própria submissão.
- A migration `20261118222910_ClassificacaoHibridaPer64` acrescenta `RespostaAusente` e as tabelas `LlmAnalisesTarefas` e `LlmAnalisesCandidatos`, com chaves únicas por lacuna e candidato. `LlmAnalysisProcessor` utiliza uma reserva recuperável. Após cinco minutos, uma tarefa interrompida pode voltar a ser processada. A validação da saída inteira precede a escrita das sugestões. O resultado registra provedor, modelo, versão do prompt e estado da tarefa, sem sobrescrever os achados por código.
- `GET /api/Alunos/tentativas/{tentativaId}/resultado`, documentado no Swagger, exige identidade do aluno e vínculo ativo na escola registrada na tentativa. `ResultadoAluno.vue` consulta essa rota ao abrir a tela ou recarregá-la, exibindo origens distintas e o estado da análise por lacuna.

## Verificação e reprodução

O build oficial da API passou com zero erros e dois avisos existentes. O build do frontend passou. Os 83 testes pertinentes passaram. Quatro testes legados de exclusão de ditados já falhavam no relatório de referência da COR-01 e ficaram fora desse recorte.

1. Execute `powershell -ExecutionPolicy Bypass -File .\scripts\build-api.ps1` com `UseArtifactsOutput=true` e `ArtifactsPath` apontando para uma saída exclusiva da demonstração. Use o SDK .NET 9 do repositório.
2. Execute `powershell -ExecutionPolicy Bypass -File .\scripts\demo-per64.ps1 -Schema ditado_per64_<sufixo-unico> -ArtifactsPath <saída>`. O script prepara um schema novo no container existente e executa a API local em 5064, com `Llm__Enabled=true`, `Llm__Simulated=true` e ambiente `Testing`. O script aplica as três colunas legadas descobertas no gate anterior somente na fixture, pois suas migrations carecem de metadados de descoberta.
3. No frontend, execute `npm.cmd run build`. Defina `VITE_API_BASE_URL=http://127.0.0.1:5064/api` e suba com `npm.cmd run dev -- --host 127.0.0.1 --port 5194 --strictPort`.
4. No frontend, defina `E2E_API_URL=http://127.0.0.1:5064/api`, `E2E_BASE_URL=http://127.0.0.1:5194`, `E2E_SCHEMA`, `E2E_ARTIFACTS_PATH` e `E2E_RESTART_SCRIPT` com o caminho absoluto de `scripts/demo-per64.ps1` no backend. Execute `node tests/e2e/per64-classificacao.e2e.js`. A fixture interrompe a API de demonstração durante uma tarefa e simula o vencimento da reserva no MySQL para validar a retomada sem esperar cinco minutos.

O Playwright passou com a API em 5064, o frontend em 5194 e o schema isolado `ditado_per64_finalnov18`. A submissão mostrou o achado por código e devolveu nota 33,33 com três lacunas, incluindo um acerto e uma omissão. Após interromper a API durante o processamento e simular o vencimento da reserva, a recarga exibiu a mesma nota, os achados Código e LLM na mesma lacuna, e a análise concluída. Uma conta da outra escola recebeu 404 no endpoint e viu “Resultado indisponível” na tela. O banco confirmou três tarefas concluídas, um candidato correspondente e dois não correspondentes, todos com snapshot de versão 1 e proveniência `Simulado / fixture-v1 / per18-v1`.

As saídas em `work/logs/` são `per64-final-build.log`, `per64-final.trx`, `per64-final-mysql.log` e `per64-final-playwright.log`. O último arquivo inclui o snapshot acessível de texto do resultado. A demonstração usa apenas dados sintéticos e um provedor simulado. A compatibilidade com os provedores reais e a revisão humana ficam nas próximas etapas da PER-18.
