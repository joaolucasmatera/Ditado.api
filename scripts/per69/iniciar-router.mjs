import fs from 'node:fs';
import http from 'node:http';
import path from 'node:path';
import { pathToFileURL } from 'node:url';

const home = process.env.FREE_LLM_HOME;
const routerRoot = process.argv[2]
  ? pathToFileURL(path.resolve(process.argv[2]) + path.sep)
  : new URL('./', import.meta.url);
const source = JSON.parse(fs.readFileSync(new URL('catalog.snapshot.json', routerRoot)));
const providers = new Set(['groq-cloud', 'cohere', 'openrouter']);
source.providers = source.providers.filter(provider => providers.has(provider.slug)).map(provider => ({
  ...provider,
  anonymous_key: null,
  models: provider.slug === 'openrouter'
    ? provider.models.filter(model => model.id.endsWith(':free'))
    : provider.models,
}));
fs.mkdirSync(home, { recursive: true, mode: 0o700 });
const catalog = JSON.stringify(source);
fs.writeFileSync(path.join(home, 'catalog.json'), catalog);
const catalogServer = http.createServer((_, response) => {
  response.writeHead(200, { 'content-type': 'application/json' });
  response.end(catalog);
});
await new Promise(resolve => catalogServer.listen(0, '127.0.0.1', resolve));

const configPath = path.join(home, 'config.json');
const saved = fs.existsSync(configPath) ? JSON.parse(fs.readFileSync(configPath)) : {};
const config = {
  ...saved,
  host: '0.0.0.0',
  port: 4000,
  strategy: 'order',
  chain: [
    'groq-cloud/openai/gpt-oss-120b',
    'cohere/command-a-plus-05-2026',
    'openrouter/particle-ai/particle-1:free',
  ],
  sticky_minutes: 0,
  max_attempts: 5,
  request_timeout_s: 55,
  first_token_timeout_s: 18,
  catalog: { url: `http://127.0.0.1:${catalogServer.address().port}/catalog.json`, refresh_hours: 24 },
  cache: { enabled: false },
  ollama: { enabled: false },
  mcp: { enabled: false },
  log_requests: false,
};
delete config.api_key;
fs.writeFileSync(configPath, JSON.stringify(config), { mode: 0o600 });
const { startServer } = await import(new URL('src/server.js', routerRoot));
await startServer({ quiet: true });
