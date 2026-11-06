using Ditado.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ditado.Infra.Configurations;

public class RespostaSegmentoConfiguration : IEntityTypeConfiguration<RespostaSegmento>
{
    public void Configure(EntityTypeBuilder<RespostaSegmento> builder)
    {
        builder.ToTable("RespostaSegmentos");
        
        builder.HasKey(rs => rs.Id);
        
        builder.Property(rs => rs.RespostaFornecida)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(rs => rs.RespostaEsperada)
            .HasMaxLength(500);
        
        builder.Property(rs => rs.Correto)
            .IsRequired();
        
        builder.Property(rs => rs.TipoErro)
            .HasConversion<int?>();

        builder.Property(rs => rs.MetodoClassificacao)
            .HasConversion<int?>();

        builder.Property(rs => rs.EstadoClassificacao)
            .HasConversion<int>()
            .IsRequired();
        
        builder.HasOne(rs => rs.Segmento)
            .WithMany(s => s.Respostas)
            .HasForeignKey(rs => rs.SegmentoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
