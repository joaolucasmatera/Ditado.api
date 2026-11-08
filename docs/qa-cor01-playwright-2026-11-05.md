# Validação da COR 01 com Playwright

Validação realizada em 5 de novembro de 2026 com Chromium visível e Playwright instalado no frontend. A implementação atual não atende à cobertura de classificação da tabela 10.6 de `Ditado/docs/ESPECIFICACAO_PG2.md`.

## Execução e dados

Com autorização do usuário, foi criado o ditado separado ID 3, “QA COR01 Playwright 20261105”, com 14 lacunas. A execução completa analisada gerou a RespostaDitado ID 7. O fixture Ditado 1 / RespostaDitado 1 não foi alterado. Uma tentativa anterior também foi submetida durante a execução interrompida pela recarga do ambiente; nenhuma tentativa foi excluída.

Comando reproduzível nesta máquina:

```powershell
node 'C:\Users\lucas\.codex\visualizations\2026\11\05\01a1ad2c-2c50-7280-83ca-1db03679fb20\qa-cor01.cjs'
```

O script reutiliza o ditado ID 3 e cria uma nova tentativa ao ser executado. Conecta-se ao Chromium visível na porta CDP 9223. O áudio do fixture é um WAV silencioso; reprodução de conteúdo falado não foi avaliada.

O teste preencheu os campos da tela `/realizar-ditado/3`, clicou em “Submeter respostas” e leu as 14 linhas da tela `/resultado-aluno`. As classificações da UI corresponderam à resposta real da API. Nenhuma resposta de negócio foi simulada.

## Resultado

Foram executados 14 casos: 4 aprovados, 8 divergências e 2 inconclusivos por decisões de escopo. A comparação automática bruta marcou 4 aprovados e 10 divergentes; a revisão separou os dois casos inconclusivos abaixo para não presumir decisões pendentes.

| Esperado → digitado | Tipo esperado pela tabela | Resultado real | Avaliação |
| --- | --- | --- | --- |
| bolacha → bulacha | Transcrição da fala / escrita fonética | Não classificado | Divergência |
| faca → vaca | Troca de grafemas / surda-sonora | Troca da PRIMEIRA letra | Divergência |
| carro → caro | Regra contextual | Não classificado | Divergência |
| casa → caza | Grafia arbitrária / irregular | Não classificado | Divergência |
| nuvem → nuve | Nasalização | Supressão da ÚLTIMA letra | Divergência |
| prato → pato | Omissão, acréscimo ou substituição e Complexidade silábica | Não classificado | Divergência de classificação múltipla |
| frágil → frágio | Hipercorreção / supergeneralização | Troca da ÚLTIMA letra | Divergência |
| café → cafe | Acentuação | Acentuação | Aprovado |
| embora → em bora | Juntura / segmentação | Não classificado | Divergência |
| chegaram → chegou | Concordância/flexão | Não classificado | Inconclusivo sem contexto de frase |
| Maria → maria | Maiúsculas e pontuação | Acerto e NaoSeAplica | Inconclusivo quanto à classificação separada do resultado objetivo |
| gato → gato | Acerto sem classificação | Acerto e NaoSeAplica | Aprovado |
| cachorro → cachoro | Não classificado no controle conservador anterior | Não classificado | Aprovado apenas como controle da decisão anterior |
| gato → gat | Supressão da ÚLTIMA letra | Supressão da ÚLTIMA letra | Aprovado como controle de regra anterior |

O caso `prato → pato` vem de duas linhas da tabela, permitindo verificar múltiplos tipos com o mesmo par. Não recebeu nenhum. O código atual de `IdentificarTiposErro` também retorna no máximo um tipo, pois encapsula o resultado de `IdentificarTipoErro` em uma lista de um item.

Os exemplos foram reduzidos às palavras das lacunas. Não foi validada a classificação de frases completas. Em especial, não se deve inventar o contexto de concordância. O resultado objetivo atual ignora a diferença entre Maria e maria; corrigir a classificação de maiúsculas exige respeitar a decisão de não alterar esse resultado objetivo. O controle cachorro/cachoro não valida a cobertura de regra contextual; registra apenas a manutenção do comportamento conservador anteriormente combinado.

A nota exibida foi 14,3%, com 2 acertos e 12 erros objetivos. Essa nota não é a taxa de aprovação do QA.

## Rede e proxy

Antes da correção, o login pela UI retornou HTTP 500 em `/api/Usuarios/login`. O Vite apontava `/api` para `https://localhost:7036`, enquanto a API ativa estava em `http://127.0.0.1:5031`. O problema foi comunicado ao chat SQUAD DITADO por solicitação do usuário e encaminhado ao fullstack.

Houve um redirecionamento temporário apenas na sessão Playwright para investigar o ambiente. O fullstack corrigiu o proxy, e a execução final acima removeu a interceptação. Login, carregamento do ditado e submissão retornaram HTTP 200 pelo próprio frontend na porta 5173. Nenhum erro JavaScript não tratado foi capturado nessa execução final; mensagens genéricas de console não foram coletadas integralmente por esse script.

## Evidências

Arquivos no diretório `C:\Users\lucas\.codex\visualizations\2026\11\05\01a1ad2c-2c50-7280-83ca-1db03679fb20`:

- `qa-cor01.cjs`: roteiro executado.
- `qa-fixture.json`: ID e pares de teste.
- `qa-api-resultado.json`: retorno real da submissão final.
- `qa-cor01-resultados.json`: comparação por caso e status de rede.
- `qa-resultado.png`: captura integral da tela final, inspecionada visualmente.
- `qa-respostas.png`: campos preenchidos antes da submissão.
- `qa-aluno.png`: erro de login antes da correção do proxy.

Nenhum código de aplicação foi alterado pelo QA. A história permanece sem aprovação; não foi marcada como Done nem houve alteração no Linear.
