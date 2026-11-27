const assert = require('node:assert/strict');
const { readFileSync, writeFileSync } = require('node:fs');
const baseUrl = 'http://127.0.0.1:4270';

const definicoes = [
  ['Concordância verbal', 'Marque apenas a lacuna do verbo que discorda em número do sujeito escrito na mesma frase.', 'Os alunos chegaram', 'Os alunos chegou'],
  ['Troca inicial de ch por x', 'Marque quando a única diferença for substituir ch inicial por x.', 'chave', 'xave'],
  ['Supressão de til em ão', 'Marque quando a única diferença for substituir ão final por ao.', 'portão', 'portao'],
  ['Redução de ss para s', 'Marque quando a única diferença for substituir ss entre vogais por s.', 'passou', 'pasou'],
  ['Troca de m por n antes de p', 'Marque quando a única diferença for substituir m por n imediatamente antes de p.', 'campo', 'canpo'],
];
const casos = [
  ['Concordância', 'Os alunos [chegaram]. O [café] esfriou.', ['chegou', 'cafe'], [[0], []], 0],
  ['CH inicial', 'A [chave] caiu.', ['xave'], [[1]], 0],
  ['Til final', 'O [portão] abriu.', ['portao'], [[2]], 0],
  ['SS intervocálico', 'O trem [passou].', ['pasou'], [[3]], 0],
  ['M antes de P', 'O [campo] secou.', ['canpo'], [[4]], 0],
  ['Grafias combinadas', 'A [chave] do [portão] caiu. O trem [passou] perto do [campo].', ['xave', 'portao', 'pasou', 'canpo'], [[1], [2], [3], [4]], 0],
  ['Controles limpos', 'Os alunos [chegaram]. A [chave] abriu o [portão]. O trem [passou] no [campo].', ['chegaram', 'chave', 'portão', 'passou', 'campo'], [[], [], [], [], []], 100],
  ['Controle contextual', '[As crianças] [chegaram] cedo. Os professores [chegaram] cedo.', ['A criança', 'chegou', 'chegou'], [[], [], [0]], 0],
  ['Lacuna ausente', 'O [portão] abriu. O [campo] secou.', [null, 'campo'], [[], []], 50],
  ['Misto com controle limpo', 'As meninas [saltaram]. A [chave] abriu. O rato [passou].', ['saltou', 'chave', 'pasou'], [[0], [], [3]], 33.33],
];

async function executar() {
  const destino = process.argv[2];
  assert.ok(destino, 'Informe o arquivo das capturas sanitizadas.');
  const dashboard = await (await fetch(baseUrl, { signal: AbortSignal.timeout(5000) })).text();
  const chave = process.env.DITADO_LLM_ROUTER_KEY || dashboard.match(/"key"\s*:\s*"([^"]+)"/)?.[1];
  assert.ok(chave, 'Credencial local indisponível.');
  const fonte = readFileSync('Ditado.API/Llm/LlmProviderClient.cs', 'utf8');
  const prompt = fonte.match(/InstrucoesRouter = """\s*([\s\S]*?)\s*""";/)?.[1].replace(/^ {8}/gm, '').replace(/\r\n/g, '\n');
  assert.ok(prompt);
  const errosCandidatos = definicoes.map(([nome, descricao, exemploEsperado, exemploResposta], i) =>
    ({ id: i + 1, nome, descricao, exemploEsperado, exemploResposta }));
  const registros = [];
  for (const [titulo, textoComMarcacoes, escritas, esperados] of casos) {
    const lacunas = Array.from(textoComMarcacoes.matchAll(/\[([^\]]+)\]/g), (m, i) =>
      ({ segmentoId: 101 + i, esperado: m[1] }));
    assert.equal(lacunas.length, escritas.length);
    const entrada = {
      ditado: { titulo, descricao: null, textoComMarcacoes, categorias: [], lacunas },
      errosCandidatos,
      respostas: lacunas.map(({ segmentoId }, i) => ({ segmentoId, resposta: escritas[i] }))
    };
    const gabarito = lacunas.map(({ segmentoId }, i) => ({
      segmentoId, candidatosCorrespondentes: esperados[i].map(id => id + 1),
      inconclusivo: escritas[i] === null
    }));
    const request = { model: 'auto', response_format: { type: 'json_object' }, max_tokens: 4096,
      messages: [{ role: 'system', content: prompt }, { role: 'user', content: JSON.stringify(entrada) }] };
    const inicio = performance.now();
    let retorno;
    try {
      const resposta = await fetch(baseUrl + '/v1/chat/completions', {
        method: 'POST', body: JSON.stringify(request), signal: AbortSignal.timeout(65000), redirect: 'error',
        headers: { 'content-type': 'application/json', authorization: 'Bearer ' + chave }
      });
      const corpo = await resposta.json().catch(() => null);
      retorno = {
        http: resposta.status, provedor: resposta.headers.get('x-free-llm-provider'),
        modelo: resposta.headers.get('x-free-llm-model'), tentativas: resposta.headers.get('x-free-llm-attempts'),
        cache: resposta.headers.get('x-free-llm-cache'), modeloEnvelope: corpo?.model,
        uso: corpo?.usage, fim: corpo?.choices?.[0]?.finish_reason, saida: corpo?.choices?.[0]?.message?.content,
        erro: corpo?.error?.type
      };
    } catch (erro) {
      retorno = { erro: erro.name };
    }
    registros.push({ entrada, gabarito, retorno, latenciaMs: Math.round(performance.now() - inicio) });
    writeFileSync(destino, JSON.stringify({ modeloSolicitado: 'auto', formatoSolicitado: 'json_object', maxTokens: 4096, prompt, registros }, null, 2));
    console.log(registros.length + '/10 ' + titulo + ' — ' + (retorno.http ?? retorno.erro));
  }
}

executar().catch(erro => {
  console.error(erro.message);
  process.exitCode = 1;
});
