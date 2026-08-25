using LocalRag.Domain.RepositoryInterfaces;
using ModelContextProtocol.Server;
using ProcurementAiApi.LocalRAG.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace LocalRag.Infrastructure.MCPServers
{
    [McpServerToolType]
    public class VectorSearchTools
    {
        private readonly IEmbeddingService _embedding;
        private readonly IVectorRepository _repository;

        public VectorSearchTools(IEmbeddingService embedding,  IVectorRepository repository)
        {
            _embedding = embedding;
            _repository = repository;
        }

        [McpServerTool]
        [Description("Search company documents using semantic similarity")]
        public async Task<string> SearchDocuments(
        string question)
        {

            var vector = await _embedding.GenerateEmbeddingAsync(question);

            var documents = await _repository.SearchAsync(vector,5);
            return string.Join("\n\n", documents.Select(x => x.Content)
            );
        }
    }
}
