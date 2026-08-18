using LocalRag.Domain.Model;
using LocalRag.Domain.RepositoryInterfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LocalRag.Infrastructure.Repositories
{
    public class VectorRepository : IVectorRepository
    {
        private readonly string _connectionString;

        public VectorRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection is not configured.");
        }

        public async Task AddAsync(
            DocumentChunk document,
            CancellationToken cancellationToken = default)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            if (document.Embedding == null || document.Embedding.Length == 0)
                throw new ArgumentException("Embedding cannot be empty.", nameof(document));

            const string sql = """
            INSERT INTO "DocumentChunks"
            (
                "DocumentId",
                "Title",
                "Content",
                "Embedding",
                "CreatedAt"
            )
            VALUES
            (
                @documentId,
                @title,
                @content,
                CAST(@embedding AS vector),
                @createdAt
            );
            """;

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("documentId", document.DocumentId);
            command.Parameters.AddWithValue("title", (object?)document.Title ?? DBNull.Value);
            command.Parameters.AddWithValue("content", document.Content);
            command.Parameters.AddWithValue("embedding", ToVectorString(document.Embedding));
            command.Parameters.AddWithValue("createdAt", document.CreatedAt);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<List<DocumentChunkSearchResult>> SearchAsync(
            float[] embedding,
            int limit = 5,
            CancellationToken cancellationToken = default)
        {
            if (embedding == null || embedding.Length == 0)
                throw new ArgumentException("Embedding cannot be empty.", nameof(embedding));

            if (limit <= 0)
                limit = 5;

            const string sql = """
            SELECT
                "Id",
                "DocumentId",
                "Title",
                "Content",
                ("Embedding" <=> CAST(@embedding AS vector)) AS "Distance"
            FROM "DocumentChunks"
            ORDER BY "Embedding" <=> CAST(@embedding AS vector)
            LIMIT @limit;
            """;

            var results = new List<DocumentChunkSearchResult>();

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue(
                "embedding",
                ToVectorString(embedding));

            command.Parameters.AddWithValue("limit", limit);

            await using var reader =
                await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(new DocumentChunkSearchResult
                {
                    Id = reader.GetInt64(reader.GetOrdinal("Id")),
                    DocumentId = reader.GetString(reader.GetOrdinal("DocumentId")),
                    Title = reader.IsDBNull(reader.GetOrdinal("Title"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("Title")),
                    Content = reader.GetString(reader.GetOrdinal("Content")),
                    Distance = reader.GetDouble(reader.GetOrdinal("Distance"))
                });
            }

            return results;
        }

        private static string ToVectorString(float[] embedding)
        {
            return "[" + string.Join(
                ",",
                embedding.Select(x =>
                    x.ToString("G", CultureInfo.InvariantCulture))) + "]";
        }
    }
}
