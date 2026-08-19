using LocalRag.Domain.Model;
using LocalRag.Domain.RepositoryInterfaces;
using LocalRag.Infrastructure.Persistance.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using ProcurementAiApi.LocalRAG.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LocalRag.Infrastructure.Repositories
{
    public class VectorRepository : IVectorRepository
    {
        private readonly LocalRagDbContext _context;

        public VectorRepository(LocalRagDbContext dbContext)
        {
            _context = dbContext;
        }

        public async Task AddAsync(DocumentChunk document, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(document);

            document.CreatedAt = DateTime.UtcNow;

            await _context.DocumentChunks.AddAsync(
                document,
                cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<DocumentChunk>> SearchAsync(float[] embedding, int limit = 5,
        CancellationToken cancellationToken = default)
        {
            if (embedding is null || embedding.Length == 0)
            {
                throw new ArgumentException(
                    "Embedding cannot be empty.",
                    nameof(embedding));
            }

            if (limit <= 0)
            {
                limit = 5;
            }

            var queryVector = new Pgvector.Vector(embedding);

            var results = await _context.DocumentChunks
                .AsNoTracking()
                .OrderBy(x =>
                    x.Embedding.CosineDistance(queryVector))
                .Take(limit)
                .Select(x => new DocumentChunk
                {
                    Id = x.Id,
                    DocumentId = x.DocumentId,
                    Title = x.Title,
                    Content = x.Content,
                    Distance = x.Embedding.CosineDistance(queryVector)
                })
                .ToListAsync(cancellationToken);

            return results;
        }
    }
}
