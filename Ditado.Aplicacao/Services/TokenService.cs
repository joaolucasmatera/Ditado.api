using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Ditado.Dominio.Entidades;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Ditado.Aplicacao.Services;

public class TokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public static TimeSpan DuracaoToken => TimeSpan.FromHours(8);

    public string GerarToken(Usuario usuario)
        => GerarToken(usuario, Guid.NewGuid());

    public string GerarToken(Usuario usuario, Guid sessaoId)
    {
        var chaveSecreta = _configuration["Jwt:ChaveSecreta"] 
            ?? throw new InvalidOperationException("Chave secreta JWT n�o configurada");
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chaveSecreta));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Jti, sessaoId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nome),
            new Claim(ClaimTypes.Role, usuario.Tipo.ToString()),
            new Claim("AdministradorGeral", usuario.AdministradorGeral.ToString().ToLowerInvariant()),
            new Claim("Login", usuario.Login)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Emissor"],
            audience: _configuration["Jwt:Audiencia"],
            claims: claims,
            expires: DateTime.UtcNow.Add(DuracaoToken),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
