using System.Diagnostics;
using System.Data;
using Ditado.Dominio.Entidades;
using Ditado.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace Ditado.Aplicacao.Services;

public sealed class AudioRetencaoService(DitadoDbContext context)
{
    public async Task<IReadOnlyList<AudioLimpezaCandidato>> ListarCandidatosAsync(int atorId, DateTime agoraUtc, CancellationToken cancellationToken = default)
    {
        var ator = await context.Usuarios.AsNoTracking().SingleOrDefaultAsync(u => u.Id == atorId && u.Ativo && u.Tipo == Ditado.Dominio.Enums.TipoUsuario.Administrador && u.AdministradorGeral, cancellationToken);
        if (ator is null) throw new InvalidOperationException("Somente Administrador Geral/Técnico pode listar limpeza de áudio.");
        var limite = agoraUtc.AddHours(-24);
        var blobs = context.Database.IsRelational()
            ? await context.Database.SqlQuery<AudioBlobInfo>($"""
            SELECT Id AS DitadoId, DataCriacao, OCTET_LENGTH(AudioLeitura) AS TamanhoBytes
            FROM Ditados
            WHERE AudioLeitura IS NOT NULL
              AND OCTET_LENGTH(AudioLeitura) > 0
              AND DataCriacao <= {limite}
            """).ToListAsync(cancellationToken)
            : await context.Ditados.AsNoTracking().Where(d => d.AudioLeitura != null && d.AudioLeitura.Length > 0 && d.DataCriacao <= limite)
                .Select(d => new AudioBlobInfo(d.Id, d.DataCriacao, d.AudioLeitura.Length)).ToListAsync(cancellationToken);
        var ids = blobs.Select(b => b.DitadoId).ToArray();
        var bloqueados = await context.AplicacoesDitados.Where(a => ids.Contains(a.DitadoId)).Select(a => a.DitadoId)
            .Concat(context.RespostaDitados.Where(r => ids.Contains(r.DitadoId)).Select(r => r.DitadoId))
            .Concat(context.TurmaDitados.Where(t => ids.Contains(t.DitadoId)).Select(t => t.DitadoId))
            .Distinct().ToListAsync(cancellationToken);
        var candidatos = blobs.Where(b => !bloqueados.Contains(b.DitadoId))
            .Select(b => new AudioLimpezaCandidato(b.DitadoId, b.DataCriacao, b.TamanhoBytes)).ToList();
        context.AuditoriaEventos.Add(new AuditoriaEvento { UsuarioId = atorId, Operacao = "LimparAudioNaoAtribuido", Recurso = "Ditado", Resultado = "CandidatosSelecionados", Motivo = $"fase=selecao; candidatos={candidatos.Count}", DataUtc = agoraUtc });
        await context.SaveChangesAsync(cancellationToken);
        return candidatos;
    }

    public async Task<int> ConfirmarLimpezaAsync(IReadOnlyCollection<int> ids, int atorId, DateTime agoraUtc, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0) return 0;
        var ator = await context.Usuarios.AsNoTracking().SingleOrDefaultAsync(u => u.Id == atorId && u.Ativo && u.Tipo == Ditado.Dominio.Enums.TipoUsuario.Administrador && u.AdministradorGeral, cancellationToken);
        if (ator is null) throw new InvalidOperationException("Somente Administrador Geral/Técnico pode confirmar limpeza de áudio.");
        var limite = agoraUtc.AddHours(-24);
        await using var transaction = context.Database.IsRelational() ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken) : null;
        var blobIds = context.Database.IsRelational()
            ? await context.Database.SqlQuery<int>($"""
            SELECT Id AS Value
            FROM Ditados
            WHERE AudioLeitura IS NOT NULL
              AND OCTET_LENGTH(AudioLeitura) > 0
              AND DataCriacao <= {limite}
            """).Where(id => ids.Contains(id)).ToListAsync(cancellationToken)
            : await context.Ditados.Where(d => ids.Contains(d.Id) && d.AudioLeitura != null && d.AudioLeitura.Length > 0 && d.DataCriacao <= limite)
                .Select(d => d.Id).ToListAsync(cancellationToken);
        var ditados = await context.Ditados.Where(d => blobIds.Contains(d.Id)
            && !context.AplicacoesDitados.Any(a => a.DitadoId == d.Id)
            && !context.RespostaDitados.Any(r => r.DitadoId == d.Id)
            && !context.TurmaDitados.Any(t => t.DitadoId == d.Id)).ToListAsync(cancellationToken);
        foreach (var ditado in ditados) ditado.AudioLeitura = Array.Empty<byte>();
        context.AuditoriaEventos.Add(new AuditoriaEvento
        {
            UsuarioId = atorId,
            Operacao = "LimparAudioNaoAtribuido",
            Recurso = "Ditado",
            Resultado = "Permitido",
            Motivo = $"fase=confirmacao; ids={string.Join(',', ids)}; removidos={ditados.Count}; preservados={ids.Count - ditados.Count}",
            DataUtc = agoraUtc
        });
        await context.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return ditados.Count;
    }
}

public sealed record AudioLimpezaCandidato(int DitadoId, DateTime CriadoEmUtc, int TamanhoBytes);
public sealed record AudioBlobInfo(int DitadoId, DateTime DataCriacao, int TamanhoBytes);
