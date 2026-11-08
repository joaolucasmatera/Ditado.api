using Ditado.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ditado.Infra.Configurations;

public sealed class ParticipacaoTurmaConfiguration : IEntityTypeConfiguration<ParticipacaoTurma>
{
    public void Configure(EntityTypeBuilder<ParticipacaoTurma> builder)
    {
        builder.ToTable("ParticipacoesTurmas");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.DataEntrada).IsRequired();
        builder.HasIndex(p => new { p.TurmaId, p.AlunoId }).IsUnique();
        builder.HasIndex(p => new { p.AlunoId, p.Ativa });
        builder.HasOne(p => p.Turma).WithMany().HasForeignKey(p => p.TurmaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Aluno).WithMany().HasForeignKey(p => p.AlunoId).OnDelete(DeleteBehavior.Restrict);
    }
}
