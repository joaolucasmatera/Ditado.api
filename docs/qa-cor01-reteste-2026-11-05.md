# Reteste da COR 01 após revisão das regras

Execução em 5 de novembro de 2026 contra a API atualizada na porta 5031, usando Playwright com Chromium visível e frontend 5173, sem interceptação de rede ou respostas simuladas. Resultado: **35 casos executados, 32 aprovados, 3 divergentes, nenhum bloqueado**. A classificação ainda perde o tipo Maiúsculas e pontuação quando a diferença de caixa está combinada com outra alteração. Não aprovar a entrega como concluída antes do ajuste e reteste.

## Critérios e escopo

Foram aplicadas as decisões mais recentes do usuário: analisar somente a lacuna; diferenças de caixa contam erro objetivo; acumular tipos independentes; separar Omissão, Acréscimo e Substituição sem grupo adicional; admitir padrões delimitados além dos exemplos; manter sem classificação o que não tem regra segura. Transcrição da fala e hipercorreção foram adiadas, portanto não foram exigidas nem aceitas como inferências automáticas neste ciclo. Isso altera as expectativas do relatório histórico, não seus resultados registrados.

## Resultado por caso

Nos 32 casos aprovados, os tipos abaixo corresponderam exatamente aos retornados pela API, exibidos na UI e gravados em RespostaSegmentoTiposErro. Todos são erros objetivos, exceto os seis casos marcados como acerto. Omissão corresponde ao ID29, Acréscimo ao ID30 e Substituição ao ID31; não houve atribuição do tipo agrupado legado a estas respostas novas.

| Esperado → digitado | Tipos recebidos | Resultado QA |
| --- | --- | --- |
| bolacha → bulacha | Substituição | Aprovado |
| faca → vaca | Troca de grafemas / surda-sonora; Substituição | Aprovado |
| carro → caro | Regra contextual; Omissão | Aprovado |
| casa → caza | Grafia arbitrária / irregular; Substituição | Aprovado |
| nuvem → nuve | Nasalização; Omissão | Aprovado |
| prato → pato | Omissão; Complexidade silábica | Aprovado |
| frágil → frágio | Substituição | Aprovado |
| café → cafe | Acentuação | Aprovado |
| embora → em bora | Juntura / segmentação | Aprovado |
| chegaram → chegou | Concordância/flexão | Aprovado para o padrão de radical e sufixos entregue |
| Maria → maria | Maiúsculas e pontuação | Aprovado; erro objetivo |
| gato → gato | Sem tipos; NaoSeAplica | Aprovado; acerto |
| cachorro → cachoro | Regra contextual; Omissão | Aprovado |
| gato → gat | Omissão | Aprovado |
| gato → gto | Omissão | Aprovado |
| gato → gatto | Acréscimo | Aprovado |
| gato → cato | Troca de grafemas / surda-sonora; Substituição | Aprovado |
| carro → carro | Sem tipos; NaoSeAplica | Aprovado; acerto |
| cachorro → cachorro | Sem tipos; NaoSeAplica | Aprovado; acerto |
| carta → cata | Omissão; sem Regra contextual | Aprovado |
| rato → rrato | Acréscimo; sem Regra contextual | Aprovado |
| café → café | Sem tipos; NaoSeAplica | Aprovado; acerto |
| Maria → Maria | Sem tipos; NaoSeAplica | Aprovado; acerto |
| gato → zebra | Sem tipos; NaoClassificado; método nulo | Aprovado; continua erro objetivo |
| embora → embora | Sem tipos; NaoSeAplica | Aprovado; acerto |
| homem → omem | Grafia arbitrária / irregular; Omissão | Aprovado |
| comprar → compar | Omissão; Complexidade silábica | Aprovado |
| pão → pao | Nasalização; Acentuação | Aprovado |
| caneta → canita | Substituição; sem Transcrição da fala | Aprovado |
| fácil → fácio | Substituição; sem Hipercorreção | Aprovado |
| gato → gato! | Maiúsculas e pontuação | Aprovado; erro objetivo |
| gato → gtao | Sem tipos; NaoClassificado; método nulo | Aprovado; continua erro objetivo |
| café → Cafe | Somente Acentuação; falta Maiúsculas e pontuação | Divergente |
| casa → Caza | Grafia arbitrária / irregular; Substituição; falta Maiúsculas e pontuação | Divergente |
| prato → Pato | Omissão; Complexidade silábica; falta Maiúsculas e pontuação | Divergente |

