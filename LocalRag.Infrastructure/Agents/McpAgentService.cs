using LocalRag.Application.DTOs;
using LocalRag.Application.Interfaces;
using LocalRag.Infrastructure.MCPServers;
using ProcurementAiApi.LocalRAG.Application.Interfaces;
using System.Text;
using System.Text.Json;

namespace LocalRag.Infrastructure.Agents;

public sealed class McpAgentService : IMcpAgentService
{
    private readonly ILlmService _llm;
    private readonly VectorSearchTools _vectorSearchTools;
    private readonly DatabaseTools _databaseTools;

    public McpAgentService(ILlmService llm, VectorSearchTools vectorSearchTools, DatabaseTools databaseTools)
    {
        _llm = llm;
        _vectorSearchTools = vectorSearchTools;
        _databaseTools = databaseTools;
    }

    public async Task<string> AskAsync(string question, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question))
            throw new ArgumentException("Question cannot be empty.", nameof(question));

        var planningResult = await _llm.GenerateJsonAsync(BuildPlanningPrompt(question), cancellationToken);

        if (string.IsNullOrWhiteSpace(planningResult))
            throw new InvalidOperationException("Planner returned an empty response.");

        Console.WriteLine("===== PLANNER RAW RESPONSE =====");
        Console.WriteLine(planningResult);
        Console.WriteLine("================================");

        AgentPlan plan;

        try
        {
            var cleanJson = CleanJson(planningResult);
            plan = JsonSerializer.Deserialize<AgentPlan>(cleanJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidOperationException("Planner returned an empty plan.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Unable to parse planner response.", ex);
        }

        var toolResults = new List<AgentToolResult>();

        if (plan.UseDocumentSearch)
        {
            try
            {
                var result = await _vectorSearchTools.SearchDocuments(question);
                toolResults.Add(new AgentToolResult { ToolName = "SearchDocuments", Result = result });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SearchDocuments failed: {ex.Message}");
                toolResults.Add(new AgentToolResult { ToolName = "SearchDocuments", Result = "Document search was unavailable." });
            }
        }

        if (plan.UseDatabase)
        {
            var sql = await GenerateSqlAsync(question, cancellationToken);

            Console.WriteLine("===== GENERATED SQL =====");
            Console.WriteLine(sql);
            Console.WriteLine("=========================");

            var result = await _databaseTools.ExecuteQuery(sql, cancellationToken);

            toolResults.Add(new AgentToolResult { ToolName = "ExecuteQuery", Result = result });
        }

        if (toolResults.Count == 0)
            return "I could not find enough information to answer the question.";

        var contextBuilder = new StringBuilder();

        foreach (var toolResult in toolResults)
        {
            contextBuilder.AppendLine($"===== {toolResult.ToolName} =====");
            contextBuilder.AppendLine(toolResult.Result);
            contextBuilder.AppendLine();
        }

        var finalAnswer = await _llm.GenerateAsync(BuildFinalPrompt(question, contextBuilder.ToString()), cancellationToken);

        return CleanFinalAnswer(finalAnswer);
    }

    private static string BuildPlanningPrompt(string question)
    {
        return $$"""
You are a tool selection planner for a Procurement ERP assistant.

Your ONLY job is to decide which tools are required.

Available tools:

1. SearchDocuments
Use this for:
- procurement policy
- procurement procedure
- company rules
- approval rules
- workflow
- supplier terms and conditions
- policy documents
- stored company documents

2. ExecuteQuery
Use this for:
- supplier information
- purchase orders
- purchase order amounts
- quantities
- totals
- counts
- dates
- rankings
- current ERP database information
- transactional data

Rules:
- Do NOT answer the user's question.
- Do NOT generate SQL.
- Do NOT explain your decision.
- Return ONLY valid JSON.
- Do NOT use markdown.
- Do NOT use ``` fences.

Return exactly:

{
  "useDocumentSearch": false,
  "useDatabase": false
}

Examples:

Question:
"What is our procurement process?"

Answer:
{
  "useDocumentSearch": true,
  "useDatabase": false
}

Question:
"How much was our latest purchase order?"

Answer:
{
  "useDocumentSearch": false,
  "useDatabase": true
}

Question:
"What is our procurement policy and how much was our latest PO?"

Answer:
{
  "useDocumentSearch": true,
  "useDatabase": true
}

User question:
{{question}}
""";
    }

    private async Task<string> GenerateSqlAsync(string question, CancellationToken cancellationToken)
    {
        var prompt = """
You are a PostgreSQL SQL generator.

Your ONLY task is to generate one SQL SELECT query.

Database schema:

TABLE: purchase_order
COLUMNS:
id
po_number
po_date
supplier_id
total_amount
status

TABLE: supplier
COLUMNS:
id
name
phone
email

RELATION:
purchase_order.supplier_id = supplier.id

Rules:
1. Return ONLY the SQL query.
2. The response MUST start with SELECT.
3. Do NOT write explanations.
4. Do NOT write steps.
5. Do NOT write "Sure".
6. Do NOT write "Here is the query".
7. Do NOT use markdown.
8. Do NOT use ```.
9. Do NOT use INSERT.
10. Do NOT use UPDATE.
11. Do NOT use DELETE.
12. Do NOT use DROP.
13. Do NOT use ALTER.
14. Do NOT use CREATE.
15. Use only the tables and columns provided above.
16. Return exactly one SELECT query.

For the latest purchase order:
ORDER BY po_date DESC
LIMIT 1

User question:
""" + question;

        var response = await _llm.GenerateAsync(prompt, cancellationToken);

        Console.WriteLine("===== RAW SQL LLM RESPONSE =====");
        Console.WriteLine(response);
        Console.WriteLine("=================================");

        if (string.IsNullOrWhiteSpace(response))
            throw new InvalidOperationException("SQL generator returned an empty response.");

        var sql = response.Trim();

        var selectIndex = sql.IndexOf(
            "SELECT",
            StringComparison.OrdinalIgnoreCase);

        if (selectIndex < 0)
        {
            throw new InvalidOperationException(
                $"LLM did not generate SQL. Raw response: {response}");
        }

        sql = sql[selectIndex..].Trim();

        var fenceIndex = sql.IndexOf("```");

        if (fenceIndex >= 0)
            sql = sql[..fenceIndex].Trim();

        var semicolonIndex = sql.IndexOf(';');

        if (semicolonIndex >= 0)
            sql = sql[..(semicolonIndex + 1)];

        sql = sql.Trim();

        if (!sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Generated query is not a SELECT query: {sql}");
        }

        Console.WriteLine("===== CLEAN SQL =====");
        Console.WriteLine(sql);
        Console.WriteLine("=====================");

        return sql;
    }

    private static string BuildFinalPrompt(string question, string toolContext)
    {
        return $$"""
You are a Procurement ERP assistant.

Answer the user's question using ONLY the information in TOOL RESULTS.

Rules:
- Output ONLY the final answer.
- Do NOT output reasoning.
- Do NOT analyze the question.
- Do NOT say "Okay".
- Do NOT say "The user is asking".
- Do NOT say "I need to".
- Do NOT say "First, I see".
- Do NOT say "Let me check".
- Do NOT say "Let's analyze".
- Do NOT mention tools.
- Do NOT mention SearchDocuments.
- Do NOT mention ExecuteQuery.
- Do NOT mention the planner.
- Do NOT mention retrieval.
- Do NOT mention distance.
- Do NOT mention relevance scores.
- Do NOT mention internal reasoning.
- Do NOT invent information.
- Do NOT make up numbers.
- Use ONLY facts contained in TOOL RESULTS.
- If information is unavailable, say: "The requested information is not available."
- Keep the answer concise and professional.
- For process or workflow questions, use a numbered list.
- For database questions, clearly show the relevant values.
- Return ONLY the final user-facing answer.

USER QUESTION:

{{question}}

TOOL RESULTS:

{{toolContext}}

FINAL USER-FACING ANSWER:
""";
    }

    private static string CleanJson(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("Planner returned empty response.");

        text = text.Trim();

        if (text.StartsWith("```"))
        {
            var firstNewLine = text.IndexOf('\n');

            if (firstNewLine >= 0)
                text = text[(firstNewLine + 1)..];

            var lastFence = text.LastIndexOf("```");

            if (lastFence >= 0)
                text = text[..lastFence];

            text = text.Trim();
        }

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');

        if (start < 0 || end < 0 || end <= start)
            throw new InvalidOperationException($"Planner did not return valid JSON. Raw response: {text}");

        return text[start..(end + 1)].Trim();
    }

    private static string CleanFinalAnswer(string answer)
    {
        if (string.IsNullOrWhiteSpace(answer))
            return "I could not generate an answer.";

        answer = answer.Trim();

        var thinkStart = answer.IndexOf("<think>", StringComparison.OrdinalIgnoreCase);

        while (thinkStart >= 0)
        {
            var thinkEnd = answer.IndexOf("</think>", thinkStart, StringComparison.OrdinalIgnoreCase);

            if (thinkEnd < 0)
            {
                answer = answer[..thinkStart];
                break;
            }

            answer = answer.Remove(thinkStart, thinkEnd + "</think>".Length - thinkStart);
            thinkStart = answer.IndexOf("<think>", StringComparison.OrdinalIgnoreCase);
        }

        if (answer.StartsWith("```") && answer.EndsWith("```"))
        {
            var firstNewLine = answer.IndexOf('\n');

            if (firstNewLine >= 0)
                answer = answer[(firstNewLine + 1)..];

            var lastFence = answer.LastIndexOf("```");

            if (lastFence >= 0)
                answer = answer[..lastFence];
        }

        return answer.Trim();
    }
}

public sealed class AgentPlan
{
    public bool UseDocumentSearch { get; set; }
    public bool UseDatabase { get; set; }
}

public sealed class AgentToolResult
{
    public string ToolName { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
}