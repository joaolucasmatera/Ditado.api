# Roteiro do próximo ciclo da COR 01

Preparado em 5 de novembro de 2026 após as decisões do usuário encaminhadas pelo SQUAD DITADO. Este roteiro não é resultado de execução. Aguardar a entrega do fullstack antes de retestar. O relatório e as evidências do ciclo anterior permanecem como histórico.

## Critérios atualizados

Analisar somente o conteúdo da lacuna. Não inferir sujeito, frase ou contexto externo. Concordância e flexão só podem ser classificadas quando uma regra delimitada e testada sustentar a identificação pelo par esperado e digitado. Na ausência dessa regra, exigir estado NaoClassificado, lista vazia e método nulo.

Maria → maria deve contar como erro objetivo e receber Maiúsculas e pontuação. Essa decisão substitui deliberadamente a normalização anterior para maiúsculas. Na bateria original de 14 lacunas, passa a haver somente um acerto objetivo, gato → gato: nota esperada 100/14, aproximadamente 7,14%, e 13 erros, independentemente de quantos tipos cada resposta receba.

Padrões delimitados além dos exemplos devem ter positivos e negativos. O padrão r/rr entre vogais deve abranger carro → caro e cachorro → cachoro como Regra contextual. O antigo controle de não classificação de cachorro → cachoro fica substituído; não editar a resposta histórica do fixture para obter o novo resultado.

Decisão posterior explícita do usuário, encaminhada pelo SQUAD DITADO: manter a solução simples e adiar transcrição da fala e hipercorreção para futuro cadastro pelo professor e análise por LLM. Não exigir esses dois rótulos interpretativos na classificação automática desta entrega. As heurísticas provisórias o/u e e/i átonas e -il → -io devem ser removidas. Isso não retira os pares da bateria: ambos continuam sendo erros objetivos e devem receber somente classificações observáveis cobertas por regras confirmadas, ou NaoClassificado quando nenhuma delas se aplicar. Nenhuma nova integração com LLM faz parte deste ciclo. Aguardar a confirmação do fullstack de que esse ajuste está no assembly ativo antes de executar.

Decisão mais recente do usuário: não agrupar omissão, acréscimo e substituição. Para classificações novas, exigir os nomes separados Omissão, Acréscimo e Substituição, sem categoria adicional. Preservar os registros e valores de enum legados; a existência do nome agrupado no histórico não é falha, mas sua atribuição a uma classificação nova é divergência. Esta decisão substitui o nome agrupado presente na tabela original e no relatório histórico.

## Casos positivos

| Esperado → resposta | Verificação |
| --- | --- |
| bolacha → bulacha | Erro objetivo; não atribuir Transcrição da fala / escrita fonética; aceitar somente regra observável confirmada, ou NaoClassificado |
| faca → vaca | Troca de grafemas / surda-sonora |
| carro → caro | Regra contextual |
| cachorro → cachoro | Regra contextual; novo resultado em nova tentativa |
| casa → caza | Grafia arbitrária / irregular |
| nuvem → nuve | Nasalização |
| prato → pato | Os dois tipos Omissão e Complexidade silábica, sem duplicatas e sem tipo principal |
| gato → gto | Omissão; não atribuir o nome agrupado legado |
| gato → gatto | Acréscimo; não atribuir o nome agrupado legado |
| gato → cato | Substituição; eventuais outros tipos somente por regras confirmadas |
| frágil → frágio | Erro objetivo; não atribuir Hipercorreção / supergeneralização; aceitar somente regra observável confirmada, ou NaoClassificado |
| café → cafe | Acentuação |
| embora → em bora | Juntura / segmentação |
| Maria → maria | Maiúsculas e pontuação; correto false; entra no cálculo de erros |
| gato → gato | correto true; NaoSeAplica; sem tipos |

chegaram → chegou deixa de ser uma expectativa incondicional. Confrontar a regra entregue e seus limites: aceitar classificação apenas com regra confiável baseada no próprio par; caso contrário exigir NaoClassificado. Não usar “As meninas” como evidência, pois esse contexto não faz parte da lacuna.

## Casos negativos e limites

| Esperado → resposta | O que verificar |
| --- | --- |
| carro → carro | Acerto; nenhum tipo, mesmo contendo rr |
| cachorro → cachorro | Acerto; nenhum tipo |
| carta → cata | Não atribuir a regra r/rr entre vogais; outros tipos somente se uma regra própria for demonstrada |
| rato → rrato | Não atribuir automaticamente a regra r/rr entre vogais, pois a posição é inicial |
| café → café | Acerto; sem Acentuação |
| Maria → Maria | Acerto; sem Maiúsculas e pontuação |
| gato → zebra | Sem classificação na ausência de regra delimitada; não inventar tipo genérico para toda diferença |
| embora → embora | Acerto; sem Juntura / segmentação |

O relatório final deve distinguir os dois tipos adiados das divergências das regras que continuam no escopo. Não chamar os tipos adiados de implementados ou aprovados, nem reescrever os resultados históricos como se já usassem esta decisão.

Casos adicionais devem ser escolhidos após ler os limites efetivamente entregues. Não exigir a grafia de todo par desconhecido nem aprovar falsos positivos só porque o exemplo principal passou.

## Execução e evidência

Usar Playwright com Chromium visível, frontend na porta 5173 e API real na porta 5031, sem interceptação ou respostas simuladas. Confirmar o login pela UI com proxy real. Reutilizar o ditado separado ID 3 para a bateria original, criando nova tentativa; para os negativos, criar outro ditado separado autorizado. Preservar integralmente Ditado ID 1 e RespostaDitado ID 1, inclusive associações.

Antes de executar, atualizar uma cópia do roteiro automatizado anterior, preservando as evidências antigas. Substituir as expectativas de maiúsculas, r/rr e concordância, remover estados derivados de índices fixos e verificar também correto, nota, tipos indevidos, método, duplicatas e consistência entre UI e API. Salvar a resposta real antes da navegação para não perder evidência numa recarga do Vite.

Capturar tela final, mensagens relevantes do console, erros JavaScript e status de rede. Separar aprovação do conteúdo linguístico de aprovação da interface. Confirmar persistência pela consulta disponível e não apenas pela resposta do POST; se não houver leitura apropriada, declarar esse limite. Executar os testes focados entregues pelo fullstack e informar comando e totais, sem tratar seu sucesso como substituto do fluxo real.
