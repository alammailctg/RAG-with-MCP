using LocalRag.Application.Features.Queries;
using LocalRag.Application.ServiceInterface;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace LocalRag.Application.Features.QueryHandlers
{
    public class ExecuteDatabaseQueryHandler : IRequestHandler<ExecuteDatabaseQuery, string>
    {
        private readonly IDatabaseQueryService _databaseService;
        public ExecuteDatabaseQueryHandler(IDatabaseQueryService databaseService)
        {
            _databaseService = databaseService;
        }
        public async Task<string> Handle(ExecuteDatabaseQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Sql))
                throw new ArgumentException(
                    "SQL cannot be empty");

            return await _databaseService.ExecuteAsync(request.Sql, cancellationToken);
        }
    }
}
