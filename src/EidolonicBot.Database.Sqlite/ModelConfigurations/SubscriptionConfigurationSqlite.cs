namespace EidolonicBot.ModelConfigurations;

public class SubscriptionConfigurationSqlite : SubscriptionConfigurationBase {
  protected override void ConfigureByProvider(EntityTypeBuilder<Subscription> builder) {
    builder.Property(c => c.Address)
      .UseCollation("NOCASE");
  }
}
