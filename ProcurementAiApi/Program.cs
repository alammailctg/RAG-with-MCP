
using Microsoft.EntityFrameworkCore;
using ProcurementAiApi.Persistance;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();

builder.Services.AddOpenApi();
builder.Services.AddDbContext<RagDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("RagDb")));
var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
