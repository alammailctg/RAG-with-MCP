using Microsoft.EntityFrameworkCore;

namespace ProcurementAiApi.Persistance
{
    public class RagDbContext : DbContext
    {
        public RagDbContext(DbContextOptions<RagDbContext> options) : base(options) { }
    }
}
