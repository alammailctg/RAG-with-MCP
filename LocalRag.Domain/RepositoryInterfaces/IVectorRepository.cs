using LocalRag.Domain.Model;
namespace LocalRag.Domain.RepositoryInterfaces
{
    public interface IVectorRepository
    {
        Task AddAsync(DocumentChunk document, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<DocumentChunk>> SearchAsync(float[] embedding, int limit = 5,
        CancellationToken cancellationToken = default);
    }
}
