using LocalRag.Domain.RepositoryInterfaces;
using ModelContextProtocol.Server;
using ProcurementAiApi.LocalRAG.Application.Interfaces;
using System.ComponentModel;

namespace LocalRag.Infrastructure.MCPServers;

[McpServerToolType]
public sealed class VectorSearchTools
{
    private readonly IEmbeddingService _embedding;
    private readonly IVectorRepository _repository;

    public VectorSearchTools(
        IEmbeddingService embedding,
        IVectorRepository repository)
    {
        _embedding = embedding;
        _repository = repository;
    }

    [McpServerTool]
    [Description("Search company ERP documents using semantic similarity.")]
    public async Task<string> SearchDocuments(
        string question,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question))
            throw new ArgumentException(
                "Question cannot be empty.",
                nameof(question));

        var vector =
            await _embedding.GenerateEmbeddingAsync(
                question,
                cancellationToken);

        var documents =
            await _repository.SearchAsync(vector, 5);

        if (documents is null || documents.Count == 0)
            return "No relevant documents found.";

        return string.Join(
            "\n\n---\n\n",
            documents.Select(x =>
                $"Title: {x.Title}\n" +
                $"Content: {x.Content}\n" +
                $"Distance: {x.Distance}"));
    }
}