Nos três casos divergentes, a nota e o resultado objetivo estão corretos; a lista de tipos está incompleta. A UI reproduz fielmente a API e os registros persistidos, portanto o defeito não é somente visual. A condição MesmoTextoSemCaixaOuPontuacao no classificador só reconhece a apresentação quando o restante do texto já coincide; esse é o ponto provável para investigação pelo fullstack. O requisito de acumular regras independentes já havia sido decidido.

## Notas e persistência

| Bateria | Ditado | Resposta | Acertos e erros | Nota API | Nota persistida | Nota na tela |
| --- | --- | --- | --- | --- | --- | --- |
| Original revisada, 14 casos | 3 | 8 | 1 acerto e 13 erros | 7,142857% | 7,14% | 7,1% |
| Limites e operações, 18 casos | 4 | 9 | 5 acertos e 13 erros | 27,777778% | 27,78% | 27,8% |
| Combinações, 3 casos | 5 | 10 | 0 acertos e 3 erros | 0% | 0% | 0,0% |

Essas notas são as notas dos ditados, não taxas de aprovação do QA. Conferidos diretamente no MySQL os IDs dos tipos, os valores esperado e digitado, resultado objetivo, estado e método de classificação. Estados e métodos do banco corresponderam aos da API em todos os casos. O campo singular legado ficou nulo nas respostas novas consultadas. Nenhuma duplicata foi encontrada nas listas retornadas.

Snapshots antes e depois foram idênticos para Ditado1, hash de seu áudio, segmentos, respostas e associações de tipos. A RespostaDitado1 preservou cachoro/cachorro como não classificado e gato/gato como acerto. Nenhum registro histórico foi reclassificado para fazer o teste passar. As novas gravações ocorreram somente em ditados separados autorizados.

## Testes automatizados e rede

Comando executado independentemente pelo QA no backend:

```powershell
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
dotnet test Ditado.Testes/Ditado.Testes.csproj --no-restore --filter 'FullyQualifiedName~DitadoServiceTests_TiposErro|FullyQualifiedName~DitadosControllerTests|FullyQualifiedName~ProfessorServiceTests' --logger 'console;verbosity=minimal' -v quiet
```

Resultado: 59 aprovados, 0 falhas, 0 ignorados. Avisos de compilação incluíram conflito de versões EF Core Relational 9.0.0/9.0.11, ocultação de membros em testes e variável não usada; não impediram a execução. Os três casos de composição adicionais mostram uma lacuna apesar do sucesso dessa suíte.

Todos os logins, carregamentos e submissões observados pelo frontend retornaram HTTP200. As criações auxiliares de fixtures ocorreram pela API real. Não houve pageerror nem console.error capturado. Os únicos warnings coletados foram ausência de token antes do login, esperada nesse estado. O proxy corrigido foi usado sem route, fulfill ou outro redirecionamento na sessão.

## Reprodução e evidências

Diretório base: `C:\Users\lucas\.codex\visualizations\2026\11\05\01a1ad2c-2c50-7280-83ca-1db03679fb20`.

```powershell
node 'C:\Users\lucas\.codex\visualizations\2026\11\05\01a1ad2c-2c50-7280-83ca-1db03679fb20\qa-cor01-reteste.cjs'
node 'C:\Users\lucas\.codex\visualizations\2026\11\05\01a1ad2c-2c50-7280-83ca-1db03679fb20\qa-cor01-reteste.cjs' --combinacoes
```

Cada execução cria um fixture adicional e novas tentativas. A execução sem argumento reutiliza também Ditado3. Não rodar enquanto o coordenador prepara reinício da API. As pastas desta execução devem ser preservadas antes de repetir, pois os nomes de saída do roteiro são fixos.

Na pasta `reteste-20261105`: `original-resultado.png` e `limites-resultado.png` foram inspecionadas visualmente; arquivos `*-api.json`, `*-persistencia.json` e `relatorio.json` guardam os detalhes por caso. Na pasta `reteste-combinacoes-20261105`: `combinacoes-resultado.png` foi inspecionada visualmente e documenta a falha; JSONs equivalentes guardam API, banco e rede. Cada pasta inclui `fixture-antes.txt` e `fixture-depois.txt` idênticos. O roteiro lançou Chromium com headless false e capturou as telas após submissão real; a sessão automatizada foi encerrada ao concluir.

O SQUAD DITADO foi informado dos 32 aprovados e 3 divergentes e encaminhou a correção ao fullstack. Novas submissões estão suspensas enquanto se prepara o próximo assembly. Este ciclo não alterou código de aplicação, não marcou a história como Done e não atualizou o Linear.
