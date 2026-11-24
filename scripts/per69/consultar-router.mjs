import fs from 'node:fs';

const entrada = JSON.parse(fs.readFileSync(0, 'utf8').replace(/^\uFEFF/, ''));
entrada.model = 'auto';
entrada.max_tokens = entrada.max_completion_tokens ?? entrada.max_tokens;
delete entrada.max_completion_tokens;

const inicio = performance.now();
try {
  const resposta = await fetch(process.argv[2] ?? 'http://router:4000/v1/chat/completions', {
    method: 'POST',
    headers: { 'content-type': 'application/json', authorization: `Bearer ${process.env.FREE_LLM_API_KEY}` },
    body: JSON.stringify(entrada),
    signal: AbortSignal.timeout(60000),
  });
  const corpo = await resposta.json();
  const identificadoresSeguros = /^[\w./:-]+$/;
  const cabecalho = nome => {
    const valor = resposta.headers.get(nome);
    return valor && identificadoresSeguros.test(valor) ? valor : null;
  };
  const trilha = corpo.error?.message?.match(/tried: ([^)]+)\)/)?.[1];
  const falha = trilha && /^[\w./:; →-]+$/.test(trilha) ? trilha : null;
  console.log(JSON.stringify({
    entrada,
    status: resposta.status,
    latenciaMs: Math.round(performance.now() - inicio),
    provedor: cabecalho('x-free-llm-provider'),
    modelo: cabecalho('x-free-llm-model'),
    tentativas: cabecalho('x-free-llm-attempts'),
    uso: corpo.usage ?? null,
    fim: corpo.choices?.[0]?.finish_reason ?? null,
    saida: corpo.choices?.[0]?.message?.content ?? null,
    falha,
  }, null, 2));
} catch (erro) {
  console.log(JSON.stringify({ entrada, latenciaMs: Math.round(performance.now() - inicio),
    status: null, falha: erro.name, saida: null, uso: null }, null, 2));
  process.exitCode = 1;
}
