using Microsoft.EntityFrameworkCore;

namespace ProcurementAiApi.LocalRAG.Infrastructure.Persistance
{
    public class RagDbContext : DbContext
    {
        public RagDbContext(DbContextOptions<RagDbContext> options) : base(options) { }
    }
}
