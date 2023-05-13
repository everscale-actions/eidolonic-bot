using EidolonicBot.Configurations;

namespace EidolonicBot;

public static class HostApplicationBuilderExtensions {
    public static HostApplicationBuilder AddSubscriptions(this HostApplicationBuilder builder) {
        builder.Services.AddSingleton<ISubscriptionService, SubscriptionService>();
        builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<ISubscriptionService>());

        builder.Services.Configure<BlockchainOptions>(builder.Configuration.GetSection("Blockchain"));
        
        return builder;
    }
}