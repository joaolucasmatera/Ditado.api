const assert=require('node:assert/strict');
const {randomUUID}=require('node:crypto');
const modo = process.argv[2] ?? 'disponivel';
assert.ok(['disponivel', 'indisponivel'].includes(modo));
const apiUrl = 'http://127.0.0.1:5069/api';
const start = performance.now();
async function api(method, route, status, body, token) {
  const response = await fetch(apiUrl + route, {
    method,
    headers: { 'Content-Type': 'application/json', ...(token && { Authorization: 'Bearer ' + token }) },
    body: body === undefined ? undefined : JSON.stringify(body)
  });
  assert.equal(response.status, status, method + ' ' + route);
  return status === 200 || status === 201 ? response.json() : null;
}

async function criarEscola() {
  const login = `per64-${randomUUID()}@teste.invalid`;
  const senha = randomUUID();
  const professor = await api('POST', '/Usuarios/cadastro-professor', 200, {
    nome: 'Professor sintético', login, senha, opcaoEscola: 'CriarNova', nomeNovaEscola: 'Escola sintética PER-69'
  });
  const { token } = await api('POST', '/Usuarios/login', 200, { login, senha });
  const [contexto] = await api('GET', '/Usuarios/contexto-escolar', 200, undefined, token);
  const turma = await api('POST', '/Turmas', 201, {
    nome: 'Turma sintética', serie: 3, anoLetivo: 2026, escolaId: contexto.escolaId, professorResponsavelId: professor.id
  }, token);
  const aluno = await api('POST', '/Usuarios/alunos-manual', 200, {
    nome: 'Aluno sintético', numeroChamada: 1, turmaId: turma.id
  }, token);
  const acesso = { matriculaOuCodigo: aluno.codigoCadastro, pin: String(1000 + (parseInt(randomUUID().slice(0, 4), 16) % 9000)) };
  await api('POST', '/Usuarios/primeiro-acesso-aluno', 200, acesso);
  const authAluno = await api('POST', '/Usuarios/login-aluno', 200, acesso);
  return { token, turma, authAluno };
}
(async()=>{
 const escola=await criarEscola(), user=escola.authAluno.token;
 const acento=(await api('GET','/ErrosCatalogo?estado=ativo',200,undefined,escola.token)).find(e=>e.codigo==='COR01_ACENTUACAO');
 assert.ok(acento?.temRegraExecutavel);
 const docente=await api('POST','/ErrosCatalogo',201,{nome:'Concordância entre sujeito e verbo escritos',descricao:'Classifique somente o verbo que não concorda em número com o sujeito efetivamente escrito na mesma frase. Quando ambos estão no singular ou ambos no plural, a concordância é válida, inclusive se o gabarito tinha outro número.'},escola.token);
 const ditado=await api('POST','/Ditados',201,{titulo:'Fixture sintética controle contextual',textoComMarcacoes:'O [café] esfriou. No porto, [as crianças] [chegaram] cedo. Na escola, os professores [chegaram] cedo. O sino [tocou].',audioBase64:'data:audio/mpeg;base64,SUQzAwAAAAAAJlRQRTEAAAAcAAAAU291bmRKYXk=',erroCatalogoIds:[acento.id,docente.id]},escola.token);
 await api('POST','/Ditados/'+ditado.id+'/visibilidade',204,{nivel:1},escola.token);
 await api('POST','/Turmas/'+escola.turma.id+'/ditados',204,{ditadoId:ditado.id,dataLimite:new Date(Date.now()+86400000).toISOString()},escola.token);
 const tarefa=await api('GET','/Ditados/'+ditado.id+'/realizar',200,undefined,user);
 const lacunas=tarefa.segmentos.filter(s=>s.tipo.toLowerCase()==='lacuna').sort((a,b)=>a.ordem-b.ordem);
 const escritas=['cafe','a criança','chegou','chegou','tocou'];assert.equal(lacunas.length,escritas.length);
 const initial=await api('POST','/Ditados/'+ditado.id+'/submeter',200,{respostas:lacunas.map((s,i)=>({segmentoId:s.segmentoId,resposta:escritas[i]}))},user);
 assert.equal(initial.nota,20);
 assert.deepEqual(initial.detalhes[0].classificacoes.map(c=>c.origem),['Codigo']);
 let final;const path='/Alunos/tentativas/'+initial.respostaDitadoId+'/resultado';
 for (let i = 0; i < 75; i++) {
   final = await api('GET', path, 200, undefined, user);
   if (final.detalhes.every(d => !['Pendente', 'Processando'].includes(d.estadoAnaliseLlm))) break;
   await new Promise(resolve => setTimeout(resolve, 1000));
 }
 const relido = await api('GET', path, 200, undefined, user);
 const origensEsperadas = modo === 'indisponivel' ? [['Codigo'], [], [], [], []] : [['Codigo'], [], [], ['LLM'], []];
 const estadoEsperado = modo === 'indisponivel' ? 'Falha' : 'Concluida';
 const origensObtidas = final.detalhes.map(d => d.classificacoes.map(c => c.origem));
 const releituraIgual = JSON.stringify(relido) === JSON.stringify(final);
 console.log(JSON.stringify({ modo, statusSubmissao: 200, statusReleitura: 200, latenciaFluxoMs: Math.round(performance.now() - start),
   tentativaId: initial.respostaDitadoId, notaInicial: initial.nota, notaFinal: final.nota, releituraIgual, estadoEsperado,
   origensEsperadas, saida: final }, null, 2));
 assert.equal(final.nota, 20);
 assert.ok(final.detalhes.every(d => d.estadoAnaliseLlm === estadoEsperado));
 assert.deepEqual(origensObtidas, origensEsperadas);
 assert.deepEqual(relido, final);
})().catch(e=>{console.error(e.message);process.exitCode=1;});
