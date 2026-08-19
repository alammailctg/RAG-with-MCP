using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace LocalRag.Application.Features.Commands
{
    public record AddDocumentCommand(string DocumentId, string? Title, string Content) : IRequest<int>;
}
