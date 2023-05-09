using EidolonicBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EidolonicBot.ModelConfigurations;

public class SubscriptionConfigurationSqlite : SubscriptionConfigurationBase {
    protected override void ConfigureByProvider(EntityTypeBuilder<Subscription> builder) {
        builder.Property(c => c.Address)
            .UseCollation("NOCASE");
    }
}