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
    [InlineData("concordancia", "Simulado", LlmAnaliseEstado.Concluida)]
    [InlineData("inedito", "Simulado", LlmAnaliseEstado.Concluida)]
    [InlineData("pontuacao", "Simulado", LlmAnaliseEstado.Concluida)]
    [InlineData("historico", "Simulado", LlmAnaliseEstado.Concluida)]
    [InlineData("vazio", "Simulado", LlmAnaliseEstado.Concluida)]
    [InlineData("fora", "Simulado", LlmAnaliseEstado.Revisao)]
    [InlineData("duplicado", "Simulado", LlmAnaliseEstado.Revisao)]
    [InlineData("sobreposto", "Simulado", LlmAnaliseEstado.Revisao)]
    [InlineData("cobertura", "Simulado", LlmAnaliseEstado.Revisao)]
    [InlineData("grupoAusente", "Simulado", LlmAnaliseEstado.Revisao)]
    [InlineData("confiança", "Simulado", LlmAnaliseEstado.Revisao)]
    [InlineData("permanente", "Simulado", LlmAnaliseEstado.Revisao)]
    [InlineData("malformado", "Simulado", LlmAnaliseEstado.Revisao)]
    [InlineData("nulo", "Simulado", LlmAnaliseEstado.Revisao)]
    [InlineData("router-alternativo", "FreeLlmRouter", LlmAnaliseEstado.Concluida)]
    [InlineData("router-incompleto", "FreeLlmRouter", LlmAnaliseEstado.Revisao)]
    [InlineData("router-sem-modelo", "FreeLlmRouter", LlmAnaliseEstado.Revisao)]
    [InlineData("router-fora-do-ar", "FreeLlmRouter", LlmAnaliseEstado.Falha)]
    public async Task EnviaUmaVezPorProvedorEValidaTodasAsLacunasAntesDeGravar(string caso, string ordem, LlmAnaliseEstado estado)
    {
        await using var context = new DitadoDbContext(new DbContextOptionsBuilder<DitadoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var inedito = caso == "inedito";
        var sujeito = new DitadoSegmento { Ordem = 1, Tipo = TipoSegmento.Lacuna, Conteudo = caso switch
        {
            "inedito" => "As equipes", "vazio" => "As crianças", _ => "Os alunos"
        } };
        var verbo = new DitadoSegmento { Ordem = 3, Tipo = TipoSegmento.Lacuna, Conteudo = inedito ? "viajaram" : "chegaram" };
        var ausente = new DitadoSegmento { Ordem = 5, Tipo = TipoSegmento.Lacuna, Conteudo = "ontem" };
        var ditado = new Ditado.Dominio.Entidades.Ditado
        {
            Segmentos = [ausente, verbo, new() { Ordem = 6, Tipo = TipoSegmento.Texto, Conteudo = ". ⟦123⟧" },
                sujeito, new() { Ordem = 2, Tipo = TipoSegmento.Texto, Conteudo = " " },
                new() { Ordem = 4, Tipo = TipoSegmento.Texto, Conteudo = ". As crianças chegaram " }]
        };
        var escritaSujeito = caso switch
        {
            "vazio" => "A criança",
            "inedito" => "A equipe",
            "pontuacao" => "A equipe! ⟦14⟧",
            _ => "O aluno"
        };
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
            Enabled = true, Simulated = !caso.StartsWith("router-"),
            RouterUrl = caso.StartsWith("router-") ? "http://localhost:4000/v1" : null,
            Chain = [new() { Provider = "Groq", Model = "fixture" }, new() { Provider = "OpenRouter", Model = "fixture/backup" },
                new() { Provider = "Cohere", Model = "fixture" }]
        }));
        Assert.True(await processor.ProcessNextAsync(default));
        Assert.False(await processor.ProcessNextAsync(default));
        Assert.Equal(ordem, string.Join(",", fixture.Chamadas.Select(c => c.Provedor)));
        var ids = resposta.RespostasSegmentos.Select(s => s.Id).ToArray();
        Assert.All(fixture.Chamadas, chamada =>
        {
            var entrada = chamada.Entrada;
            Assert.Equal(new[] { $"{escritaSujeito} {escritaVerbo}.", " As crianças chegaram .", " ⟦123⟧" },
                entrada.Blocos.Select(b => b.TextoEscrito));
            Assert.Equal(ids.Take(2), entrada.Blocos[0].Lacunas.Select(l => l.Id));
            Assert.Equal(ids[2], Assert.Single(entrada.Blocos[1].Lacunas).Id);
            Assert.Empty(entrada.Blocos[2].Lacunas);
            Assert.Equal(sujeito.Conteudo, entrada.Blocos[0].Lacunas[0].Esperado);
            Assert.Equal(ids, entrada.Lacunas.Select(l => l.Id));
            Assert.Equal(new[] { true, true, caso != "historico" }, entrada.Lacunas.Select(l => l.Avaliar));
            Assert.Null(entrada.Blocos[1].Lacunas[0].Escrito);
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
        var provedorRegistrado = caso is "router-alternativo" or "router-incompleto" ? "provedor-alternativo" : ordem;
        Assert.Equal(provedorRegistrado, string.Join(",", trilha.Select(a => a.Provedor)));
        if (caso == "router-alternativo")
        {
            Assert.Equal(3, Assert.Single(trilha).TentativasRouter);
            Assert.Equal("familia/modelo-alternativo:free", trilha[0].Modelo);
            Assert.All(tarefas, tarefa =>
            {
                Assert.Equal(trilha[0].Modelo, tarefa.Modelo);
                Assert.Equal(provedorRegistrado, tarefa.Provedor);
            });
        }
        Assert.All(trilha, a =>
        {
            Assert.NotNull(a.EncerradaEm);
            Assert.Equal(caso.StartsWith("router-") ? LlmOptions.RouterPromptVersion : LlmOptions.PromptVersion, a.PromptVersao);
        });
        if (caso == "vazio")
        {
            Assert.Equal("SemCorrespondencia", Assert.Single(trilha).Desfecho);
            Assert.Equal(200, trilha[0].HttpStatus);
        }
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
            var verboId = entrada.Blocos[0].Lacunas[1].Id;
            var grupos = entrada.Candidatos.Select(c => new LlmClassificationOutput { CandidatoId = c.Id }).ToList();
            var achado = new LlmGapFinding { LacunaId = verboId, Evidencia = "O sujeito escrito é singular." };
            if (caso != "vazio") grupos[0].Correspondentes.Add(achado);
            var cobertura = entrada.Lacunas.Where(l => l.Avaliar).Select(l => l.Id).ToList();
            if (caso == "fora") grupos[1].Correspondentes.Add(new() { LacunaId = int.MaxValue });
            if (caso == "duplicado") grupos[1].CandidatoId = grupos[0].CandidatoId;
            if (caso == "sobreposto") grupos[0].Inconclusivas.Add(achado);
            if (caso == "cobertura") cobertura.RemoveAt(2);
            if (caso == "grupoAusente") grupos.RemoveAt(1);
            if (caso == "router-incompleto") grupos.RemoveAt(1);
            if (caso == "confiança") grupos[1].Correspondentes.Add(new() { LacunaId = verboId, Confianca = 2 });
            return Task.FromResult(caso switch
            {
                "permanente" => new LlmProviderResponse(LlmProviderOutcome.Permanente),
                "malformado" => new LlmProviderResponse(LlmProviderOutcome.SaidaInvalida),
                "nulo" => new LlmProviderResponse(LlmProviderOutcome.Sucesso, new() { Classificacoes = null! }),
                "router-fora-do-ar" => new(LlmProviderOutcome.Tecnica, Motivo: LlmMotivo.Rede),
                "router-alternativo" or "router-incompleto" => new(LlmProviderOutcome.Sucesso,
                    new() { LacunasAvaliadas = cobertura, Classificacoes = grupos }, HttpStatus: 200,
                    ProvedorEfetivo: "provedor-alternativo", ModeloEfetivo: "familia/modelo-alternativo:free", TentativasRouter: 3),
                "limite" or "terceiro" when provider.Provider == "Groq" =>
                    new(LlmProviderOutcome.Tecnica, Motivo: LlmMotivo.LimiteProvedor, HttpStatus: 429),
                "terceiro" when provider.Provider == "OpenRouter" =>
                    new(LlmProviderOutcome.Tecnica, Motivo: LlmMotivo.Timeout),
                "todos" => new(LlmProviderOutcome.Tecnica, Motivo: provider.Provider switch
                {
                    "Groq" => LlmMotivo.LimiteProvedor,
                    "OpenRouter" => LlmMotivo.Timeout,
                    "Cohere" => LlmMotivo.Rede,
                    _ => LlmMotivo.Indisponivel
                }),
                _ => new(LlmProviderOutcome.Sucesso, new() { LacunasAvaliadas = cobertura, Classificacoes = grupos }, HttpStatus: 200)
            });
        }
    }
}
