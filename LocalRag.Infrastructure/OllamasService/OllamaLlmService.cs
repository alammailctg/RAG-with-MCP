using ProcurementAiApi.LocalRAG.Application.DTOs;
using ProcurementAiApi.LocalRAG.Application.Interfaces;
using System.Net.Http.Json;
using System.Text.Json;

namespace ProcurementAiApi.LocalRAG.Infrastructure.Ollamas;

public class OllamaLlmService : ILlmService
{
    private readonly HttpClient _httpClient;

    public OllamaLlmService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GenerateAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        var request = new
        {
            model = "qwen3:4b",
            prompt = prompt,
            stream = false,
            think = false,
            options = new
            {
                temperature = 0.1,
                num_predict = 800
            }
        };
        using var response = await _httpClient.PostAsJsonAsync(
            "/api/generate",
            request,
            cancellationToken);

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Ollama returned {(int)response.StatusCode}: {json}");
        }

        var result = JsonSerializer.Deserialize<OllamaGenerateResponse>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (result == null)
        {
            throw new InvalidOperationException(
                $"Unable to deserialize Ollama response: {json}");
        }

        if (string.IsNullOrWhiteSpace(result.Response))
        {
            throw new InvalidOperationException(
                $"Ollama returned an empty response. Thinking: {result.Thinking}");
        }

        return CleanThinking(result.Response).Trim();

        
    }

    private static string CleanThinking(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var thinkEnd = text.LastIndexOf("</think>", StringComparison.OrdinalIgnoreCase);

        if (thinkEnd >= 0)
        {
            text = text[(thinkEnd + "</think>".Length)..];
        }

        var thinkStart = text.IndexOf("<think>", StringComparison.OrdinalIgnoreCase);

        if (thinkStart >= 0)
        {
            text = text[..thinkStart];
        }

        return text.Trim();
    }
}