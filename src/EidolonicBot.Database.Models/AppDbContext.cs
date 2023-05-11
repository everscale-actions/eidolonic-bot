namespace EidolonicBot;

public abstract class AppDbContext : DbContext {
    protected AppDbContext(DbContextOptions options) : base(options) { }

    public DbSet<Subscription> Subscription { get; set; } = null!;
    public DbSet<SubscriptionByChat> SubscriptionByChat { get; set; } = null!;
}
