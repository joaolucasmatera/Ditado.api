using System.Diagnostics;
using System.Text;
using Ditado.Dominio.Enums;
using Ditado.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace Ditado.Aplicacao.Services;

public sealed class ExportacaoService(DitadoDbContext context)
{
    public async Task<ExportacaoResumo> EscreverRespostasCsvAsync(int atorId, IReadOnlyCollection<int> escolaIds, ExportacaoOpcoes opcoes, Stream destino, CancellationToken cancellationToken = default)
    {
        try { await ValidarEscopoAsync(atorId, escolaIds, opcoes, cancellationToken); }
        catch (OperationCanceledException)
        {
            await RegistrarAuditoriaAsync(atorId, "Cancelado", $"escolas={string.Join(',', escolaIds)}; fase=validacao");
            throw;
        }
        catch (InvalidOperationException ex)
        {
            context.AuditoriaEventos.Add(new Ditado.Dominio.Entidades.AuditoriaEvento { UsuarioId = atorId, Operacao = "ExportarRespostas", Recurso = "RespostaDitado", Resultado = "Negado", Motivo = ex.Message, DataUtc = DateTime.UtcNow });
            await context.SaveChangesAsync(cancellationToken);
            throw;
        }
        var cronometro = Stopwatch.StartNew();
        var bytes = 0;
        var linhas = 0;
        int? cursor = null;
        async Task EscreverAsync(string valor)
        {
            var dados = Encoding.UTF8.GetBytes(valor);
            bytes += dados.Length;
            if (bytes > opcoes.LimiteBytes) throw new InvalidOperationException("Exportação excedeu o limite de tamanho; reduza os filtros.");
            await destino.WriteAsync(dados, cancellationToken);
        }
        await EscreverAsync("id,ditadoId,alunoId,escolaId,dataRealizacao,nota\n");
        var query = context.RespostaDitados.AsNoTracking().Where(r => r.AplicacaoDitadoId.HasValue && r.AplicacaoDitado != null && escolaIds.Contains(r.AplicacaoDitado.EscolaId));
        if (opcoes.CursorId.HasValue) query = query.Where(r => r.Id > opcoes.CursorId.Value);
        try
        {
            await foreach (var resposta in query.OrderBy(r => r.Id).Take(opcoes.LimiteLinhas).Select(r => new { r.Id, r.DitadoId, r.AlunoId, EscolaId = r.AplicacaoDitado!.EscolaId, r.DataRealizacao, r.Nota }).AsAsyncEnumerable().WithCancellation(cancellationToken))
            {
                if (cronometro.ElapsedMilliseconds > opcoes.TempoMaximoMs) throw new InvalidOperationException("Exportação excedeu o limite de tempo; reduza os filtros.");
                await EscreverAsync($"{resposta.Id},{resposta.DitadoId},{resposta.AlunoId},{resposta.EscolaId},{resposta.DataRealizacao:O},{resposta.Nota}\n");
                linhas++;
                cursor = resposta.Id;
            }
        }
        catch (OperationCanceledException)
        {
            await RegistrarAuditoriaAsync(atorId, "Cancelado", $"escolas={string.Join(',', escolaIds)}; linhas={linhas}");
            throw;
        }
        context.AuditoriaEventos.Add(new Ditado.Dominio.Entidades.AuditoriaEvento { UsuarioId = atorId, Operacao = "ExportarRespostas", Recurso = "RespostaDitado", Resultado = "Permitido", Motivo = $"escolas={string.Join(',', escolaIds)}; linhas={linhas}; bytes={bytes}; streaming=true", DataUtc = DateTime.UtcNow });
        await context.SaveChangesAsync(cancellationToken);
        return new ExportacaoResumo(linhas, bytes, cursor);
    }

    public async Task RegistrarExportacaoCanceladaAsync(int atorId, IReadOnlyCollection<int> escolaIds, CancellationToken cancellationToken = default)
        => await RegistrarAuditoriaAsync(atorId, "Cancelado", $"escolas={string.Join(',', escolaIds)}; cliente_cancelou=true", cancellationToken);

    private async Task RegistrarAuditoriaAsync(int atorId, string resultado, string motivo, CancellationToken cancellationToken = default)
    {
        context.AuditoriaEventos.Add(new Ditado.Dominio.Entidades.AuditoriaEvento { UsuarioId = atorId, Operacao = "ExportarRespostas", Recurso = "RespostaDitado", Resultado = resultado, Motivo = motivo, DataUtc = DateTime.UtcNow });
        await context.SaveChangesAsync(cancellationToken == default ? CancellationToken.None : cancellationToken);
    }

    public async Task<ExportacaoResultado> ExportarRespostasCsvAsync(int atorId, IReadOnlyCollection<int> escolaIds, ExportacaoOpcoes opcoes, CancellationToken cancellationToken = default)
    {
        await using var memoria = new MemoryStream();
        var resumo = await EscreverRespostasCsvAsync(atorId, escolaIds, opcoes, memoria, cancellationToken);
        return new ExportacaoResultado(memoria.ToArray(), resumo.Linhas, resumo.ProximoCursor);
    }

    private async Task ValidarEscopoAsync(int atorId, IReadOnlyCollection<int> escolaIds, ExportacaoOpcoes opcoes, CancellationToken cancellationToken)
    {
        if (escolaIds.Count == 0) throw new InvalidOperationException("Informe ao menos uma escola no escopo da exportação.");
        if (opcoes.LimiteLinhas is < 1 or > 10000) throw new InvalidOperationException("Limite de linhas inválido.");
        if (opcoes.LimiteBytes is < 1024 or > 20_000_000) throw new InvalidOperationException("Limite de tamanho inválido.");
        if (opcoes.TempoMaximoMs is < 100 or > 30000) throw new InvalidOperationException("Limite de tempo inválido.");
        var escolasValidas = await context.Escolas.AsNoTracking().Where(e => escolaIds.Contains(e.Id) && e.Ativa).Select(e => e.Id).ToListAsync(cancellationToken);
        if (escolasValidas.Count != escolaIds.Distinct().Count()) throw new InvalidOperationException("Escopo contém escola inexistente ou inativa.");
        var ator = await context.Usuarios.AsNoTracking().SingleOrDefaultAsync(u => u.Id == atorId && u.Ativo, cancellationToken);
        if (ator is null || ator.Tipo is not (TipoUsuario.Administrador or TipoUsuario.Professor)) throw new InvalidOperationException("Ator não autorizado.");
        var administradorGlobal = ator.Tipo == TipoUsuario.Administrador && ator.AdministradorGeral;
        if (!administradorGlobal)
        {
            var vinculadas = await context.VinculosEscolares.Where(v => v.UsuarioId == atorId && v.Ativo && v.Escola.Ativa && escolaIds.Contains(v.EscolaId)).Select(v => v.EscolaId).ToListAsync(cancellationToken);
            if (vinculadas.Count != escolaIds.Distinct().Count()) throw new InvalidOperationException("Escopo contém escola sem vínculo ativo.");
        }
    }
}

public sealed class ExportacaoOpcoes
{
    public int? CursorId { get; init; }
    public int LimiteLinhas { get; init; } = 1000;
    public int LimiteBytes { get; init; } = 5_000_000;
    public int TempoMaximoMs { get; init; } = 3000;
}

public sealed record ExportacaoResultado(byte[] Conteudo, int Linhas, int? ProximoCursor);
public sealed record ExportacaoResumo(int Linhas, int Bytes, int? ProximoCursor);
