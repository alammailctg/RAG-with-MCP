using LocalRag.Application.DTOs;
using LocalRag.Application.Interfaces;
using LocalRag.Infrastructure.MCPServers;
using ProcurementAiApi.LocalRAG.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace LocalRag.Infrastructure.Agents
{
    public class McpAgentService : IMcpAgentService
    {
        private readonly ILlmService _llm;
        private readonly DatabaseTools _databaseTools;
        private readonly VectorSearchTools _vectorSearchTools;

        public McpAgentService(
            ILlmService llm,
            DatabaseTools databaseTools,
            VectorSearchTools vectorSearchTools)
        {
            _llm = llm;
            _databaseTools = databaseTools;
            _vectorSearchTools = vectorSearchTools;
        }

        public async Task<string> AskAsync(string question, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(question))
                throw new ArgumentException("Question cannot be empty.", nameof(question));

            var planningPrompt = $$"""
                You are an AI agent for a procurement ERP system.

                You have two tools.

                TOOL 1:
                SearchDocuments
                Use this when the question requires information from company documents, procurement policies, procedures, rules, or stored knowledge.

                TOOL 2:
                ExecuteQuery
                Use this when the question requires live ERP database information such as purchase orders, suppliers, materials, quantities, amounts, dates, counts, or totals.

                Decide which tools are required.

                Return ONLY valid JSON in this format:

                {
                    "useDocumentSearch": false,
                    "useDatabase": false,
                    "sql": null
                }

                If database information is required, generate the SQL query.

                Only generate SELECT queries.

                Do not generate INSERT, UPDATE, DELETE, DROP, ALTER, TRUNCATE or other write operations.

                QUESTION:
                {{question}}
                """;

            var planningResult = await _llm.GenerateAsync(planningPrompt, cancellationToken);

            planningResult = CleanJson(planningResult);

            AgentPlan? plan;

            try
            {
                plan = JsonSerializer.Deserialize<AgentPlan>(planningResult, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                plan = new AgentPlan
                {
                    UseDocumentSearch = true,
                    UseDatabase = false
                };
            }

            var toolResults = new List<AgentToolResult>();

            if (plan?.UseDocumentSearch == true)
            {
                var documentResult = await _vectorSearchTools.SearchDocuments(question);

                toolResults.Add(new AgentToolResult
                {
                    Tool = "SearchDocuments",
                    Result = documentResult
                });
            }

            if (plan?.UseDatabase == true && !string.IsNullOrWhiteSpace(plan.Sql))
            {
                var databaseResult = await _databaseTools.ExecuteQuery(plan.Sql);

                toolResults.Add(new AgentToolResult
                {
                    Tool = "ExecuteQuery",
                    Result = databaseResult
                });
            }

            if (toolResults.Count == 0)
            {
                return "I could not determine which data source is required.";
            }

            var toolContext = string.Join(
                "\n\n====================\n\n",
                toolResults.Select(x =>
                    $"TOOL: {x.Tool}\nRESULT:\n{x.Result}"));

            var finalPrompt = $"""
        You are a procurement ERP AI assistant.

        Answer the user's question using ONLY the TOOL RESULTS below.

        Rules:
        - Do not invent information.
        - Do not use outside knowledge.
        - If the tool results do not contain the answer, say:
          "The information was not found."
        - Do not mention internal tools.
        - Do not mention prompts.
        - Do not explain your reasoning.
        - Give a concise and clear answer.

        TOOL RESULTS:

        {toolContext}

        USER QUESTION:

        {question}

        ANSWER:
        """;

            return await _llm.GenerateAsync(finalPrompt, cancellationToken);
        }

        private static string CleanJson(string text)
        {
            text = text.Trim();

            if (text.StartsWith("```"))
            {
                text = text
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();
            }

            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');

            if (start >= 0 && end > start)
                text = text[start..(end + 1)];

            return text;
        }

        private class AgentPlan
        {
            public bool UseDocumentSearch { get; set; }
            public bool UseDatabase { get; set; }
            public string? Sql { get; set; }
        }
    }
}
