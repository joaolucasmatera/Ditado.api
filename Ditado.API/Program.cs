using System.Reflection;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Ditado.Aplicacao.Services;
using Ditado.Infra.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// CORS - TOTALMENTE ABERTO
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Swagger com JWT
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Ditado API", 
        Version = "v1",
        Description = "API para gerenciamento de ditados escolares"
    });
    
    // Usar o diret�rio do assembly em vez de AppContext.BaseDirectory
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, xmlFile);
    
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
        Console.WriteLine($"XML encontrado: {xmlPath}");
    }
    else
    {
        Console.WriteLine($"XML N�O encontrado em: {xmlPath}");
        Console.WriteLine($"   Procurando em: {Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}");
        Console.WriteLine($"   Arquivos dispon�veis: {string.Join(", ", Directory.GetFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "*.xml"))}");
    }
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando o esquema Bearer. Exemplo: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Banco de dados
builder.Services.AddDbContext<DitadoDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("ConexaoPadrao"),
        new MySqlServerVersion(new Version(8, 0, 21))
    ));

// JWT Authentication
var chaveSecreta = builder.Configuration["Jwt:ChaveSecreta"] ?? throw new InvalidOperationException("Jwt:ChaveSecreta n�o configurada");
var key = Encoding.UTF8.GetBytes(chaveSecreta);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Emissor"],
        ValidAudience = builder.Configuration["Jwt:Audiencia"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            if (!Guid.TryParse(jti, out var sessaoId))
            {
                context.Fail("Sessão ausente.");
                return;
            }

            var db = context.HttpContext.RequestServices.GetRequiredService<DitadoDbContext>();
            var sessao = await db.SessoesUsuarios.AsNoTracking().SingleOrDefaultAsync(s => s.Id == sessaoId);
            if (sessao == null || sessao.RevogadaEm.HasValue || sessao.ExpiraEm <= DateTime.UtcNow)
                context.Fail("Sessão revogada ou expirada.");
        }
    };
});

builder.Services.AddAuthorization();

// Application Services
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<DitadoService>();
builder.Services.AddScoped<TurmaService>();
builder.Services.AddScoped<CategoriaService>();
builder.Services.AddScoped<MarketplaceService>();
builder.Services.AddScoped<VisibilidadeService>();
builder.Services.AddScoped<AlunoService>();
builder.Services.AddScoped<ProfessorService>();
builder.Services.AddScoped<PasswordHasher>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AudioRetencaoService>();
builder.Services.AddScoped<ExportacaoService>();

var app = builder.Build();

// APLICAR MIGRATIONS AUTOMATICAMENTE (APENAS EM PRODU��O - N�O EM DEVELOPMENT NEM TESTING)
if (!app.Environment.IsDevelopment() && !app.Environment.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase))
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        try
        {
            logger.LogInformation("Aplicando migrations...");
            var context = services.GetRequiredService<DitadoDbContext>();
            context.Database.Migrate();
            logger.LogInformation("Migrations aplicadas com sucesso!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao aplicar migrations: {Message}", ex.Message);
            throw;
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
 
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseStaticFiles();

// USAR CORS - DEVE VIR ANTES DE UseAuthentication/UseAuthorization
app.UseCors("AllowAll");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Tornar Program acess�vel para testes
public partial class Program { }
