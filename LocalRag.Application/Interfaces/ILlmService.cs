namespace ProcurementAiApi.LocalRAG.Application.Interfaces
{
    public interface ILlmService
    {
        Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default);
    }
}
