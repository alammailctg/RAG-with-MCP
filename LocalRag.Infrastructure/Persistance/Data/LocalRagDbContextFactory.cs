using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LocalRag.Infrastructure.Persistance.Data
{
    public class ProcurementDbContextFactory
        : IDesignTimeDbContextFactory<LocalRagDbContext>
    {
        public LocalRagDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder =
                new DbContextOptionsBuilder<LocalRagDbContext>();

            var connectionString =
                "Host=localhost;" +
                "Port=5432;" +
                "Database=OllamaAiRag;" +
                "Username=postgres;" +
                "Password=postgres;" +
                "Include Error Detail=true";

            optionsBuilder.UseNpgsql(
                connectionString,
                npgsql => npgsql.UseVector());

            return new LocalRagDbContext(optionsBuilder.Options);
        }
    }
}