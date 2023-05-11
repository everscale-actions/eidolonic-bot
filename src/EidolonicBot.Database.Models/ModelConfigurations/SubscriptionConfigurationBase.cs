namespace EidolonicBot.ModelConfigurations;

public abstract class SubscriptionConfigurationBase : IEntityTypeConfiguration<Subscription> {
    public void Configure(EntityTypeBuilder<Subscription> builder) {
        ConfigureByProvider(builder);
    }

    protected abstract void ConfigureByProvider(EntityTypeBuilder<Subscription> builder);
}
