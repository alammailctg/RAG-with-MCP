using ProcurementAiApi.LocalRAG.Application.DTO;
using ProcurementAiApi.LocalRAG.Application.Interfaces;

namespace ProcurementAiApi.LocalRAG.Infrastructure.Ollamas
{
    public class OllamaLlmService : ILlmService
    {
        private readonly HttpClient _httpClient;

        public OllamaLlmService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
        {
            var request = new
            {
                model = "qwen3:4b",
                prompt,
                stream = false
            };

            var response = await _httpClient.PostAsJsonAsync("/api/generate", request, cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken: cancellationToken);

            return result?.Response ?? string.Empty;
        }
    }
}
