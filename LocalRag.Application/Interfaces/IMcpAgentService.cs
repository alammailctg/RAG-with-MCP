using System;
using System.Collections.Generic;
using System.Text;

namespace LocalRag.Application.Interfaces
{
    public interface IMcpAgentService
    {
        Task<string> AskAsync(string question, CancellationToken cancellationToken = default);
    }
}
