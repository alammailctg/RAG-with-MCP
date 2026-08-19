using LocalRag.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace LocalRag.Infrastructure.Persistance.Data
{
    public class LocalRagDbContext : DbContext
    {
        public LocalRagDbContext(
            DbContextOptions<LocalRagDbContext> options)
            : base(options)
        {
        }

        public DbSet<DocumentChunk> DocumentChunks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresExtension("vector");

            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(LocalRagDbContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }
    }
}