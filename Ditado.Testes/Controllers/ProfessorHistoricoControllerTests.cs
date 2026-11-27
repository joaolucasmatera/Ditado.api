using System.Net;
using System.Text.Json;
using Ditado.Aplicacao.DTOs;
using Ditado.Aplicacao.DTOs.Marketplace;
using Ditado.Aplicacao.DTOs.Professores;
using Ditado.Dominio.Entidades;
using Ditado.Dominio.Enums;
using Ditado.Infra.Data;
using Ditado.Testes.Infra;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ditado.Testes.Controllers;

public sealed class ProfessorHistoricoControllerTests : TesteIntegracaoBase
{
    private readonly CustomWebApplicationFactory _factory;

    public ProfessorHistoricoControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProfessorConsultaSomenteAsTentativasDaSuaTurma()
    {
        var professor = await CriarUsuarioSinteticoAsync(TipoUsuario.Professor);
        var aluno = await CriarUsuarioSinteticoAsync(TipoUsuario.Aluno);
        var professorExterno = await CriarUsuarioSinteticoAsync(TipoUsuario.Professor);
        var escolaId = await GarantirEscolaEVinculoAsync(professor.Id);
        await GarantirEscolaEVinculoAsync(aluno.Id, PapelEscolar.Aluno);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DitadoDbContext>();
        db.VinculosEscolares.Add(new() { Escola = new() { Codigo = "OUTRA-HISTORICO", Nome = "Outra escola" },
            UsuarioId = professorExterno.Id, Papel = PapelEscolar.Professor });
        var cadastroAluno = await db.Usuarios.FindAsync(aluno.Id);
        var turma = new Turma { EscolaId = escolaId, ProfessorResponsavelId = professor.Id, Nome = "Turma do histórico" };
        var outraTurma = new Turma { EscolaId = escolaId, ProfessorResponsavelId = professor.Id, Nome = "Outra turma" };
        turma.Alunos.Add(cadastroAluno!);
        outraTurma.Alunos.Add(cadastroAluno!);
        var ditado = new Dominio.Entidades.Ditado { Titulo = "Histórico sintético", AutorId = professor.Id, AudioLeitura = [1] };
        var segmento = new DitadoSegmento { Ditado = ditado, Tipo = TipoSegmento.Lacuna, Ordem = 1, Conteudo = "café" };
        var segundoSegmento = new DitadoSegmento { Ditado = ditado, Tipo = TipoSegmento.Lacuna, Ordem = 2, Conteudo = "quente" };
        var terceiroSegmento = new DitadoSegmento { Ditado = ditado, Tipo = TipoSegmento.Lacuna, Ordem = 3, Conteudo = "chegaram" };
        var regra = new ErroCatalogo { Codigo = "HIST-CODIGO", Nome = "Acentuação", AutorId = professor.Id,
            CategoriaPedagogicaCodigo = "HIBRIDO_ACENTUACAO" };
        var analise = new ErroCatalogo { Codigo = "HIST-LLM", Nome = "Definição docente", AutorId = professor.Id,
            CategoriaPedagogicaCodigo = "FONOLOGIA_SILABA" };
        var dataBase = new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);
        var aplicacao = new AplicacaoDitado { Ditado = ditado, Turma = turma, EscolaId = escolaId, DataAtribuicao = dataBase };
        aplicacao.Participacoes.Add(new() { AlunoId = aluno.Id, ConcedidaEm = dataBase, RevogadaEm = dataBase.AddDays(3) });
        var outraAplicacao = new AplicacaoDitado { Ditado = ditado, Turma = outraTurma, EscolaId = escolaId };
        outraAplicacao.Participacoes.Add(new() { AlunoId = aluno.Id });
        db.AddRange(segmento, segundoSegmento, terceiroSegmento, regra, analise, aplicacao, outraAplicacao, new TurmaDitado
        {
            Ditado = ditado, Turma = turma, DataAtribuicao = dataBase, DataLimite = dataBase.AddDays(10)
        });
        await db.SaveChangesAsync();

