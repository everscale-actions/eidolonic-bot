namespace EidolonicBot;

public class PostgresDbContext(
  DbContextOptions<PostgresDbContext> options
) : AppDbContext(options) {
  protected override void OnModelCreating(ModelBuilder modelBuilder) {
    modelBuilder.HasCollation("case_insensitive", "en-u-ks-primary", "icu", false);

    modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);

    base.OnModelCreating(modelBuilder);
  }
}
