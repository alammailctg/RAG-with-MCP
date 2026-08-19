
using ProcurementAiApi.LocalRAG.Application.Interfaces;
using System.Net.Http.Json;

namespace ProcurementAiApi.LocalRAG.Infrastructure.OllamasService
{
    public class OllamaEmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _httpClient;

        public OllamaEmbeddingService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
        {
            var request = new
            {
                model = "nomic-embed-text",
                input = text
            };

            var response = await _httpClient.PostAsJsonAsync("/api/embed", request, cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<Application.DTOs.OllamaEmbeddingResponse>(cancellationToken: cancellationToken);

            return result!.Embeddings[0];
        }
    }
}
