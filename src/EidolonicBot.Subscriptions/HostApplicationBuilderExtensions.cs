using EidolonicBot.Configurations;

namespace EidolonicBot;

public static class HostApplicationBuilderExtensions {
    public static HostApplicationBuilder AddSubscriptions(this HostApplicationBuilder builder) {
        builder.Services.Configure<BlockchainOptions>(builder.Configuration.GetSection("Blockchain"));

        builder.Services.AddSingleton<ISubscriptionService, SubscriptionService>()
            .AddSingleton<IHostedService>(sp => sp.GetRequiredService<ISubscriptionService>());

        return builder;
    }
}
