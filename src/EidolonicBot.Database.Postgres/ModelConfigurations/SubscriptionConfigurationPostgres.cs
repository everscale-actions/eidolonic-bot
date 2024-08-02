namespace EidolonicBot.ModelConfigurations;

public class SubscriptionConfigurationPostgres : SubscriptionConfigurationBase {
  protected override void ConfigureByProvider(EntityTypeBuilder<Subscription> builder) {
    builder.Property(c => c.Address)
      .UseCollation("case_insensitive");
  }
}
