using System.Diagnostics.CodeAnalysis;

namespace EidolonicBot;

[SuppressMessage("ReSharper", "NullableWarningSuppressionIsUsed")]
[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
public abstract class AppDbContext(
  DbContextOptions options
) : DbContext(options) {
  public DbSet<Subscription> Subscription { get; set; } = null!;
  public DbSet<SubscriptionByChat> SubscriptionByChat { get; set; } = null!;
}
