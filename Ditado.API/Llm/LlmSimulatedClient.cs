namespace Ditado.Aplicacao.Llm;

internal sealed class LlmSimulatedClient : ILlmProviderClient
{
    public async Task<LlmProviderResponse> ClassificarAsync(LlmProviderOptions provider, string respostaEsperada,
        string respostaFornecida, IReadOnlyList<LlmCandidatePrompt> candidatos, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!provider.Provider.Equals("Simulado", StringComparison.OrdinalIgnoreCase))
            return new(LlmProviderOutcome.Tecnica, Motivo: LlmMotivo.Indisponivel);

        var corresponde = respostaEsperada == "café" && respostaFornecida == "cafe";
        if (corresponde) await Task.Delay(TimeSpan.FromSeconds(12), cancellationToken);
        return new(LlmProviderOutcome.Sucesso, new LlmAnalysisOutput
        {
            Classificacoes = candidatos
                .Where(_ => corresponde)
                .Select(c => new LlmClassificationOutput { CandidatoId = c.Id, Estado = "correspondente", Evidencia = "Fixture sintética." })
                .ToList()
        });
    }
}
