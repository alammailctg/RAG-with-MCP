using LocalRag.Application.DTOs;
using LocalRag.Application.Interfaces;
using LocalRag.Infrastructure.MCPServers;
using ProcurementAiApi.LocalRAG.Application.Interfaces;
using System.Text.Json;

namespace LocalRag.Infrastructure.Agents;

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

    public async Task<string> AskAsync(
        string question,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question))
            throw new ArgumentException(
                "Question cannot be empty.",
                nameof(question));

        var planningPrompt = $$"""
You are an AI agent for a procurement ERP system.

You have two available tools.

TOOL 1: SearchDocuments

Use SearchDocuments when the question requires:
- procurement policies
- procurement procedures
- company rules
- workflow information
- approval processes
- stored company knowledge
- information contained in documents

TOOL 2: ExecuteQuery

Use ExecuteQuery when the question requires:
- purchase orders
- suppliers
- materials
- quantities
- amounts
- dates
- counts
- totals
- live ERP database information

Decide which tool or tools are required.

Return ONLY valid JSON.
Do not return markdown.
Do not return explanations.
Do not return reasoning.

Required format:

{
  "useDocumentSearch": false,
  "useDatabase": false,
  "sql": null
}

Rules:
- For company policy, procurement process, procedure, workflow, or company-rule questions, set useDocumentSearch to true.
- For live ERP data questions, set useDatabase to true.
- If both are required, set both to true.
- If database is required, generate only a SELECT SQL query.
- Never generate INSERT.
- Never generate UPDATE.
- Never generate DELETE.
- Never generate DROP.
- Never generate ALTER.
- Never generate TRUNCATE.
- Return JSON only.

QUESTION:
{{question}}
""";

        Console.WriteLine("========== PLANNER PROMPT ==========");
        Console.WriteLine(planningPrompt);

        var planningResult = await _llm.GenerateAsync(
            planningPrompt,
            cancellationToken);

        Console.WriteLine("========== PLANNER RESULT ==========");
        Console.WriteLine(planningResult);

        planningResult = CleanJson(planningResult);

        AgentPlan? plan = null;

        try
        {
            plan = JsonSerializer.Deserialize<AgentPlan>(
                planningResult,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
        catch (Exception ex)
        {
            Console.WriteLine("========== PLANNER PARSE ERROR ==========");
            Console.WriteLine(ex.Message);
        }

        if (plan == null)
        {
            plan = new AgentPlan
            {
                UseDocumentSearch = true,
                UseDatabase = false
            };
        }

        Console.WriteLine("========== PARSED PLAN ==========");
        Console.WriteLine($"UseDocumentSearch: {plan.UseDocumentSearch}");
        Console.WriteLine($"UseDatabase: {plan.UseDatabase}");
        Console.WriteLine($"SQL: {plan.Sql}");

        var toolResults = new List<AgentToolResult>();

        if (plan.UseDocumentSearch)
        {
            try
            {
                var documentResult =
                    await _vectorSearchTools.SearchDocuments(question);

                Console.WriteLine("========== DOCUMENT SEARCH RESULT ==========");
                Console.WriteLine(documentResult);

                toolResults.Add(new AgentToolResult
                {
                    Tool = "SearchDocuments",
                    Result = documentResult
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("========== DOCUMENT SEARCH ERROR ==========");
                Console.WriteLine(ex);

                toolResults.Add(new AgentToolResult
                {
                    Tool = "SearchDocuments",
                    Result = $"Error: {ex.Message}"
                });
            }
        }

        if (plan.UseDatabase &&
            !string.IsNullOrWhiteSpace(plan.Sql))
        {
            try
            {
                var databaseResult =
                    await _databaseTools.ExecuteQuery(plan.Sql);

                Console.WriteLine("========== DATABASE RESULT ==========");
                Console.WriteLine(databaseResult);

                toolResults.Add(new AgentToolResult
                {
                    Tool = "ExecuteQuery",
                    Result = databaseResult
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("========== DATABASE ERROR ==========");
                Console.WriteLine(ex);

                toolResults.Add(new AgentToolResult
                {
                    Tool = "ExecuteQuery",
                    Result = $"Error: {ex.Message}"
                });
            }
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
                You are a procurement ERP assistant.

                Question:
                {question}

                Available data:
                {toolContext}

                Write the answer directly.

                Output ONLY the answer that should be shown to the user.
                Never output reasoning.
                Never output analysis.
                Never mention the data source.
                Never mention tools.

                Answer:
                """;

        Console.WriteLine("========== FINAL PROMPT ==========");
        Console.WriteLine(finalPrompt);

        var finalAnswer = await _llm.GenerateAsync(
            finalPrompt,
            cancellationToken);

        Console.WriteLine("========== FINAL ANSWER ==========");
        Console.WriteLine(finalAnswer);

        if (string.IsNullOrWhiteSpace(finalAnswer))
        {
            return "No answer could be generated from the available data.";
        }

        return finalAnswer.Trim();
    }

    private static string CleanJson(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        text = text.Trim();

        text = text
            .Replace("```json", "", StringComparison.OrdinalIgnoreCase)
            .Replace("```", "")
            .Trim();

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');

        if (start >= 0 && end > start)
        {
            return text[start..(end + 1)];
        }

        return text;
    }

    private class AgentPlan
    {
        public bool UseDocumentSearch { get; set; }
        public bool UseDatabase { get; set; }
        public string? Sql { get; set; }
    }
}