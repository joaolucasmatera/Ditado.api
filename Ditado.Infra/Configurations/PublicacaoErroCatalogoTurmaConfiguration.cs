using Ditado.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ditado.Infra.Configurations;

public sealed class PublicacaoErroCatalogoTurmaConfiguration : IEntityTypeConfiguration<PublicacaoErroCatalogoTurma>
{
    public void Configure(EntityTypeBuilder<PublicacaoErroCatalogoTurma> builder)
    {
        builder.ToTable("PublicacoesErrosCatalogoTurmas");
        builder.HasKey(p => new { p.PublicacaoErroCatalogoId, p.TurmaId });
        builder.HasOne(p => p.PublicacaoErroCatalogo).WithMany(x => x.TurmasAlvo).HasForeignKey(p => p.PublicacaoErroCatalogoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(p => p.Turma).WithMany().HasForeignKey(p => p.TurmaId).OnDelete(DeleteBehavior.Restrict);
    }
}
