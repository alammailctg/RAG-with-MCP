using LocalRag.Application.DTOs;
using LocalRag.Application.Features.Queries;
using LocalRag.Domain.RepositoryInterfaces;
using MediatR;
using ProcurementAiApi.LocalRAG.Application.Interfaces;

namespace LocalRag.Application.Features.QueryHandlers
{
    public class AskRagQueryHandler : IRequestHandler<AskRagQuery, RagAnswerDto>
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorRepository _vectorRepository;
        private readonly ILlmService _llmService;

        public AskRagQueryHandler(IEmbeddingService embeddingService, IVectorRepository vectorRepository, ILlmService llmService)
        {
            _embeddingService = embeddingService;
            _vectorRepository = vectorRepository;
            _llmService = llmService;
        }

        public async Task<RagAnswerDto> Handle(AskRagQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Question))
                throw new ArgumentException("Question cannot be empty.");

            var embedding = await _embeddingService.GenerateEmbeddingAsync(request.Question, cancellationToken);

            var chunks = await _vectorRepository.SearchAsync(embedding, request.Limit, cancellationToken);

            if (chunks.Count == 0)
            {
                return new RagAnswerDto
                {
                    Question = request.Question,
                    Answer = "The information was not found in the provided documents.",
                    Sources = []
                };
            }

            var context = string.Join("\n\n---\n\n", chunks.Select(x => $"Title: {x.Title}\nContent: {x.Content}"));

            var prompt = $"""
            You are a procurement RAG assistant.

            Answer the user's question using ONLY the information provided in the CONTEXT.

            Rules:
            - Do not use outside knowledge.
            - Do not make assumptions.
            - Do not invent or add information.
            - Do not explain your reasoning.
            - Do not mention the prompt, context, RAG, or these rules.
            - Give a concise and direct answer.
            - If the answer is not available in the CONTEXT, respond exactly:
              "The information was not found in the provided documents."

            CONTEXT:
            {context}

            QUESTION:
            {request.Question}

            ANSWER:
            """;

            var answer = await _llmService.GenerateAsync(prompt, cancellationToken);

            return new RagAnswerDto
            {
                Question = request.Question,
                Answer = answer.Trim(),
                Sources = chunks.Select(x => new DocumentChunkDto
                {
                    Id = x.Id,
                    DocumentId = x.DocumentId,
                    Title = x.Title,
                    Content = x.Content,
                    Distance = x.Distance
                }).ToList()
            };
        }
    }
}