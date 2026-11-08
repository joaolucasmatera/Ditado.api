# CAT-02 — Publicar erro no Catálogo de erros

## Aprovação e limite

Usuário aprovou o formulário revisado com “Ok, gostei”, conforme SQUAD, 6 nov 2026. O nome de produto vigente é **Catálogo de erros**; “Marketplace” permanece somente em nomes técnicos, caminhos e referências históricas. Esta página é o contrato vigente da fatia e substitui propostas CAT02-P1 de [CAT-09](catalogo-contrato.md), sem encerrar o enabler global. Uma história funcional por vez; CAT-01/listagem ampla e CAT-05/seleção permanecem fora.

Nome e Descrição obrigatórios; Exemplo opcional em um grupo Escrita esperada → Resposta do aluno (ambos ou nenhum); Observação pedagógica e Série/ano opcionais. Inputs vazios, placeholders apenas ilustrativos, labels e ajuda sempre visíveis. A descrição pode conter orientação/exemplos livres além do grupo estruturado. Sem executor/prompt/modelo, sem chamada LLM ou alteração de classificações/notas ao publicar.

## Contrato implementado

`POST /api/ErrosCatalogo` — Professor/Administrador; anônimo401, aluno403. Entrada:

```json
{
  "nome": "Troca de M por N antes de P ou B",
  "descricao": "Descrição do padrão observado.",
  "exemploEsperado": "também",
  "exemploResposta": "tanbém",
  "observacaoPedagogica": "Retomar a regularidade.",
  "serieAno": "3º ano"
}
```

Representação é par de campos, não array nem lista repetível. Opcionais ausentes/nulos/brancos normalizam para null; exemplo com uma parte não vazia exige a outra. Nome/descrição nulos/vazios/brancos são inválidos. Espaços externos removidos, conteúdo interno preservado. Tamanhos técnicos em caracteres: nome/série200, descrição/observação10000, cada parte do exemplo2000. Não há limite de negócio de quantidade, unicidade de nome ou lista fechada de séries.

Campos desconhecidos são rejeitados (400), inclusive forjas de ID/código/autor/estado/datas e configuração de executor/prompt/modelo. Servidor gera `Codigo=ERR_` + GUID, ID, autor autenticado, DataCriacao/DataAlteracao UTC e Ativo=true. Índice único protege código; nomes repetidos permitidos. Não há propriedade configurável de executor nesta entidade da fatia; `temRegraExecutavel=false` é explícito no recibo. Formato GUID é detalhe técnico, não código digitado pelo usuário.

201 retorna registro criado completo com Location para `GET /api/ErrosCatalogo/{id}`. GET é consulta mínima do recibo, autorizada aos mesmos papéis; 404 inexistente. Nenhum GET de listagem ampla. UI em `/marketplace/publicar`, menu Marketplace staff; confirmação usa resposta real persistida e mostra nome, descrição, exemplo, observação, série, estado ativo e ID numérico como “Número do registro”. O código técnico ERR_... permanece no DTO/API/banco, mas não é renderizado na tela. Não é CAT-01 entregue.

Erros de validação usam ProblemDetails/ValidationProblemDetails do ApiController. Falha mantém campos, não exibe recibo de sucesso; botão e campos ficam bloqueados durante envio, com guarda de reentrada. Retry não implica contrato novo de idempotência da API.

## Persistência e preservação

Entidade/tabela própria `ErroCatalogo`/`ErrosCatalogo`, sem Categoria/DitadoCategoria. Relação AutorId→Usuarios com Restrict; índice único em código e índice autor. Migration `PublicarErroMarketplace` cria somente tabela/índices/FK, sem N:N de seleção, seeds, alteração de enums ou respostas. Descrição/observação são TEXT para evitar exceder tamanho de linha MySQL mantendo os limites de validação. Outras escolhas globais de Marketplace continuam em CAT-09 e não são implementadas por esta fatia.

## Validação

Testes `MarketplaceControllerTests` cobrem POST/recibo/persistência, autor/estado/datas/código, papéis, obrigatórios, opcionais, exemplo incompleto, forja de propriedades, nomes repetidos, limites e consulta mínima. QA independente seguirá [roteiro planejado](../qa-cat02-roteiro-2026-11-06.md) para UI visível, rede real, MySQL e snapshots protegidos. Resultados efetivos serão registrados no relatório do ciclo; esta seção não declara testes aprovados antecipadamente.

## Ajuste de apresentação aprovado no ciclo

Conforme autorização humana via Squad ditado 02, formulário alinhado ao padrão Vuetify existente em CadastroCategoria/CadastroDitado: cabeçalho em card, pa-6, text-h4, campos outlined/comfortable e botões do tema. Labels externos sempre visíveis, placeholders ilustrativos persistentes e ajuda permanente; campos e validações permanecem os mesmos. Recibo com hierarquia entre confirmação, estado e dados; código técnico oculto somente na UI, ID numérico rotulado “Número do registro”. Texto aprovado: “No momento, os erros cadastrados ainda não entram na correção automática.” Sem menção a LLM ou promessa de prazo no aviso ao usuário. DTO/API/storage inalterados.
