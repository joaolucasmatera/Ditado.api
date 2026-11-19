using Ditado.Aplicacao.Llm;
using Ditado.Dominio.Entidades;
using Ditado.Dominio.Enums;
using Ditado.Infra.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Ditado.Testes.Services;

public sealed class LlmAnalysisProcessorTests
{
    [Theory]
    [InlineData("concordancia", "Groq", LlmAnaliseEstado.Concluida)]
    [InlineData("inedito", "Groq", LlmAnaliseEstado.Concluida)]
    [InlineData("historico", "Groq", LlmAnaliseEstado.Concluida)]
    [InlineData("limite", "Groq,OpenRouter", LlmAnaliseEstado.Concluida)]
    [InlineData("todos", "Groq,OpenRouter,Cohere,GonkaBroker", LlmAnaliseEstado.Falha)]
    [InlineData("vazio", "Groq", LlmAnaliseEstado.Concluida)]
    [InlineData("fora", "Groq", LlmAnaliseEstado.Revisao)]
    [InlineData("duplicado", "Groq", LlmAnaliseEstado.Revisao)]
    [InlineData("sobreposto", "Groq", LlmAnaliseEstado.Revisao)]
    [InlineData("cobertura", "Groq", LlmAnaliseEstado.Revisao)]
    [InlineData("grupoAusente", "Groq", LlmAnaliseEstado.Revisao)]
    [InlineData("confiança", "Groq", LlmAnaliseEstado.Revisao)]
    [InlineData("permanente", "Groq", LlmAnaliseEstado.Revisao)]
    [InlineData("malformado", "Groq", LlmAnaliseEstado.Revisao)]
    [InlineData("nulo", "Groq", LlmAnaliseEstado.Revisao)]
    public async Task EnviaUmaVezPorProvedorEValidaTodasAsLacunasAntesDeGravar(string caso, string ordem, LlmAnaliseEstado estado)
    {
        await using var context = new DitadoDbContext(new DbContextOptionsBuilder<DitadoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var inedito = caso == "inedito";
        var sujeito = new DitadoSegmento { Ordem = 1, Tipo = TipoSegmento.Lacuna, Conteudo = inedito ? "As equipes" : "Os alunos" };
        var verbo = new DitadoSegmento { Ordem = 3, Tipo = TipoSegmento.Lacuna, Conteudo = inedito ? "viajaram" : "chegaram" };
        var ausente = new DitadoSegmento { Ordem = 5, Tipo = TipoSegmento.Lacuna, Conteudo = "ontem" };
        var ditado = new Ditado.Dominio.Entidades.Ditado
        {
            Segmentos = [ausente, verbo, new() { Ordem = 6, Tipo = TipoSegmento.Texto, Conteudo = ". ⟦123⟧" },
                sujeito, new() { Ordem = 2, Tipo = TipoSegmento.Texto, Conteudo = " " },
                new() { Ordem = 4, Tipo = TipoSegmento.Texto, Conteudo = " " }]
        };
        var escritaSujeito = inedito ? "A equipe" : "O aluno";
        var escritaVerbo = caso == "vazio" ? "chegou" : verbo.Conteudo;
        var resposta = new RespostaDitado
        {
            Ditado = ditado, Nota = 33.33m,
            RespostasSegmentos =
            [
                Lacuna(sujeito, escritaSujeito), Lacuna(verbo, escritaVerbo),
                Lacuna(ausente, "", ausente: true)
            ]
        };
        resposta.RespostasSegmentos.First().TiposErro.Add(new() { TipoErro = TipoErro.SubstituicaoLetra });
        var reserva = resposta.RespostasSegmentos.First().AnaliseLlm!;
        reserva.Estado = LlmAnaliseEstado.Processando;
        reserva.IniciadaEm = DateTime.UtcNow.AddMinutes(-6);
        if (caso == "historico")
        {
            var anterior = resposta.RespostasSegmentos.Last().AnaliseLlm!;
            anterior.Estado = LlmAnaliseEstado.Concluida;
            anterior.PromptVersao = "per18-v1";
            anterior.Candidatos.First().Evidencia = "Evidência histórica";
        }
        context.Add(resposta);
        await context.SaveChangesAsync();
        var fixture = new Fixture(caso);
        var processor = new LlmAnalysisProcessor(context, fixture, Options.Create(new LlmOptions
        {
            Enabled = true,
            Chain = [new() { Provider = "Groq", Model = "fixture" }, new() { Provider = "OpenRouter", Model = "fixture/backup" },
                new() { Provider = "Cohere", Model = "fixture" }, new() { Provider = "GonkaBroker", Model = "fixture" }]
        }));
        Assert.True(await processor.ProcessNextAsync(default));
        Assert.False(await processor.ProcessNextAsync(default));
        Assert.Equal(ordem, string.Join(",", fixture.Chamadas.Select(c => c.Provedor)));
        var ids = resposta.RespostasSegmentos.Select(s => s.Id).ToArray();
        Assert.All(fixture.Chamadas, chamada =>
        {
            var entrada = chamada.Entrada;
            Assert.Equal($"{escritaSujeito} {escritaVerbo} . ⟦123⟧", entrada.TextoEscrito);
            Assert.Equal($"⟦{ids[0]}⟧{sujeito.Conteudo}⟦/{ids[0]}⟧ ⟦{ids[1]}⟧{verbo.Conteudo}⟦/{ids[1]}⟧ ⟦{ids[2]}⟧ontem⟦/{ids[2]}⟧. ⟦123⟧", entrada.TextoEsperado);
            Assert.Equal(ids, entrada.Lacunas.Select(l => l.Id));
            Assert.Equal(new[] { true, true, caso != "historico" }, entrada.Lacunas.Select(l => l.Avaliar));
            Assert.Null(entrada.Lacunas[2].Escrito);
            Assert.Equal(new[] { 31, 32 }, entrada.Candidatos.Select(c => c.Id));
            Assert.Equal("Concordância verbal", entrada.Candidatos[0].Nome);
        });

        context.ChangeTracker.Clear();
        var tarefas = await context.LlmAnalisesTarefas.Include(t => t.Candidatos).Include(t => t.TentativasProvedor).ToListAsync();
        Assert.Equal(3, tarefas.Count);
        Assert.All(tarefas, t => Assert.Equal(estado, t.Estado));
        if (caso == "historico")
        {
            var anterior = tarefas.Single(t => t.RespostaSegmentoId == ids[2]);
            Assert.Equal("per18-v1", anterior.PromptVersao);
            Assert.Equal("Evidência histórica", anterior.Candidatos.Single(c => c.ErroCatalogoId == 31).Evidencia);
        }
        var trilha = tarefas.SelectMany(t => t.TentativasProvedor).OrderBy(a => a.Numero).ToArray();
        Assert.Equal(ordem, string.Join(",", trilha.Select(a => a.Provedor)));
        Assert.All(trilha, a =>
        {
            Assert.NotNull(a.EncerradaEm);
            Assert.Equal(LlmOptions.PromptVersion, a.PromptVersao);
        });
        if (caso == "limite")
        {
            Assert.Equal("LimiteProvedor", trilha[0].Motivo);
            Assert.Equal(429, trilha[0].HttpStatus);
        }
        if (caso == "todos")
            Assert.Equal(new[] { "LimiteProvedor", "Timeout", "Rede", "Indisponivel" }, trilha.Select(a => a.Motivo));
        if (caso == "vazio") Assert.Equal("SemCorrespondencia", Assert.Single(trilha).Desfecho);
        if (estado != LlmAnaliseEstado.Concluida)
            Assert.All(tarefas.SelectMany(t => t.Candidatos), c => Assert.Equal(LlmCandidatoEstado.NaoAvaliado, c.Estado));
        else
        {
            var achados = tarefas.SelectMany(t => t.Candidatos.Select(c => (t.RespostaSegmentoId, Candidato: c)))
                .Where(x => x.Candidato.Estado == LlmCandidatoEstado.Correspondente).ToArray();
            if (caso == "vazio") Assert.Empty(achados);
            else
            {
                var achado = Assert.Single(achados);
                Assert.Equal(ids[1], achado.RespostaSegmentoId);
                Assert.Equal("O sujeito escrito é singular.", achado.Candidato.Evidencia);
            }
        }
        Assert.Equal(33.33m, (await context.RespostaDitados.SingleAsync()).Nota);
        Assert.Equal(TipoErro.SubstituicaoLetra, (await context.RespostaSegmentoTiposErro.SingleAsync()).TipoErro);
    }

    private static RespostaSegmento Lacuna(DitadoSegmento segmento, string escrito, bool ausente = false) => new()
    {
        Segmento = segmento, RespostaEsperada = segmento.Conteudo, RespostaFornecida = escrito, RespostaAusente = ausente,
        Correto = escrito == segmento.Conteudo,
        AnaliseLlm = new()
        {
            Candidatos =
            [
                new() { ErroCatalogoId = 31, Nome = "Concordância verbal", Descricao = "Compare o sujeito e o verbo escritos." },
                new() { ErroCatalogoId = 32, Nome = "Ortografia", Descricao = "Compare a grafia com o esperado." }
            ]
        }
    };

    private sealed class Fixture(string caso) : ILlmProviderClient
    {
        public List<(string Provedor, LlmAttemptPrompt Entrada)> Chamadas { get; } = [];

        public Task<LlmProviderResponse> ClassificarAsync(LlmProviderOptions provider, LlmAttemptPrompt entrada, CancellationToken cancellationToken)
        {
            Chamadas.Add((provider.Provider, entrada));
            var verboId = entrada.Lacunas[1].Id;
            var grupos = entrada.Candidatos.Select(c => new LlmClassificationOutput { CandidatoId = c.Id }).ToList();
            var achado = new LlmGapFinding { LacunaId = verboId, Evidencia = "O sujeito escrito é singular." };
            if (caso != "vazio") grupos[0].Correspondentes.Add(achado);
            var cobertura = entrada.Lacunas.Where(l => l.Avaliar).Select(l => l.Id).ToList();
            if (caso == "fora") grupos[1].Correspondentes.Add(new() { LacunaId = int.MaxValue });
            if (caso == "duplicado") grupos[1].CandidatoId = grupos[0].CandidatoId;
            if (caso == "sobreposto") grupos[0].Inconclusivas.Add(achado);
            if (caso == "cobertura") cobertura.RemoveAt(2);
            if (caso == "grupoAusente") grupos.RemoveAt(1);
            if (caso == "confiança") grupos[1].Correspondentes.Add(new() { LacunaId = verboId, Confianca = 2 });
            return Task.FromResult(caso switch
            {
                "permanente" => new LlmProviderResponse(LlmProviderOutcome.Permanente),
                "malformado" => new LlmProviderResponse(LlmProviderOutcome.SaidaInvalida),
                "nulo" => new LlmProviderResponse(LlmProviderOutcome.Sucesso, new() { Classificacoes = null! }),
                "limite" when provider.Provider == "Groq" => new(LlmProviderOutcome.Tecnica, Motivo: LlmMotivo.LimiteProvedor, HttpStatus: 429),
                "todos" => new(LlmProviderOutcome.Tecnica, Motivo: provider.Provider switch
                {
                    "Groq" => LlmMotivo.LimiteProvedor,
                    "OpenRouter" => LlmMotivo.Timeout,
                    "Cohere" => LlmMotivo.Rede,
                    _ => LlmMotivo.Indisponivel
                }),
                _ => new(LlmProviderOutcome.Sucesso, new() { LacunasAvaliadas = cobertura, Classificacoes = grupos })
            });
        }
    }
}
