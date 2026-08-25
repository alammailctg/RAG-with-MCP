using LocalRag.Application.ServiceInterface;
using Microsoft.Extensions.Configuration;
using Npgsql;


namespace LocalRag.Infrastructure.ServiceImplementation
{
    public class DatabaseQueryService : IDatabaseQueryService
    {
        private readonly IConfiguration _configuration;
        public DatabaseQueryService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task<string> ExecuteAsync(string sql, CancellationToken cancellationToken = default)
        {
            await using var con = new NpgsqlConnection(_configuration.GetConnectionString("RagDb"));
            await con.OpenAsync(cancellationToken);
            await using var cmd =
            new NpgsqlCommand(sql, con);

            await using var reader =
            await cmd.ExecuteReaderAsync(
            cancellationToken);

            var result = new List<string>();

            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(reader.GetValue(0).ToString()!);
            }

            return string.Join("\n", result);
        }
    }
}