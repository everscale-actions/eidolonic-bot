namespace EidolonicBot;

public class SqliteDbContext(
  DbContextOptions<SqliteDbContext> options
) : AppDbContext(options) {
  protected override void OnModelCreating(ModelBuilder modelBuilder) {
    modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);

    base.OnModelCreating(modelBuilder);
  }
}
