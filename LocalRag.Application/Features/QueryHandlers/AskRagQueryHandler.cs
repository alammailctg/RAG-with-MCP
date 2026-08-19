using LocalRag.Application.DTOs;
using LocalRag.Application.Features.Queries;
using MediatR;
using ProcurementAiApi.LocalRAG.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

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
                    Answer = "No relevant information was found.",
                    Sources = []
                };
            }

            var context = string.Join("\n\n---\n\n", chunks.Select(x => $"Title: {x.Title}\nContent: {x.Content}"));

            var prompt = $"""
            You are a RAG assistant.

            Answer the question using only the provided context.

            If the answer is not available in the context, say that the information was not found.

            Context:
            {context}

            Question:
            {request.Question}

            Answer:
            """;

            var answer = await _llmService.GenerateAsync(prompt, cancellationToken);

            return new RagAnswerDto
            {
                Question = request.Question,
                Answer = answer,
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
