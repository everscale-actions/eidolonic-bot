namespace EidolonicBot;

public abstract class AppDbContext(
  DbContextOptions options
) : DbContext(options) {
  public DbSet<Subscription> Subscription { get; set; } = null!;
  public DbSet<SubscriptionByChat> SubscriptionByChat { get; set; } = null!;
}
