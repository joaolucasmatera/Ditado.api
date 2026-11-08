using Ditado.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ditado.Infra.Configurations;

public class RespostaSegmentoTipoErroConfiguration : IEntityTypeConfiguration<RespostaSegmentoTipoErro>
{
    public void Configure(EntityTypeBuilder<RespostaSegmentoTipoErro> builder)
    {
        builder.ToTable("RespostaSegmentoTiposErro");
        builder.HasKey(x => new { x.RespostaSegmentoId, x.TipoErro });
        builder.Property(x => x.TipoErro).HasConversion<int>();

        builder.HasOne(x => x.RespostaSegmento)
            .WithMany(x => x.TiposErro)
            .HasForeignKey(x => x.RespostaSegmentoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
