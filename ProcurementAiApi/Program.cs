
using Microsoft.EntityFrameworkCore;
using ProcurementAiApi.LocalRAG.Application.Interfaces;
using ProcurementAiApi.LocalRAG.Infrastructure.Ollamas;
using ProcurementAiApi.LocalRAG.Infrastructure.Persistance;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();

builder.Services.AddOpenApi();
builder.Services.AddDbContext<RagDbContext>(options => options
.UseNpgsql(builder.Configuration.GetConnectionString("RagDb")));
builder.Services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>
    (client => { client.BaseAddress = new Uri("http://localhost:11434"); });
builder.Services.AddHttpClient<ILlmService, OllamaLlmService>
    (client => { client.BaseAddress = new Uri("http://localhost:11434"); });
var app = builder.Build();

app.MapDefaultEndpoints();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
