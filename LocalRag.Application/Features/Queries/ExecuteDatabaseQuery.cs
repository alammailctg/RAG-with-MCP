using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace LocalRag.Application.Features.Queries
{
    public class ExecuteDatabaseQuery : IRequest<string>
    {
        public string Sql { get; set; } = string.Empty;
    }
}
