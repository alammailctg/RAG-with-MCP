using LocalRag.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace LocalRag.Application.Features.Queries
{
    public record AskRagQuery(string Question,  int Limit = 5) : IRequest<RagAnswerDto>;
}
