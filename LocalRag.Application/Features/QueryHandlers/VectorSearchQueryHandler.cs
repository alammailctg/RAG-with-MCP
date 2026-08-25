using LocalRag.Application.DTOs;
using LocalRag.Application.Features.Queries;
using LocalRag.Domain.RepositoryInterfaces;
using MediatR;
using ProcurementAiApi.LocalRAG.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace LocalRag.Application.Features.QueryHandlers
{
    public class VectorSearchQueryHandler: IRequestHandler<VectorSearchQuery,IReadOnlyList<DocumentChunkDto>>
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorRepository _vectorRepository;
        public VectorSearchQueryHandler(IEmbeddingService embeddingService, IVectorRepository vectorRepository)
        {
            _embeddingService = embeddingService;
            _vectorRepository = vectorRepository;
        }

        public async Task<IReadOnlyList<DocumentChunkDto>> Handle(VectorSearchQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Question))
            {
                throw new ArgumentException("Question cannot be empty");
            }
            var embedding =await _embeddingService.GenerateEmbeddingAsync(request.Question, cancellationToken);
            var results = await _vectorRepository.SearchAsync(embedding,request.Limit, cancellationToken);

            return results.Select(x => new DocumentChunkDto
            {
                Id = x.Id,
                DocumentId = x.DocumentId,
                Title = x.Title,
                Content = x.Content,
                Distance = x.Distance

            }).ToList();
        }
    }
}
