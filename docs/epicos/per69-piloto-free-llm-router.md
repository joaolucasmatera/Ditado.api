# PER-69 — Piloto local do Free-LLM Router

Em 23/11/2026, dez chamadas sintéticas diretas ao Router na porta 4270 retornaram o objeto `segmentos`. Os 23 itens retornados incluíram `segmentoId`, `candidatosCorrespondentes` e `inconclusivo`. O Cloudflare Workers AI respondeu com GPT-OSS 120B nas dez chamadas. Oito lotes coincidiram integralmente com o gabarito. Houve um falso positivo de concordância e um inconclusivo esperado que o modelo não sinalizou. [Pedidos, gabaritos e respostas](../documentacao/evidencias/per69-dez-sondas-router-2026-11-23.md)

> Em 23/11/2026, o contrato por segmento `per69-v5-segmentos` substituiu o payload v4 no caminho operacional. A configuração ativa exige o Router ou a simulação de teste. Este documento mantém o histórico das rodadas v4. [Contrato por segmento e evidências](../documentacao/evidencias/per69-contrato-segmentos-2026-11-23.md)

Piloto opt-in em `codex/per-69-free-llm-router`, com dados sintéticos e revisão `baf4e21c7b39692e143a85a307eb62490aa7bf6f`. O trabalho preserva os ensaios e as tentativas históricas da PER-65. [PER-69](https://linear.app/joaomoutinho/issue/PER-69/migrar-a-rotacao-da-correcao-llm-para-o-free-llm-router) · [Avaliação anterior](per65-avaliacao-free-llm-router.md)

## Contrato em uso no piloto v5

- O worker envia `ditado`, `errosCandidatos` e `respostas` em uma chamada por processamento da tentativa com `model=auto` e `response_format=json_object`. O texto com marcações preserva o contexto, enquanto as definições congeladas são compartilhadas por todas as lacunas.
- A resposta contém `segmentos` com cada ID solicitado exatamente uma vez, uma lista de IDs dos candidatos correspondentes e sinalização opcional de inconclusivo. A API valida o contrato, preserva achados confirmados em lacunas inconclusivas e guarda o modelo efetivo relatado pelo Router.
- O teste híbrido com Router real na porta 4270 aplicou os cinco erros cadastrados, preservou os achados de Acentuação por código e confirmou a releitura com nota de 14,29. O falso positivo contextual das sondas diretas permanece uma limitação pedagógica. [Processo completo, teste e capturas](../documentacao/evidencias/per69-processo-e-validacao-2026-11-23.md)

Para executar a API no mesmo host que o Router configurado, injete `Llm__Enabled=true`, `Llm__RouterUrl=http://127.0.0.1:4270/v1` e `DITADO_LLM_ROUTER_KEY` a partir da configuração local. Em Docker, use o endereço acessível a partir do contêiner da API. Em uma rede Compose compartilhada, use o nome do serviço Router. O Compose principal lê `LLM_ENABLED`, `LLM_ROUTER_URL` e `DITADO_LLM_ROUTER_KEY` e mapeia para as opções da API.

## Histórico do piloto v4

- Com `Llm__RouterUrl` configurado, o worker envia um POST compatível com OpenAI por processamento de tentativa, com `model=auto`, payload `per18-v4-contextual` e `response_format=json_schema`. Rotações e repetições de compatibilidade acontecem no Router. A API não aciona a cadeia direta diante de falha no Router.
- O backend compara os cabeçalhos `x-free-llm-provider` e `x-free-llm-model` com `model` do envelope, valida término da geração, JSON, cobertura integral das lacunas e grupos dos candidatos. A identificação corresponde ao destino que respondeu segundo o Router. Nesta revisão o envelope e os cabeçalhos usam a mesma origem de metadados. Eles não atestam um subprovedor interno do OpenRouter.
- Uma trilha por requisição da API guarda provedor/modelo efetivos quando identificados, versão do prompt, status, desfecho, motivo e `TentativasRouter` quando informado. O Router entrega a contagem no sucesso, incluindo repetições no mesmo modelo. A API não lê nem persiste mensagens de erro livres que possam conter conteúdo do provedor. O histórico detalhado de cada salto foi capturado no ensaio com simuladores. Em falha total, a API retém o status e o motivo que pôde classificar.
- JSON malformado, truncamento, cobertura incompleta e identificação ausente ou inconsistente deixam o lote para revisão sem aplicar achados. Um inconclusivo válido deixa somente a lacuna correspondente em revisão. Falha técnica deixa o estado LLM em `Falha`, com a correção objetiva e as classificações por código disponíveis.
- A migration `20261122120000_RegistrarTentativasRouterPer69` acrescenta uma coluna anulável. Linhas da PER-65 e da configuração direta não exigem preenchimento retroativo.

### Execução isolada das rodadas v4

O [Compose de piloto](../../docker-compose.per69.yml) usa uma rede interna para API, Router e MySQL. O Router tem uma rede de saída e um volume privado. A API publica a porta 5069 somente no loopback. MySQL e Router não publicam portas. Use um projeto Compose exclusivo e um banco vazio dedicado. O perfil prefere Groq `openai/gpt-oss-120b`, Cohere `command-a-plus-05-2026` e OpenRouter `particle-ai/particle-1:free`, e permite outros modelos gratuitos do catálogo fixado nessas contas. OpenRouter aceita apenas entradas terminadas em `:free`. O catálogo e o arquivo de configuração local do piloto desativam convidados, cache e registro de corpos.

Injete `PER69_DB_PASSWORD`, `PER69_DB_ROOT_PASSWORD`, `PER69_JWT_SECRET` e `PER69_ROUTER_KEY` de uma fonte privada. Repita os mesmos valores ao reutilizar o volume MySQL. Adicione credenciais gratuitas `GROQ_API_KEY`, `COHERE_API_KEY` e/ou `OPENROUTER_API_KEY` ao processo do Compose. Elas chegam somente ao Router no perfil de piloto. A API recebe apenas a credencial de gateway para LLM.

```powershell
docker compose -p ditado-per69 -f docker-compose.per69.yml config --quiet
docker compose -p ditado-per69 -f docker-compose.per69.yml up -d --build
node scripts/per69/validar-ditado.cjs disponivel
docker compose -p ditado-per69 -f docker-compose.per69.yml stop router
node scripts/per69/validar-ditado.cjs indisponivel
docker compose -p ditado-per69 -f docker-compose.per69.yml stop
```

Em host Windows, o Router pode ser executado com Node.js 24.11.1 em diretório privado novo e revisão original limpa. Use `FREE_LLM_HOME` dedicado, `FREE_LLM_API_KEY` efêmera, `FREE_LLM_HOST=127.0.0.1` e `FREE_LLM_PORT=4269`. Comece com `node scripts/per69/iniciar-router.mjs <checkout>/router`. Para testes de API em TestServer, use a mesma credencial em `DITADO_LLM_ROUTER_KEY` e `DITADO_PER69_ROUTER_REAL=1`. Limpe as chaves dos provedores da sessão que inicia a API. O teste padrão usa respostas simuladas e dispensa chaves.

### Conferência da trilha no Compose

Depois dos dois cenários sintéticos e antes de parar o MySQL, execute a consulta de leitura abaixo. Ela usa a credencial já injetada no container e devolve somente os campos necessários para a conferência.

```powershell
@'
SELECT r.RespostaDitadoId, t.Provedor, t.Modelo, t.TentativasRouter,
       t.PromptVersao, t.Desfecho, t.HttpStatus, t.Motivo
FROM LlmTentativasProvedor AS t
JOIN LlmAnalisesTarefas AS a ON a.Id = t.LlmAnaliseTarefaId
JOIN RespostaSegmentos AS r ON r.Id = a.RespostaSegmentoId
ORDER BY r.RespostaDitadoId, t.Numero;
'@ | docker compose -p ditado-per69 -f docker-compose.per69.yml exec -T mysql sh -c 'MYSQL_PWD="$MYSQL_PASSWORD" mysql --batch -u"$MYSQL_USER" "$MYSQL_DATABASE"'
```

Use os IDs retornados pelo script para identificar os dois lotes. Com o Router ativo, confira o provedor e modelo efetivos, o prompt v4 e o resultado da análise. Com o Router parado, a trilha deve identificar o destino solicitado `FreeLlmRouter/auto`, com motivo `Rede` e sem status HTTP externo ou contagem de tentativas do gateway. Compare com a consulta relida no piloto nativo. [Rodadas E e F](../documentacao/evidencias/per69-piloto-router-2026-11-22.md#e-e-f--api-local-em-kestrel-e-armazenamento-relacional-mysql)

### Validações locais

```powershell
node --test <checkout>/router/test/router.test.js
node scripts/per69/ensaio-router.mjs <checkout> scripts/per69/lote-v4-sintetico.json
powershell -ExecutionPolicy Bypass -File .\scripts\build-api.ps1 -ArtifactsPath "$env:TEMP\ditado-per69-artifacts"
dotnet test Ditado.Testes/Ditado.Testes.csproj --filter 'FullyQualifiedName~Llm|FullyQualifiedName~Cor01|FullyQualifiedName~Per69'
```

Para retorno temporário ao caminho direto, encerre os serviços do piloto. Na configuração anterior, deixe `Llm__RouterUrl` ausente e restaure as opções da cadeia direta e as credenciais na configuração segura correspondente. Os caminhos são mutuamente exclusivos. A coluna de trilha pode permanecer no banco.

O uso com dados escolares depende de autorização para os dados e o ambiente, liberação do perfil de provedores e regras de retenção, além do gate técnico e QA. Os resultados pedagógicos das rodadas usam o critério de zero falso positivo nos controles negativos e registram separadamente omissões, inconclusivos e timeouts. [Evidências das rodadas](../documentacao/evidencias/per69-piloto-router-2026-11-22.md)