        var antiga = NovaResposta(dataBase.AddDays(1), 0, null);
        antiga.RespostasSegmentos.Single().TipoErro = TipoErro.Acentuacao;
        var recente = NovaResposta(dataBase.AddDays(2), 50, aplicacao);
        var revisada = recente.RespostasSegmentos.Single();
        revisada.RespostaEsperada = "Café";
        revisada.TipoErro = TipoErro.Acentuacao;
        revisada.TiposErro.Add(new() { TipoErro = TipoErro.Acentuacao, ErroCatalogoId = regra.Id });
        revisada.AnaliseLlm = new()
        {
            Estado = LlmAnaliseEstado.Revisao,
            Modelo = "modelo-efetivo-do-teste",
            Candidatos = [new() { ErroCatalogoId = analise.Id, Codigo = analise.Codigo,
                Nome = analise.Nome, Estado = LlmCandidatoEstado.Correspondente }]
        };
        recente.RespostasSegmentos.Add(new()
        {
            SegmentoId = segundoSegmento.Id, RespostaFornecida = "quenti",
            AnaliseLlm = new() { Estado = LlmAnaliseEstado.Processando,
                Candidatos = [new() { ErroCatalogoId = analise.Id, Estado = LlmCandidatoEstado.NaoAvaliado }] }
        });
        recente.RespostasSegmentos.Add(new()
        {
            SegmentoId = terceiroSegmento.Id, RespostaFornecida = "chegou",
            TiposErro = [new() { TipoErro = TipoErro.ConcordanciaFlexao,
                CategoriaPedagogicaCodigo = "NAO_ORTOGRAFICO", CategoriaPedagogicaNome = "Concordância verbal" }],
            AnaliseLlm = new() { Estado = LlmAnaliseEstado.Concluida,
                Candidatos = [new() { ErroCatalogoId = analise.Id, Estado = LlmCandidatoEstado.NaoCorrespondente }] }
        });
        db.AddRange(antiga, recente, NovaResposta(dataBase.AddDays(3), 100, outraAplicacao));
        await db.SaveChangesAsync();
        var url = $"/api/Professores/turmas/{turma.Id}/alunos/{aluno.Id}/ditados/{ditado.Id}/tentativas";

        AdicionarTokenAutorizacao(professor.Token);
        var historico = Assert.IsType<List<TentativaAlunoProfessorDto>>(await GetAsync<List<TentativaAlunoProfessorDto>>(url));
        Assert.Equal(new[] { recente.Id, antiga.Id }, historico.Select(t => t.Resultado.RespostaDitadoId));
        Assert.Equal(new[] { dataBase.AddDays(2), dataBase.AddDays(1) }, historico.Select(t => t.DataRealizacao));
        Assert.Equal(new[] { 50m, 0m }, historico.Select(t => t.Resultado.Nota));
        Assert.Equal(3, historico[0].Resultado.Detalhes.Count);
        var detalhe = historico[0].Resultado.Detalhes[0];
        Assert.Equal(("Café", "cafe", "Revisao"), (detalhe.RespostaEsperada, detalhe.RespostaFornecida, detalhe.EstadoAnaliseLlm));
        Assert.Equal(new[] { "Codigo", "LLM" }, detalhe.Classificacoes.Select(c => c.Origem));
        Assert.All(detalhe.Classificacoes, c => Assert.Null(c.CategoriaPedagogica));
        Assert.Null(Assert.Single(detalhe.Classificacoes, c => c.Origem == "Codigo").ModeloLlm);
        Assert.Equal("modelo-efetivo-do-teste", Assert.Single(detalhe.Classificacoes, c => c.Origem == "LLM").ModeloLlm);
        var categoriaGravada = new CategoriaPedagogicaDto("NAO_ORTOGRAFICO", "Concordância verbal", null);
        var achadoSemVinculo = Assert.Single(historico[0].Resultado.Detalhes[2].Classificacoes);
        Assert.Equal(("Codigo", (int?)TipoErro.ConcordanciaFlexao, categoriaGravada),
            (achadoSemVinculo.Origem, achadoSemVinculo.TipoErro, achadoSemVinculo.CategoriaPedagogica));
        var legado = Assert.Single(historico[1].Resultado.Detalhes);
        Assert.Equal("café", legado.RespostaEsperada);
        Assert.Single(legado.TiposErro);
        Assert.Empty(legado.Classificacoes);

        var resumo = Assert.IsType<DitadoAtribuidoDetalheDto>(
            await GetAsync<DitadoAtribuidoDetalheDto>($"/api/Professores/turmas/{turma.Id}/ditados/{ditado.Id}/resultados"));
        Assert.Equal(50m, Assert.Single(resumo.Alunos).Nota);
        Assert.Equal(2, Assert.Single(resumo.Alunos).NumeroTentativas);
        Assert.Equal(50m, resumo.NotaMedia);
        Assert.Equal(3, resumo.ErrosPorTipo.Count);
        var categoriaAusente = Assert.Single(resumo.ErrosPorCategoria, g => g.CategoriaPedagogica == null);
        Assert.Equal(2, categoriaAusente.Quantidade);
        Assert.Equal(1, Assert.Single(resumo.ErrosPorCategoria, g => g.CategoriaPedagogica == categoriaGravada).Quantidade);
        var codigo = Assert.Single(resumo.ErrosPorTipo, e => e.Origem == "Codigo" && e.ErroCatalogoId == regra.Id);
        var llm = Assert.Single(resumo.ErrosPorTipo, e => e.Origem == "LLM");
        Assert.Equal(((int)TipoErro.Acentuacao, regra.Id, 1), (codigo.TipoErroId, codigo.ErroCatalogoId!.Value, codigo.Quantidade));
        Assert.Equal((0, analise.Id, 1), (llm.TipoErroId, llm.ErroCatalogoId!.Value, llm.Quantidade));
        Assert.All(resumo.ErrosPorPalavra, palavra => Assert.Equal(1, palavra.QuantidadeErros));
        Assert.Equal(1, resumo.LacunasIncorretasSemClassificacao);
        Assert.Equal(1, resumo.LacunasIncorretasAguardandoLlm);

