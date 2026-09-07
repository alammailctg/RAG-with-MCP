using LocalRag.Application.Interfaces;
using ProcurementAiApi.LocalRAG.Application.DTOs;
using ProcurementAiApi.LocalRAG.Application.Interfaces;
using System.Net.Http.Json;

namespace LocalRag.Infrastructure.OllamasService;

public sealed class OllamaEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;

    public OllamaEmbeddingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<float[]> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException(
                "Text cannot be empty.",
                nameof(text));

        var request = new
        {
            model = "nomic-embed-text",
            input = text
        };

        using var response = await _httpClient.PostAsJsonAsync(
            "/api/embed",
            request,
            cancellationToken);

        var result =
            await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>(
                cancellationToken: cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Ollama embedding request failed: {(int)response.StatusCode}");
        }

        if (result?.Embeddings is null ||
            result.Embeddings.Count == 0)
        {
            throw new InvalidOperationException(
                "Ollama returned no embedding.");
        }

        return result.Embeddings[0];
    }
}