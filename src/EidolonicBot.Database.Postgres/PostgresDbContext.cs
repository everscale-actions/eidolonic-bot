namespace EidolonicBot;

public class PostgresDbContext : AppDbContext {
    public PostgresDbContext(DbContextOptions<PostgresDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.HasCollation("case_insensitive", "en-u-ks-primary", "icu", false);

        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }
}
