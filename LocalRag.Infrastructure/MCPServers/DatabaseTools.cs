using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Server;
using Npgsql;
using System.ComponentModel;
using System.Text.Json;

namespace LocalRag.Infrastructure.MCPServers
{
    [McpServerToolType]
    public class DatabaseTools
    {
        private readonly IConfiguration _configuration;

        public DatabaseTools(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [McpServerTool]
        [Description("Execute a read-only SQL SELECT query against the ERP PostgreSQL database.")]
        public async Task<string> ExecuteQuery(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("SQL query cannot be empty.");

            var normalizedSql = sql.Trim().ToLowerInvariant();

            if (!normalizedSql.StartsWith("select") &&
                !normalizedSql.StartsWith("with"))
            {
                throw new InvalidOperationException(
                    "Only SELECT or WITH queries are allowed.");
            }

            if (normalizedSql.Contains(";"))
                throw new InvalidOperationException(
                    "Multiple SQL statements are not allowed.");

            await using var con = new NpgsqlConnection(
                _configuration.GetConnectionString("RagDb"));

            await con.OpenAsync();

            await using var cmd = new NpgsqlCommand(sql, con);

            await using var reader = await cmd.ExecuteReaderAsync();

            var rows = new List<Dictionary<string, object?>>();

            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>();

                for (var i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] =
                        reader.IsDBNull(i) ? null : reader.GetValue(i);
                }

                rows.Add(row);
            }

            return JsonSerializer.Serialize(rows);
        }
    }
}