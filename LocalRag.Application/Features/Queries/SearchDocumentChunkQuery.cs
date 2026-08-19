using LocalRag.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace LocalRag.Application.Features.Queries
{
    public record SearchDocumentChunkQuery(float[] Embedding, int Limit = 5) : IRequest<IReadOnlyList<DocumentChunkDto>>;
}
