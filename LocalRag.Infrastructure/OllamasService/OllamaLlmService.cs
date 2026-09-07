using ProcurementAiApi.LocalRAG.Application.DTOs;
using ProcurementAiApi.LocalRAG.Application.Interfaces;
using System.Net.Http.Json;
using System.Text.Json;

namespace ProcurementAiApi.LocalRAG.Infrastructure.Ollamas;

public sealed class OllamaLlmService : ILlmService
{
    private readonly HttpClient _httpClient;

    public OllamaLlmService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GenerateAsync (string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException(
                "Prompt cannot be empty.",
                nameof(prompt));

        var request = new
        {
            model = "qwen3:4b",
            prompt,
            stream = false,
            think = false,
            options = new
            {
                temperature = 0.1,
                num_predict = 500
            }
        };

        using var response = await _httpClient.PostAsJsonAsync(
            "/api/generate",
            request,
            cancellationToken);

        var json = await response.Content.ReadAsStringAsync(
            cancellationToken);

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

        if (result is null)
            throw new InvalidOperationException(
                $"Unable to deserialize Ollama response: {json}");

        if (string.IsNullOrWhiteSpace(result.Response))
            throw new InvalidOperationException(
                $"Ollama returned an empty response. Thinking: {result.Thinking}");

        return CleanThinking(result.Response);
    }

    private static string CleanThinking(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var thinkEnd = text.LastIndexOf(
            "</think>",
            StringComparison.OrdinalIgnoreCase);

        if (thinkEnd >= 0)
        {
            text = text[(thinkEnd + "</think>".Length)..];
        }

        var thinkStart = text.IndexOf(
            "<think>",
            StringComparison.OrdinalIgnoreCase);

        if (thinkStart >= 0)
        {
            text = text[..thinkStart];
        }

        return text.Trim();
    }

    public async Task<string> GenerateJsonAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException(
                "Prompt cannot be empty.",
                nameof(prompt));

        var request = new
        {
            model = "qwen3:4b",
            prompt,
            stream = false,
            think = false,
            format = "json",
            options = new
            {
                temperature = 0.0,
                num_predict = 100
            }
        };

        using var response = await _httpClient.PostAsJsonAsync(
            "/api/generate",
            request,
            cancellationToken);

        var json = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Ollama JSON request failed: {(int)response.StatusCode}: {json}");
        }

        var result = JsonSerializer.Deserialize<OllamaGenerateResponse>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (result is null)
            throw new InvalidOperationException(
                $"Unable to deserialize Ollama response: {json}");

        if (string.IsNullOrWhiteSpace(result.Response))
            throw new InvalidOperationException(
                $"Ollama returned empty JSON response. Thinking: {result.Thinking}");

        return result.Response.Trim();
    }
}