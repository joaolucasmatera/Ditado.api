using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Ditado.Aplicacao.DTOs.Usuarios;
using Ditado.Dominio.Entidades;
using Ditado.Dominio.Enums;
using Ditado.Infra.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ditado.Testes.Infra;

public abstract class TesteIntegracaoBase : IClassFixture<CustomWebApplicationFactory>
{
    protected readonly HttpClient Client;
    private readonly CustomWebApplicationFactory _factory;
    private string? _tokenAdmin; // Cache do token admin
    
    protected readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected TesteIntegracaoBase(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        Client = factory.CreateClient();
    }

    protected async Task<int> GarantirEscolaEVinculoAsync(int usuarioId, PapelEscolar papel = PapelEscolar.Professor)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DitadoDbContext>();
        var escola = await context.Escolas.FirstOrDefaultAsync(e => e.Codigo == "TESTE-GLOBAL");
        if (escola == null)
        {
            escola = new Escola { Nome = "Escola de Testes", Codigo = "TESTE-GLOBAL", Ativa = true };
            context.Escolas.Add(escola);
            await context.SaveChangesAsync();
        }
        if (!await context.VinculosEscolares.AnyAsync(v => v.UsuarioId == usuarioId && v.EscolaId == escola.Id && v.Ativo))
        {
            context.VinculosEscolares.Add(new VinculoEscolar { UsuarioId = usuarioId, EscolaId = escola.Id, Papel = papel, Ativo = true });
            await context.SaveChangesAsync();
        }
        return escola.Id;
    }

    protected async Task<(int Id, string Token)> CriarUsuarioSinteticoAsync(TipoUsuario tipo)
    {
        AdicionarTokenAutorizacao(await ObterTokenAdminAsync());
        var login = $"teste-{Guid.NewGuid():N}@teste.invalid";
        var senha = Guid.NewGuid().ToString("N");
        var usuario = Assert.IsType<UsuarioResponse>(await PostAsync<UsuarioResponse>("/api/usuarios",
            new CriarUsuarioRequest { Nome = "Usuário de teste", Login = login, Senha = senha, Tipo = tipo }));
        var auth = Assert.IsType<LoginResponse>(await PostAsync<LoginResponse>("/api/usuarios/login",
            new LoginRequest { Login = login, Senha = senha }));
        return (usuario.Id, auth.Token);
    }

    protected async Task<List<int>> GarantirRegrasPublicasAsync(params TipoErro[] regras)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DitadoDbContext>();
        var autorId = await context.Usuarios.Where(u => u.AdministradorGeral).Select(u => u.Id).FirstAsync();
        var itens = new List<ErroCatalogo>();
        foreach (var regra in regras)
        {
            var erro = await context.ErrosCatalogo.SingleOrDefaultAsync(e => e.RegraClassificacao == regra);
            if (erro == null)
            {
                erro = new ErroCatalogo { Codigo = $"TESTE-{regra}", Nome = $"Regra {regra}", AutorId = autorId, RegraClassificacao = regra };
                erro.Publicacoes.Add(new PublicacaoErroCatalogo { Nivel = NivelVisibilidade.PublicoGeral, ChaveVigente = 1, Versao = 1, CriadaPorId = autorId });
                context.ErrosCatalogo.Add(erro);
            }
            itens.Add(erro);
        }
        await context.SaveChangesAsync();
        return itens.Select(e => e.Id).ToList();
    }

    /// <summary>
    /// Obt�m token do admin tempor�rio (criado no seed da factory)
    /// </summary>
    protected async Task<string> ObterTokenAdminAsync()
    {
        if (_tokenAdmin != null)
            return _tokenAdmin;

        var loginRequest = new LoginRequest
        {
            Login = "admin@admin.com",
            Senha = "admin"
        };

        var loginResponse = await PostAsync<LoginResponse>("/api/usuarios/login", loginRequest);

        if (loginResponse?.Token == null)
            throw new InvalidOperationException("N�o foi poss�vel obter token de admin. Certifique-se de que o usu�rio admin@admin.com existe.");

        _tokenAdmin = loginResponse.Token;
        return _tokenAdmin;
    }

    protected async Task<TResponse?> PostAsync<TResponse>(string url, object data)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(data, JsonOptions),
            Encoding.UTF8,
            "application/json"
        );

        var response = await Client.PostAsync(url, content);
        
        if (!response.IsSuccessStatusCode)
            return default;

        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TResponse>(responseContent, JsonOptions);
    }

    protected async Task<TResponse?> PutAsync<TResponse>(string url, object data)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(data, JsonOptions),
            Encoding.UTF8,
            "application/json"
        );

        var response = await Client.PutAsync(url, content);
        
        if (!response.IsSuccessStatusCode)
            return default;

        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TResponse>(responseContent, JsonOptions);
    }

    protected async Task<TResponse?> GetAsync<TResponse>(string url)
    {
        var response = await Client.GetAsync(url);
        
        if (!response.IsSuccessStatusCode)
            return default;

        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TResponse>(responseContent, JsonOptions);
    }

    protected async Task<HttpResponseMessage> DeleteAsync(string url)
    {
        return await Client.DeleteAsync(url);
    }

    protected async Task<HttpResponseMessage> PostAsyncRaw(string url, object data)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(data, JsonOptions),
            Encoding.UTF8,
            "application/json"
        );

        return await Client.PostAsync(url, content);
    }

    protected async Task<HttpResponseMessage> GetAsyncRaw(string url)
    {
        return await Client.GetAsync(url);
    }

    protected void AdicionarTokenAutorizacao(string token)
    {
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    protected void RemoverTokenAutorizacao()
    {
        Client.DefaultRequestHeaders.Authorization = null;
    }
}
