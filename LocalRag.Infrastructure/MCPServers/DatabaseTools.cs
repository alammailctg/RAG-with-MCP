using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Server;
using Npgsql;
using System.ComponentModel;
using System.Text.Json;

namespace LocalRag.Infrastructure.MCPServers;

[McpServerToolType]
public sealed class DatabaseTools
{
    private readonly IConfiguration _configuration;

    public DatabaseTools(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [McpServerTool]
    [Description("Execute a read-only SQL SELECT query against the ERP PostgreSQL database.")]
    public async Task<string> ExecuteQuery(
        string sql,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("SQL query cannot be empty.", nameof(sql));

        sql = sql.Trim();

        if (!sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) &&
            !sql.StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Only SELECT or WITH queries are allowed. Received: {sql}");
        }
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException(
                "SQL query cannot be empty.",
                nameof(sql));

        var normalizedSql = sql.Trim();

        if (normalizedSql.EndsWith(';'))
            normalizedSql = normalizedSql[..^1].Trim();

        if (normalizedSql.Contains(';'))
            throw new InvalidOperationException(
                "Multiple SQL statements are not allowed.");

        if (!IsReadOnlyQuery(normalizedSql))
            throw new InvalidOperationException(
                "Only SELECT or WITH queries are allowed.");

        var connectionString =
            _configuration.GetConnectionString("RagDb");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "RagDb connection string is not configured.");

        await using var connection =
            new NpgsqlConnection(connectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command =
            new NpgsqlCommand(normalizedSql, connection);

        command.CommandTimeout = 30;

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        var rows = new List<Dictionary<string, object?>>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>(
                StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] =
                    reader.IsDBNull(i)
                        ? null
                        : reader.GetValue(i);
            }

            rows.Add(row);
        }

        return JsonSerializer.Serialize(rows);
    }

    private static bool IsReadOnlyQuery(string sql)
    {
        var normalized = sql.TrimStart();

        if (normalized.StartsWith(
                "SELECT ",
                StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals(
                "SELECT",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (normalized.StartsWith(
                "WITH ",
                StringComparison.OrdinalIgnoreCase))
        {
            var lower = normalized.ToLowerInvariant();

            if (lower.Contains("insert "))
                return false;

            if (lower.Contains("update "))
                return false;

            if (lower.Contains("delete "))
                return false;

            if (lower.Contains("merge "))
                return false;

            return true;
        }

        return false;
    }
}