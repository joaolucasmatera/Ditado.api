# PER-69 · Diagnóstico local do Free-LLM Router

O cliente HTTP emite um evento por chamada ao Router quando o opt-in está ativo em um contêiner no ambiente Development. O evento inclui o identificador da chamada e, quando o worker estiver presente, os IDs da resposta e da tarefa e o número da tentativa. O log registra o status HTTP, o tempo decorrido, a identidade efetiva validada, `x-free-llm-attempts`, a versão do prompt, o desfecho e o motivo do cliente.

O evento inclui os JSONs completos da requisição e da resposta, após a retirada de propriedades sensíveis e o mascaramento de valores reconhecidos como credenciais ou dados pessoais. O filtro percorre objetos, listas e conteúdo JSON serializado em campos de texto. Preserva campos como `usage`, metadados do Router e corpos JSON de erros HTTP. Se o corpo estiver indisponível, malformado ou acima do limite de leitura de 100.000 bytes, o log usa `null` para esse corpo. A trilha no banco segue com os metadados existentes.

O filtro protege as credenciais configuradas, nomes de campos sensíveis e formatos reconhecíveis. Um valor desconhecido e sem identificação em texto livre pode exigir revisão antes de compartilhamento.

## Ativação local

Use um banco local já migrado com dados sintéticos e um Router de teste. Acrescente as duas opções abaixo ao arquivo local passado ao Compose.

```dotenv
ASPNETCORE_ENVIRONMENT=Development
LLM_ROUTER_LOG_BODIES=true
```

```powershell
docker compose --env-file .env.docker up -d --build --no-deps api
docker logs --since 5m ditado-api 2>&1 | Select-String -SimpleMatch 'FreeLLM chamada'
```

Recriando a API local com `LLM_ROUTER_LOG_BODIES=false`, o diagnóstico volta a ficar desligado. Em Production, a configuração é bloqueada mesmo quando o valor é `true`.

## Primeira validação isolada

O roteiro `work/per69-validar-observabilidade.ps1` usa uma publicação da API, a porta 5069, um banco MySQL descartável e uma resposta sintética compatível com o Router. Ele confere a submissão, a releitura, os metadados e a ausência das credenciais sintéticas na saída. Ele intencionalmente inclui uma credencial ecoada e um campo livre do upstream na resposta da fixture.

A API compilou com zero erros e dois avisos preexistentes. Os 73 testes selecionados de LLM, PER-69 e COR-01 passaram, com a comparação externa fora do filtro. O teste focal cobriu opt-in desligado, resposta bem-sucedida, corpo protegido e HTTP 503. O Compose validou o padrão `false` e o repasse do valor `true`. Evidências locais em `work/logs/per69-obs-build.log` e `work/logs/per69-obs.trx`.

O cliente e o filtro estão em `Ditado.API/Llm/LlmProviderClient.cs` e `LlmRouterDiagnostics.cs`. O worker e os contratos internos da chamada estão em `Ditado.Aplicacao/Llm/`. A restrição de ambiente fica em `Ditado.API/Program.cs` e o opt-in é repassado por `docker-compose.yml` e documentado em `.env.docker.example`. Os testes foram ampliados em `Ditado.Testes/Services/LlmProviderClientTests.cs`.

As duas submissões em Docker, com o opt-in ligado nos dois ambientes, obtiveram nota 20 e releitura idêntica. A análise manteve o achado por código e a classificação do Router simulado. Em Production, o console registrou zero eventos de diagnóstico. Em Development, registrou um evento que continha o payload e o conteúdo validado da resposta. A varredura de ambos os logs confirmou a ausência das cinco credenciais sintéticas, incluindo a chave ecoada pelo servidor de teste. O Swagger respondeu HTTP 200 nos dois contêineres.

```text
FreeLLM chamada 0e695b9f-1105-4c4b-8eb4-f5f02602e64b RespostaDitadoId=2 LlmAnaliseTarefaId=6 Tentativa=1 HTTP=200 LatenciaMs=99 Provedor=fixture Modelo=modelo-sintetico x-free-llm-attempts=2 PromptVersao=per69-v5-segmentos Desfecho=Sucesso Motivo=(null)
```

O trecho acima veio de `docker logs ditado-per69-obs-8c669922-development`. A linha completa e o resumo estão em `work/logs/per69-obs-docker-evento.log` e `work/logs/per69-obs-docker.log`. O teste usou contêineres próprios e os encerrou após as verificações.

## Revisão da exportação JSON

O build passou com zero erros e 74 testes pertinentes aprovados. Os casos de regressão verificaram corpo HTTP 503 em JSON, saída não JSON, metadados em objetos aninhados e remoção de credenciais em conteúdo serializado. Resultados em `work/logs/per69-json.trx`.

A rodada final na porta 5079 usou o Router simulado, com HTTP 200, nota 20 e releitura idêntica. O opt-in gerou um evento em Development e zero em Production. A exportação contém a requisição de cinco lacunas, a resposta com os cinco segmentos, o objeto `usage` com 48 tokens sintéticos e a rota de teste. As credenciais sintéticas, o campo de autorização e a lista de cabeçalhos ecoados ficaram fora da saída.

```powershell
docker logs ditado-per69-obs-beaafb61-development 2>&1 | Select-String -SimpleMatch 'FreeLLM chamada'
```

Arquivo completo da rodada em `work/logs/per69-json-final-exportacao.json`. O resumo da execução fica em `work/logs/per69-json-final-docker.log`.
