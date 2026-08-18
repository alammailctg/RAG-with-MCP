using LocalRag.Domain.Model;
using ProcurementAiApi.LocalRAG.Infrastructure.Persistance.Entities;

namespace LocalRag.Domain.RepositoryInterfaces
{
    public interface IVectorRepository
    {
        Task AddAsync(DocumentChunk document, CancellationToken cancellationToken = default);

        Task<List<DocumentChunkSearchResult>> SearchAsync(
            float[] embedding,
            int limit = 5,
            CancellationToken cancellationToken = default);
    }
}
