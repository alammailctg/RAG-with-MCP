using LocalRag.Domain.Model;
using LocalRag.Domain.RepositoryInterfaces;
using MediatR;
using Pgvector;
using ProcurementAiApi.LocalRAG.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace LocalRag.Application.Features.Commands
{
    public class AddDocumentCommandHandler : IRequestHandler<AddDocumentCommand, int>
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorRepository _vectorRepository;

        public AddDocumentCommandHandler(
            IEmbeddingService embeddingService,
            IVectorRepository vectorRepository)
        {
            _embeddingService = embeddingService;
            _vectorRepository = vectorRepository;
        }

        public async Task<int> Handle(AddDocumentCommand request,  CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
                throw new ArgumentException("Content cannot be empty.");

            var chunks = ChunkText(request.Content, 1000);

            foreach (var chunkText in chunks)
            {
                var embedding = await _embeddingService.GenerateEmbeddingAsync(
                    chunkText,
                    cancellationToken);

                if (embedding.Length != 768)
                    throw new InvalidOperationException(
                        $"Expected 768 dimensions but received {embedding.Length}.");

                var chunk = new DocumentChunk
                {
                    DocumentId = request.DocumentId,
                    Title = request.Title,
                    Content = chunkText,
                    Embedding = new Vector(embedding),
                    CreatedAt = DateTime.UtcNow
                };

                await _vectorRepository.AddAsync(
                    chunk,
                    cancellationToken);
            }

            return chunks.Count;
        }

        private static List<string> ChunkText(string text, int chunkSize)
        {
            var chunks = new List<string>();

            for (int i = 0; i < text.Length; i += chunkSize)
            {
                var length = Math.Min(chunkSize, text.Length - i);

                chunks.Add(text.Substring(i, length));
            }

            return chunks;
        }
    }
}
