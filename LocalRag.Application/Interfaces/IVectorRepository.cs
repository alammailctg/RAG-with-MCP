
using LocalRag.Domain.Model;
using ProcurementAiApi.LocalRAG.Application.DTOs;

namespace ProcurementAiApi.LocalRAG.Application.Interfaces
{
    public interface IVectorRepository
    {
        Task AddAsync(DocumentChunk document, CancellationToken cancellationToken = default);

        Task<List<DocumentChunkSearchResult>> SearchAsync(float[] embedding,int limit = 5, CancellationToken cancellationToken = default);
    }
}
