using Ditado.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ditado.Infra.Configurations;

public sealed class ParticipacaoAplicacaoConfiguration : IEntityTypeConfiguration<ParticipacaoAplicacao>
{
    public void Configure(EntityTypeBuilder<ParticipacaoAplicacao> builder)
    {
        builder.ToTable("ParticipacoesAplicacoes");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.ConcedidaEm).IsRequired();
        builder.HasIndex(p => new { p.AplicacaoDitadoId, p.AlunoId }).IsUnique();
        builder.HasIndex(p => new { p.AlunoId, p.RevogadaEm });
        builder.HasOne(p => p.AplicacaoDitado).WithMany(a => a.Participacoes).HasForeignKey(p => p.AplicacaoDitadoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Aluno).WithMany().HasForeignKey(p => p.AlunoId).OnDelete(DeleteBehavior.Restrict);
    }
}
