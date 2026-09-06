namespace ProcurementAiApi.LocalRAG.Application.DTOs
{
    public class OllamaGenerateResponse
    {
        public string Model { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public string Thinking { get; set; } = string.Empty;
        public bool Done { get; set; }
        public string DoneReason { get; set; } = string.Empty;
    }
}
