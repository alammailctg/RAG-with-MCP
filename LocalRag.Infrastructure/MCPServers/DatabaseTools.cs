using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Server;
using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

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
        [Description("Execute read only SQL query against ERP database")]
        public async Task<string> ExecuteQuery(
            string sql)
        {
            await using var con =new NpgsqlConnection(_configuration.GetConnectionString("RagDb"));

            await con.OpenAsync();
            await using var cmd = new NpgsqlCommand(sql, con);

            var reader =  await cmd.ExecuteReaderAsync();
            var result = new List<string>();

            while (await reader.ReadAsync())
            {
                result.Add(
                  reader.GetString(0)
                );
            }


            return string.Join("\n", result);
        }
    }
}
