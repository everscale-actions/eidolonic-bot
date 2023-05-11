using EidolonicBot.Events;

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
                    typeof(SubscriptionReceivedConsumer).Assembly);
            });

        builder.AddSubscriptions();

        return builder;
    }
}
