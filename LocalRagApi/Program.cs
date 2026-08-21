using LocalRag.Application.Features.Commands;
using LocalRag.Application.Features.QueryHandlers;
using LocalRag.Infrastructure;
using LocalRag.Infrastructure.Persistance.Data;
using LocalRag.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using ProcurementAiApi.LocalRAG.Application.Interfaces;
using ProcurementAiApi.LocalRAG.Infrastructure.Ollamas;
using ProcurementAiApi.LocalRAG.Infrastructure.OllamasService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<LocalRagDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("RagDb"),
        npgsql => npgsql.UseVector()));

//AI Part
builder.Services.AddScoped<LocalRag.Domain.RepositoryInterfaces.IVectorRepository, VectorRepository>();

builder.Services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:11434");
    client.Timeout = TimeSpan.FromMinutes(2);
});

builder.Services.AddHttpClient<ILlmService, OllamaLlmService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:11434");
    client.Timeout = TimeSpan.FromMinutes(5);
});



// Basic Part
builder.Services.AddInfrastructure();

builder.Services.AddMediatR(config =>
    config.RegisterServicesFromAssemblyContaining<SearchDocumentChunkQueryHandler>());

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(
        typeof(AddDocumentCommand).Assembly);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{

}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();