using LocalRag.Application.DTOs;
using LocalRag.Application.Features.Queries;
using LocalRag.Domain.RepositoryInterfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace LocalRag.Application.Features.QueryHandlers
{
    public class SearchDocumentChunkQueryHandler: IRequestHandler<SearchDocumentChunkQuery, IReadOnlyList<DocumentChunkDto>>
    {
        private readonly IVectorRepository _vectorRepository;

        public SearchDocumentChunkQueryHandler(IVectorRepository vectorRepository)
        {
            _vectorRepository = vectorRepository;
        }

        public async Task<IReadOnlyList<DocumentChunkDto>> Handle(SearchDocumentChunkQuery request, CancellationToken cancellationToken)
        {
            if (request.Embedding is null || request.Embedding.Length == 0)
                throw new ArgumentException("Embedding cannot be empty.", nameof(request.Embedding));

            var results = await _vectorRepository.SearchAsync(request.Embedding, request.Limit, cancellationToken);

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

