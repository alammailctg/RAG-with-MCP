using LocalRag.Application.Features.Commands;
using LocalRag.Application.Features.QueryHandlers;
using LocalRag.Domain.RepositoryInterfaces;
using LocalRag.Infrastructure;
using LocalRag.Infrastructure.MCPServers;
using LocalRag.Infrastructure.Persistance.Data;
using LocalRag.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using ProcurementAiApi.LocalRAG.Application.Interfaces;
using ProcurementAiApi.LocalRAG.Infrastructure.Ollamas;

using ModelContextProtocol.AspNetCore;
using LocalRag.Infrastructure.OllamasService;

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

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<DatabaseTools>()
    .WithTools<VectorSearchTools>();
builder.Services.AddScoped<IVectorRepository, VectorRepository>();

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

builder.Services.AddInfrastructure();

 

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(AddDocumentCommand).Assembly);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Angular", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


var app = builder.Build();
app.UseCors("Angular");
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapMcp("/mcp");

app.Run();