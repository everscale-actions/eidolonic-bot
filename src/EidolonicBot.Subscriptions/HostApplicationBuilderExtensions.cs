namespace EidolonicBot;

public static class HostApplicationBuilderExtensions {
    public static HostApplicationBuilder AddSubscriptions(this HostApplicationBuilder builder) {
        builder.Services.AddSingleton<ISubscriptionService, SubscriptionService>()
            .AddSingleton<IHostedService>(sp => sp.GetRequiredService<ISubscriptionService>());

        return builder;
    }
}
