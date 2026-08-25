using System;
using System.Collections.Generic;
using System.Text;

namespace LocalRag.Application.ServiceInterface
{
    public interface IDatabaseQueryService
    {
        Task<string> ExecuteAsync(string sql, CancellationToken cancellationToken = default);
    }
}
