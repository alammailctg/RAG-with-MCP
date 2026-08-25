using LocalRag.Domain.RepositoryInterfaces;
using LocalRagApi.DtoRequest;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProcurementAiApi.LocalRAG.Application.Interfaces;

namespace LocalRagApi.Controllers
{
    [ApiController]
    [Route("api/vector")]
    public class VectorController : ControllerBase
    {
        private readonly IEmbeddingService _embedding;
        private readonly IVectorRepository _repository;
        public VectorController(IEmbeddingService embedding, IVectorRepository repository)
        {
            _embedding = embedding;
            _repository = repository;
        }

        [HttpPost("search")]
        public async Task<IActionResult> Search(SearchRequest request)
        {
            var embedding =await _embedding.GenerateEmbeddingAsync(request.Question);
            var result = await _repository.SearchAsync(embedding,5);
            return Ok(result);
        }

    }
}
