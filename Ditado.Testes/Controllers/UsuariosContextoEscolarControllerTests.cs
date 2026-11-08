using System.Security.Claims;
using Ditado.API.Controllers;
using Ditado.Aplicacao.Services;
using Ditado.Dominio.Entidades;
using Ditado.Dominio.Enums;
using Ditado.Infra.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Ditado.Testes.Controllers;

public sealed class UsuariosContextoEscolarControllerTests : IDisposable
{
    private readonly DitadoDbContext _context;
    private readonly UsuarioService _service;
    private readonly int _usuarioId;

    public UsuariosContextoEscolarControllerTests()
    {
        var options = new DbContextOptionsBuilder<DitadoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new DitadoDbContext(options);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:ChaveSecreta"] = "chave-de-teste-com-tamanho-suficiente-123456",
                ["Jwt:Emissor"] = "teste",
                ["Jwt:Audiencia"] = "teste"
            })
            .Build();
        _service = new UsuarioService(_context, new PasswordHasher(), new TokenService(configuration));

        var usuario = new Usuario { Nome = "Usuário", Login = "usuario@teste", Tipo = TipoUsuario.Professor, Ativo = true, SenhaHash = "hash" };
        _context.Usuarios.Add(usuario);
        _context.SaveChanges();
        _usuarioId = usuario.Id;
    }

    [Fact]
    public async Task ListarContexto_SemHeader_DeveRetornarLista()
    {
        await AdicionarVinculoAsync(7, true);
        var controller = CriarController();

        var resultado = await controller.ListarContextoEscolar();

        var ok = Assert.IsType<OkObjectResult>(resultado.Result);
        var contextos = Assert.IsAssignableFrom<IReadOnlyList<ContextoEscolarResponse>>(ok.Value);
        Assert.Single(contextos);
    }

    [Fact]
    public async Task ListarContexto_ComHeaderInvalido_DeveRetornar400EAuditarNegado()
    {
        var controller = CriarController();
        controller.HttpContext.Request.Headers["X-Escola-Id"] = "abc";

        var resultado = await controller.ListarContextoEscolar();

        var erro = Assert.IsType<BadRequestObjectResult>(resultado.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, erro.StatusCode);
        Assert.Equal("Negado", await _context.AuditoriaEventos.Select(a => a.Resultado).SingleAsync());
    }

    [Fact]
    public async Task ListarContexto_SemVinculo_DeveRetornar403EAuditarNegado()
    {
        await AdicionarVinculoAsync(7, false);
        var controller = CriarController();
        controller.HttpContext.Request.Headers["X-Escola-Id"] = "7";

        var resultado = await controller.ListarContextoEscolar();

        var erro = Assert.IsType<ObjectResult>(resultado.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, erro.StatusCode);
        Assert.Equal("Negado", await _context.AuditoriaEventos.Select(a => a.Resultado).SingleAsync());
    }

    private UsuariosController CriarController()
    {
        var controller = new UsuariosController(_service)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, _usuarioId.ToString())
        }, "Teste"));
        return controller;
    }

    private async Task AdicionarVinculoAsync(int escolaId, bool ativo)
    {
        _context.Escolas.Add(new Escola { Id = escolaId, Nome = "Escola", Codigo = $"ESC-{Guid.NewGuid()}", Ativa = true });
        await _context.SaveChangesAsync();
        _context.VinculosEscolares.Add(new VinculoEscolar { EscolaId = escolaId, UsuarioId = _usuarioId, Papel = PapelEscolar.Professor, Ativo = ativo });
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