        AdicionarTokenAutorizacao(professorExterno.Token);
        Assert.Equal(HttpStatusCode.NotFound, (await GetAsyncRaw(url)).StatusCode);
        AdicionarTokenAutorizacao(aluno.Token);
        Assert.Equal(HttpStatusCode.Forbidden, (await GetAsyncRaw(url)).StatusCode);
        var resultadoAluno = Assert.IsType<ResultadoDitadoResponse>(await GetAsync<ResultadoDitadoResponse>(
            $"/api/Alunos/tentativas/{recente.Id}/resultado"));
        Assert.Equal(categoriaGravada, Assert.Single(resultadoAluno.Detalhes[2].Classificacoes).CategoriaPedagogica);
        RemoverTokenAutorizacao();
        Assert.Equal(HttpStatusCode.Unauthorized, (await GetAsyncRaw(url)).StatusCode);
        using var swagger = JsonDocument.Parse(await Client.GetStringAsync("/swagger/v1/swagger.json"));
        var operacao = swagger.RootElement.GetProperty("paths")
            .GetProperty("/api/Professores/turmas/{turmaId}/alunos/{alunoId}/ditados/{ditadoId}/tentativas").GetProperty("get");
        Assert.Equal(3, operacao.GetProperty("parameters").GetArrayLength());
        Assert.Contains("categoriaPedagogica", operacao.GetProperty("description").GetString());
        Assert.True(operacao.GetProperty("responses").TryGetProperty("404", out _));
        var schemas = swagger.RootElement.GetProperty("components").GetProperty("schemas");
        Assert.True(schemas.GetProperty("ClassificacaoCatalogoDto").GetProperty("properties")
            .TryGetProperty("modeloLlm", out _));
        Assert.True(schemas.GetProperty("AlunoResultadoDto").GetProperty("properties")
            .TryGetProperty("numeroTentativas", out _));
        Assert.True(schemas.GetProperty("ErrosPorTipoDto").GetProperty("properties").TryGetProperty("origem", out _));
        Assert.True(schemas.GetProperty("DitadoAtribuidoDetalheDto").GetProperty("properties")
            .TryGetProperty("lacunasIncorretasAguardandoLlm", out _));
        Assert.True(schemas.GetProperty("DitadoAtribuidoDetalheDto").GetProperty("properties")
            .TryGetProperty("errosPorCategoria", out _));
        var exportacaoSwagger = swagger.RootElement.GetProperty("paths").GetProperty("/api/exportacoes/respostas").GetProperty("get");
        Assert.Contains("categoriaPedagogicaCodigo", exportacaoSwagger.GetProperty("description").GetString());
        Assert.True(exportacaoSwagger.GetProperty("responses").GetProperty("200").GetProperty("content").TryGetProperty("text/csv", out _));

        db.TurmaDitados.Add(new() { TurmaId = outraTurma.Id, DitadoId = ditado.Id,
            DataAtribuicao = dataBase, DataLimite = dataBase.AddDays(10) });
        await db.SaveChangesAsync();
        AdicionarTokenAutorizacao(professor.Token);
        var semAmbigua = Assert.IsType<List<TentativaAlunoProfessorDto>>(await GetAsync<List<TentativaAlunoProfessorDto>>(url));
        Assert.Equal(recente.Id, Assert.Single(semAmbigua).Resultado.RespostaDitadoId);

        RespostaDitado NovaResposta(DateTime data, decimal nota, AplicacaoDitado? origem) => new()
        {
            DitadoId = ditado.Id, AlunoId = aluno.Id, AplicacaoDitadoId = origem?.Id, EscolaId = origem?.EscolaId,
            DataRealizacao = data, Nota = nota,
            RespostasSegmentos = [new() { SegmentoId = segmento.Id, RespostaFornecida = "cafe" }]
        };
    }
}
