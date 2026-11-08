using Ditado.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ditado.Infra.Configurations;

public sealed class PublicacaoErroCatalogoConfiguration : IEntityTypeConfiguration<PublicacaoErroCatalogo>
{
    public void Configure(EntityTypeBuilder<PublicacaoErroCatalogo> builder)
    {
        builder.ToTable("PublicacoesErrosCatalogo");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Nivel).HasConversion<int>().IsRequired();
        builder.Property(p => p.Versao).IsRequired();
        builder.Property(p => p.VigenteDesde).IsRequired();
        builder.HasIndex(p => new { p.ErroCatalogoId, p.ChaveVigente }).IsUnique();
        builder.HasIndex(p => new { p.EscolaId, p.Nivel, p.EncerradaEm });
        builder.HasOne(p => p.ErroCatalogo).WithMany(e => e.Publicacoes).HasForeignKey(p => p.ErroCatalogoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Escola).WithMany().HasForeignKey(p => p.EscolaId).OnDelete(DeleteBehavior.Restrict);
    }
}
