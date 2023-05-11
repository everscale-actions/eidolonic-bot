using EidolonicBot.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EidolonicBot;

public static class HostApplicationBuilderExtensions {
    public static HostApplicationBuilder AddSubscriptions(this HostApplicationBuilder builder) {
        builder.Services.AddSingleton<ISubscriptionService, SubscriptionService>();
        builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<ISubscriptionService>());

        return builder;
    }
}
