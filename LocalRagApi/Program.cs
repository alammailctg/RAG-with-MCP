using LocalRag.Application.Features.Commands;
using LocalRag.Application.Features.QueryHandlers;
using LocalRag.Infrastructure;
using LocalRag.Infrastructure.MCPServers;
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
        npgsql =>
        {
            npgsql.UseVector();
            npgsql.CommandTimeout(60);
        }));


builder.Services.AddScoped<
    LocalRag.Domain.RepositoryInterfaces.IVectorRepository,
    VectorRepository>();

builder.Services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>(
client =>
{
    client.BaseAddress =
        new Uri("http://localhost:11434");

    client.Timeout =
        TimeSpan.FromMinutes(2);
});

builder.Services.AddHttpClient<ILlmService, OllamaLlmService>(
client =>
{
    client.BaseAddress =
        new Uri("http://localhost:11434");

    client.Timeout =
        TimeSpan.FromMinutes(5);
});

builder.Services.AddInfrastructure();

builder.Services
    .AddMcpServer()
    .WithTools<DatabaseTools>()
    .WithTools<VectorSearchTools>();
    //.WithTools<ERPTools>();

builder.Services.AddScoped<DatabaseTools>();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(
        typeof(AddDocumentCommand).Assembly);

    cfg.RegisterServicesFromAssembly(
        typeof(SearchDocumentChunkQueryHandler).Assembly);
});

var app = builder.Build();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapMcp();

app.Run();