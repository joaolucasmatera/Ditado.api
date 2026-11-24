import assert from 'node:assert/strict';
import fs from 'node:fs';
import http from 'node:http';
import os from 'node:os';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import { execFileSync } from 'node:child_process';
import { randomUUID } from 'node:crypto';

const [checkout, fixture] = process.argv.slice(2);
const revision = 'baf4e21c7b39692e143a85a307eb62490aa7bf6f';
assert.equal(execFileSync('git', ['-C', checkout, 'rev-parse', 'HEAD'], { encoding: 'utf8' }).trim(), revision);
assert.equal(execFileSync('git', ['-C', checkout, 'diff', 'HEAD', '--', 'router'], { encoding: 'utf8' }), '');
const payload = JSON.parse(fs.readFileSync(fixture, 'utf8'));
payload.model = 'auto';
payload.max_tokens = payload.max_completion_tokens;
delete payload.max_completion_tokens;
const synthetic = JSON.parse(payload.messages[1].content);
assert.deepEqual(synthetic.candidatos.map(candidate => candidate.id), [17, 29, 43]);
assert.deepEqual(synthetic.blocos.flatMap(block => block.lacunas.map(gap => gap.id)), [1, 2, 3, 4, 5, 6, 7, 8]);

const home = fs.mkdtempSync(path.join(os.tmpdir(), 'ditado-per69-router-'));
process.env.FREE_LLM_HOME = home;
process.env.FREE_LLM_NO_ENV_IMPORT = '1';
process.env.FREE_LLM_API_KEY = randomUUID();
const root = pathToFileURL(path.resolve(checkout, 'router') + path.sep);
const calls = [];
let origin;
const route = (slug, id) => ({
  slug, name: slug, type: 'API', base_url: `${origin}/${slug}/v1`,
  openai_compatible: true, openai_base_url: `${origin}/${slug}/v1`,
  category: 'permanent', free_tier: 'truly_free', needs: [], anonymous_key: null,
  models: [{ id, name: id, key: id, kind: 'chat', context_tokens: 128000 }],
});
const match = lacunaId => ({ lacunaId, confianca: null, evidencia: null });
const expected = {
  lacunasAvaliadas: [1, 2, 3, 4, 5, 6, 7, 8],
  classificacoes: [
    { candidatoId: 17, correspondentes: [match(1), match(7)], inconclusivas: [match(8)] },
    { candidatoId: 29, correspondentes: [match(3)], inconclusivas: [] },
    { candidatoId: 43, correspondentes: [match(5)], inconclusivas: [] },
  ],
};
const mock = http.createServer(async (request, response) => {
  response.setHeader('content-type', 'application/json');
  if (request.url === '/catalog') {
    response.end(JSON.stringify({ schema_version: 2, providers: [route('alpha', 'limitado'), route('beta', 'alternativo')] }));
    return;
  }
  const chunks = [];
  for await (const chunk of request) chunks.push(chunk);
  const body = JSON.parse(Buffer.concat(chunks).toString());
  const status = body.model === 'limitado' ? 429 : body.response_format ? 400 : 200;
  const result = status === 200
    ? { choices: [{ index: 0, message: { content: JSON.stringify(expected) }, finish_reason: 'stop' }],
        usage: { prompt_tokens: 11, completion_tokens: 7, total_tokens: 18 } }
    : { error: { message: status === 429 ? 'Rate limit reached' : 'Unsupported parameter: response_format' } };
  calls.push({ model: body.model, status, responseFormat: Boolean(body.response_format),
    mesmoConteudo: body.messages[1].content === payload.messages[1].content, response: result });
  response.writeHead(status);
  response.end(JSON.stringify(result));
});
let router;
try {
  await new Promise(resolve => mock.listen(0, '127.0.0.1', resolve));
  origin = `http://127.0.0.1:${mock.address().port}`;
  fs.writeFileSync(path.join(home, 'config.json'), JSON.stringify({
    port: 0, catalog: { url: origin + '/catalog' }, strategy: 'order',
    chain: ['alpha/limitado', 'beta/alternativo'], sticky_minutes: 0, log_requests: false,
  }));
  const { addKey, saveConfig, loadConfig } = await import(new URL('src/config.js', root));
  const cfg = loadConfig();
  for (const provider of ['alpha', 'beta']) addKey(cfg, provider, randomUUID());
  saveConfig(cfg);
  const { startServer } = await import(new URL('src/server.js', root));
  ({ server: router } = await startServer({ quiet: true, port: 0 }));
  const start = performance.now();
  const response = await fetch(`http://127.0.0.1:${router.address().port}/v1/chat/completions`, {
    method: 'POST',
    headers: { 'content-type': 'application/json', authorization: `Bearer ${cfg.api_key}` },
    body: JSON.stringify(payload),
  });
  const result = await response.json();
  const latenciaMs = Math.round(performance.now() - start);
  assert.equal(response.status, 200, JSON.stringify(result));
  assert.deepEqual(calls.map(({ model, status, responseFormat, mesmoConteudo }) =>
    [model, status, responseFormat, mesmoConteudo]), [
    ['limitado', 429, true, true], ['alternativo', 400, true, true], ['alternativo', 200, false, true],
  ]);
  assert.equal(response.headers.get('x-free-llm-provider'), 'beta');
  assert.equal(response.headers.get('x-free-llm-model'), 'alternativo');
  assert.equal(response.headers.get('x-free-llm-attempts'), '3');
  assert.deepEqual(JSON.parse(result.choices[0].message.content), expected);
  console.log(JSON.stringify({
    revision, strategy: 'order', requestedModel: payload.model, payload, expected,
    status: response.status, latenciaMs, response: result,
    effectiveProvider: 'beta', effectiveModel: 'alternativo', calls,
  }, null, 2));
} finally {
  router?.closeAllConnections();
  router?.close();
  mock.closeAllConnections();
  mock.close();
  fs.rmSync(home, { recursive: true, force: true });
}
