using Microsoft.EntityFrameworkCore;

namespace EidolonicBot;

public class PostgresDbContext : AppDbContext {
    public PostgresDbContext(DbContextOptions<PostgresDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }
}