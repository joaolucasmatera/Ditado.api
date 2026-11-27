# Relatório de ciclo — PER-69

Em 22/11/2026, o piloto local foi implementado na branch `codex/per-69-free-llm-router`. O objetivo foi concentrar credenciais, seleção e rotação dos modelos no Free-LLM Router e preservar a classificação híbrida da API. [Issue PER-69](https://linear.app/joaomoutinho/issue/PER-69/migrar-a-rotacao-da-correcao-llm-para-o-free-llm-router)

| Componente | Arquivos | Alteração |
| --- | --- | --- |
| Opções, cliente e worker | `Ditado.API/Llm/LlmProviderClient.cs`, `Ditado.API/Program.cs`, `Ditado.Aplicacao/Llm/` | Uma requisição ao gateway, validação de identidade efetiva e permanência da validação v4. |
| Persistência | `LlmTentativaProvedor.cs`, snapshot EF e migration `20261122120000_RegistrarTentativasRouterPer69` | Contagem anulável de tentativas do Router junto ao provedor, modelo, versão, status e desfecho. |
| Piloto local | `docker-compose.per69.yml`, `scripts/per69/`, `scripts/build-api.ps1` | Ambiente segregado, revisão fixa, volume do Router, catálogo local, SDK .NET 9 já instalado e build com saída isolada. |
| Testes | `LlmProviderClientTests.cs`, `LlmAnalysisProcessorTests.cs`, `Per69ControllerTests.cs` | Contrato do Router, fallback exclusivamente no gateway, aceitação de alternativa, releitura, inconclusivo e falha de rede. |
| Documentação | Contrato de piloto, evidências, relatório vivo, referencial e changelog | Trilha de rodadas, parâmetros, gabaritos e limites da validação. |

## Validação

- Build local com SDK 9.0.320 e os avisos preexistentes de EF Core Relational e variável sem uso.
- Suíte pertinente de LLM, COR-01 e PER-69 com 66 aprovados e 1 caso de comparação externa ignorado. Swagger recompilado devolveu HTTP 200 e documentou as respostas 200 da submissão e da releitura.
- Suite upstream do Router, 25/25. A rodada controlada reproduziu 429 → 400 → 200, com troca de provedor e remoção de `response_format` no último salto.
- TestServer com Router real e Groq autenticado, resultado HTTP 200, nota 33,33, regra de acentuação, definição docente por LLM e duas releituras iguais. A simulação de falha de rede preservou a nota e a regra. A simulação de inconclusivo deixou apenas a lacuna correspondente em revisão.
- Sonda real do Router com oito lacunas. Quatro achados esperados, zero falsos positivos, zero omissões, um inconclusivo esperado e zero timeouts.
- Compose validado estruturalmente. A construção foi interrompida por erro de I/O do Engine. A repetição esbarrou na indisponibilidade do Docker Desktop. Após a recuperação do Engine 29.9.2, a repetição no Compose isolado passou com MySQL 8.0.21, migration aplicada, Swagger HTTP 200 e persistência do modelo efetivo. Com o Router parado, a releitura preservou a nota 20 e a regra por código. O QA independente segue pendente.
- A ferramenta do EF confirmou o snapshot e gerou o SQL da coluna anulável. O MySQL nativo falhou inicialmente em C:, que reportou zero bytes livres. Um MySQL 26.10.0 isolado em D: aplicou a migration, e a API Kestrel demonstrou submissão, nota 20, regra por código, achado LLM, releitura idêntica, contingência com o Router parado e persistência relacional do modelo efetivo.
- A suíte ampla terminou com 317 aprovados, 21 falhas e 1 ignorado. A comparação nominal com `e4b45ba56393c1099bbb4d10a091e99b61511075`, compilada em cópia isolada, encontrou as mesmas 21 falhas. A base anterior teve 305 aprovados e 1 ignorado.

[Contrato operacional](epicos/per69-piloto-free-llm-router.md) · [Evidências](documentacao/evidencias/per69-piloto-router-2026-11-22.md)
