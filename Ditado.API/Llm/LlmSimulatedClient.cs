namespace Ditado.Aplicacao.Llm;

internal sealed class LlmSimulatedClient : ILlmProviderClient
{
    public async Task<LlmProviderResponse> ClassificarAsync(LlmProviderOptions provider, LlmAttemptPrompt entrada,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!provider.Provider.Equals("Simulado", StringComparison.OrdinalIgnoreCase))
            return new(LlmProviderOutcome.Tecnica, Motivo: LlmMotivo.Indisponivel);

        var lacunas = entrada.Lacunas.Where(l => l.Avaliar).ToArray();
        var correspondentes = lacunas.Where(l => l.Esperado == "café" && l.Escrito == "cafe").ToArray();
        if (correspondentes.Length != 0) await Task.Delay(TimeSpan.FromSeconds(12), cancellationToken);
        return new(LlmProviderOutcome.Sucesso, new LlmAnalysisOutput
        {
            LacunasAvaliadas = lacunas.Select(l => l.Id).ToList(),
            Classificacoes = entrada.Candidatos
                .Select(c => new LlmClassificationOutput
                {
                    CandidatoId = c.Id,
                    Correspondentes = correspondentes.Select(l => new LlmGapFinding { LacunaId = l.Id, Evidencia = "Fixture sintética." }).ToList()
                })
                .ToList()
        });
    }
}
