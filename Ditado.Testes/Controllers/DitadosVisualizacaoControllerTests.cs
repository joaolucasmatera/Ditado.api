using System.Net;
using Ditado.Aplicacao.DTOs;
using Ditado.Aplicacao.DTOs.Usuarios;
using Ditado.Dominio.Enums;
using Ditado.Testes.Infra;
using Xunit;

namespace Ditado.Testes.Controllers;

public class DitadosVisualizacaoControllerTests : TesteIntegracaoBase
{
	public DitadosVisualizacaoControllerTests(CustomWebApplicationFactory factory) : base(factory)
	{
	}

	[Fact]
	public async Task Visualizar_AdminRecebeMetadadosAudioEConteudoDasLacunas()
	{
		var ditadoId = await CriarDitadoComoAdminAsync();

		var resultado = await GetAsync<DitadoCompletoDto>($"/api/ditados/{ditadoId}/visualizar");

		Assert.NotNull(resultado);
		Assert.Equal(ditadoId, resultado.Id);
		Assert.Equal("O [cachorro] late quando ve o [gato].", TextoComMarcacoes(resultado.Segmentos));
		Assert.Equal("Texto", resultado.Segmentos[0].Tipo);
		Assert.Equal("Lacuna", resultado.Segmentos[1].Tipo);
		Assert.Equal("cachorro", resultado.Segmentos[1].Conteudo);
		Assert.Equal("gato", resultado.Segmentos[^2].Conteudo);
		Assert.StartsWith("data:audio/mpeg;base64,", resultado.AudioBase64);
		Assert.Equal("Detalhes do modal", resultado.Titulo);
	}

	[Fact]
	public async Task Visualizar_ProfessorPodeConsultarGabarito()
	{
		var ditadoId = await CriarDitadoComoAdminAsync();
		var professorToken = await CriarUsuarioEObterTokenAsync(TipoUsuario.Professor);
		AdicionarTokenAutorizacao(professorToken);

		var resultado = await GetAsync<DitadoCompletoDto>($"/api/ditados/{ditadoId}/visualizar");

		Assert.NotNull(resultado);
		Assert.Contains(resultado.Segmentos, segmento => segmento.Tipo == "Lacuna" && segmento.Conteudo == "cachorro");
	}

	[Fact]
	public async Task Visualizar_AlunoNaoPodeConsultarGabarito()
	{
		var ditadoId = await CriarDitadoComoAdminAsync();
		var alunoToken = await CriarUsuarioEObterTokenAsync(TipoUsuario.Aluno);
		AdicionarTokenAutorizacao(alunoToken);

		var resposta = await GetAsyncRaw($"/api/ditados/{ditadoId}/visualizar");

		Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
	}

	[Fact]
	public async Task Visualizar_SemAutenticacaoRetornaUnauthorized()
	{
		var resposta = await GetAsyncRaw("/api/ditados/1/visualizar");

		Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
	}

	[Fact]
	public async Task Visualizar_DitadoInexistenteRetornaNotFound()
	{
		var tokenAdmin = await ObterTokenAdminAsync();
		AdicionarTokenAutorizacao(tokenAdmin);

		var resposta = await GetAsyncRaw("/api/ditados/2147483647/visualizar");

		Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
	}

	private async Task<int> CriarDitadoComoAdminAsync()
	{
		var tokenAdmin = await ObterTokenAdminAsync();
		AdicionarTokenAutorizacao(tokenAdmin);

		var request = new CriarDitadoRequest
		{
			Titulo = "Detalhes do modal",
			Descricao = "Ditado de teste de visualizacao",
			TextoComMarcacoes = "O [cachorro] late quando ve o [gato].",
			AudioBase64 = "data:audio/mpeg;base64,SUQzAwAAAAAAJlRQRTEAAAAcAAAAU291bmRKYXk="
		};
		var criado = await PostAsync<DitadoResponse>("/api/ditados", request);

		Assert.NotNull(criado);
		return criado.Id;
	}

	private async Task<string> CriarUsuarioEObterTokenAsync(TipoUsuario tipo)
	{
		var tokenAdmin = await ObterTokenAdminAsync();
		AdicionarTokenAutorizacao(tokenAdmin);
		var login = $"visualizacao-{tipo}-{Guid.NewGuid():N}@teste.local";
		var usuario = await PostAsync<UsuarioResponse>("/api/usuarios", new CriarUsuarioRequest
		{
			Nome = $"Usuario {tipo}",
			Login = login,
			Senha = "senha123",
			Tipo = tipo
		});
		Assert.NotNull(usuario);

		RemoverTokenAutorizacao();
		var loginResponse = await PostAsync<LoginResponse>("/api/usuarios/login", new LoginRequest
		{
			Login = login,
			Senha = "senha123"
		});

		Assert.NotNull(loginResponse);
		return loginResponse.Token;
	}

	private static string TextoComMarcacoes(IEnumerable<SegmentoCompletoDto> segmentos)
	{
		return string.Concat(segmentos
			.OrderBy(segmento => segmento.Ordem)
			.Select(segmento => segmento.Tipo == "Lacuna" ? $"[{segmento.Conteudo}]" : segmento.Conteudo));
	}
}
