using LocalRag.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace LocalRag.Application.Features.Queries
{
    public class VectorSearchQuery: IRequest<IReadOnlyList<DocumentChunkDto>>
    {
        public string Question { get; set; } = string.Empty;
        public int Limit { get; set; } = 5;
    }
}

