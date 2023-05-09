using EidolonicBot.Abstract;
using EidolonicBot.Notifications.UpdateConsumers;
using EidolonicBot.Services;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EidolonicBot;

public static class HostApplicationBuilderExtensions {
    public static HostApplicationBuilder AddBusiness(this HostApplicationBuilder builder) {
        builder.Services.AddSingleton<IStaticService, StaticService>();
        builder.Services
            .AddMassTransit(x =>
                x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context))
            )
            .AddMediator(x => {
                x.AddConsumers(type => type.IsAssignableTo(typeof(IMediatorConsumer)),
                    typeof(SendCommandNotificationConsumer).Assembly);
            });

        builder.Services.AddSingleton<ISubscriptionService, SubscriptionService>();
        builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<ISubscriptionService>());

        return builder;
    }
}