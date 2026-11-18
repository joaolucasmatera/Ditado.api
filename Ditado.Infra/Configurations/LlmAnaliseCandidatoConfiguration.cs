using Ditado.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ditado.Infra.Configurations;

public sealed class LlmAnaliseCandidatoConfiguration : IEntityTypeConfiguration<LlmAnaliseCandidato>
{
    public void Configure(EntityTypeBuilder<LlmAnaliseCandidato> builder)
    {
        builder.ToTable("LlmAnalisesCandidatos");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Codigo).IsRequired().HasMaxLength(40);
        builder.Property(x => x.Nome).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Descricao).IsRequired().HasMaxLength(10000).HasColumnType("text");
        builder.Property(x => x.Estado).HasConversion<int>().IsRequired();
        builder.Property(x => x.Confianca).HasPrecision(5, 4);
        builder.Property(x => x.Evidencia).HasMaxLength(2000);
        builder.HasOne(x => x.LlmAnaliseTarefa).WithMany(x => x.Candidatos).HasForeignKey(x => x.LlmAnaliseTarefaId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ErroCatalogo).WithMany().HasForeignKey(x => x.ErroCatalogoId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.LlmAnaliseTarefaId, x.ErroCatalogoId }).IsUnique();
    }
}
