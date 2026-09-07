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

       
        var finalJson = await _llm.GenerateJsonAsync(
            BuildFinalPrompt(question, contextBuilder.ToString()),
            cancellationToken);

        using var finalDocument = JsonDocument.Parse(finalJson);

        if (!finalDocument.RootElement.TryGetProperty("answer", out var answerElement))
            throw new InvalidOperationException("LLM final response does not contain 'answer'.");

        return answerElement.GetString()?.Trim()
               ?? "The requested information is not available.";
 

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
        var prompt = $$"""
        You generate PostgreSQL SQL for a procurement database.

        Return ONLY valid JSON.
        Do not return Markdown.
        Do not return explanations.
        Do not return reasoning.

        Required JSON format:
        {
          "sql": "SELECT ..."
        }

        DATABASE SCHEMA:

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

        RULES:
        - sql must contain exactly one SELECT statement.
        - sql must start with SELECT.
        - Use only the tables and columns defined above.
        - Never use INSERT.
        - Never use UPDATE.
        - Never use DELETE.
        - Never use DROP.
        - Never use ALTER.
        - Never use CREATE.
        - Do not include comments.
        - Do not include explanations.
        - Do not include reasoning.

        USER QUESTION:
        {{question}}
        """;

        var response = await _llm.GenerateJsonAsync(prompt, cancellationToken);
                Console.WriteLine("===== RAW SQL JSON RESPONSE =====");
                Console.WriteLine(response);
                Console.WriteLine("=================================");

                if (string.IsNullOrWhiteSpace(response))
                    throw new InvalidOperationException("SQL generator returned an empty response.");

                using var document = JsonDocument.Parse(response);

                if (!document.RootElement.TryGetProperty("sql", out var sqlElement))
                    throw new InvalidOperationException("LLM response does not contain 'sql'.");

                var sql = sqlElement.GetString()?.Trim();

                if (string.IsNullOrWhiteSpace(sql))
                    throw new InvalidOperationException("Generated SQL is empty.");

                sql = sql.Replace("```sql", "", StringComparison.OrdinalIgnoreCase)
                         .Replace("```", "", StringComparison.OrdinalIgnoreCase)
                         .Trim();

                var semicolonIndex = sql.IndexOf(';');

                if (semicolonIndex >= 0)
                    sql = sql[..semicolonIndex].Trim();

                if (!sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Generated query is not SELECT: {sql}");

                var forbiddenWords = new[]
                {
                "However",
                "Sure",
                "Let's",
                "Let me",
                "Here is",
                "The query",
                "Explanation:",
                "Answer:",
                "SELECT statement"
            };

        foreach (var word in forbiddenWords)
        {
            if (sql.Contains(word, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"LLM generated invalid SQL containing natural language: {sql}");
        }

        Console.WriteLine("===== CLEAN SQL =====");
        Console.WriteLine(sql);
        Console.WriteLine("=====================");

        return sql;
    }



    private string BuildFinalPrompt(string question, string toolResults) { return $$""" You are the final answer generator for a procurement system. Return ONLY valid JSON in exactly this format: { "answer": "your concise answer" } STRICT RULES: - Do not output Markdown. - Do not output reasoning. - Do not explain your thought process. - Do not repeat the question. - Do not mention tools. - Do not mention "tool result". - Do not mention "context". - Do not use "However". - Do not use "Let's re-read". - Do not use "But note". - Do not speculate. - Do not invent information. - Use ONLY the information contained in the tool results. - Keep the answer concise and professional. - If the requested information is unavailable, answer exactly: "The requested information is not available." USER QUESTION: {{question}} TOOL RESULTS: {{toolResults}} """; }

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