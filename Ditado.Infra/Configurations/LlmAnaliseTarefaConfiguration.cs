using Ditado.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ditado.Infra.Configurations;

public sealed class LlmAnaliseTarefaConfiguration : IEntityTypeConfiguration<LlmAnaliseTarefa>
{
    public void Configure(EntityTypeBuilder<LlmAnaliseTarefa> builder)
    {
        builder.ToTable("LlmAnalisesTarefas");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Estado).HasConversion<int>().IsRequired();
        builder.Property(x => x.PromptVersao).IsRequired().HasMaxLength(80);
        builder.Property(x => x.Provedor).HasMaxLength(80);
        builder.Property(x => x.Modelo).HasMaxLength(200);
        builder.Property(x => x.Origem).HasMaxLength(30);
        builder.Property(x => x.UltimoErro).HasMaxLength(2000);
        builder.Property(x => x.CriadaEm).IsRequired();
        builder.Property(x => x.IniciadaEm).IsConcurrencyToken();
        builder.HasOne(x => x.RespostaSegmento).WithOne(x => x.AnaliseLlm).HasForeignKey<LlmAnaliseTarefa>(x => x.RespostaSegmentoId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.Estado, x.CriadaEm });
        builder.HasIndex(x => x.RespostaSegmentoId).IsUnique();
    }
}
