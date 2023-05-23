using EidolonicBot.Events.SubscriptionServiceActivatedConsumers;
using EidolonicBot.Serilog;

namespace EidolonicBot;

public static class HostApplicationBuilderExtensions {
    public static HostApplicationBuilder AddLogging(this HostApplicationBuilder builder) {
        builder.Services.AddLogging(loggingBuilder => {
            loggingBuilder.AddConfiguration(builder.Configuration);
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Destructure.With<IncludePublicNotNullFieldsPolicy>()
                .Destructure.With<SerializeJsonElementPolicy>()
                .CreateLogger());
            loggingBuilder.AddSentry(options => options.Environment = builder.Environment.EnvironmentName);
        });
        return builder;
    }

    public static HostApplicationBuilder AddMemoryCache(this HostApplicationBuilder builder) {
        builder.Services.AddMemoryCache()
            .Configure<MemoryCacheOptions>(builder.Configuration.GetSection("MemoryCache"));
        return builder;
    }

    public static HostApplicationBuilder AddEverClient(this HostApplicationBuilder builder) {
        builder.Services.AddEverClient()
            .Configure<EverClientOptions>(builder.Configuration.GetSection("EverClient"))
            .Configure<FilePackageManagerOptions>(options =>
                options.PackagesPath =
                    Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "_contracts"));
        return builder;
    }

    public static HostApplicationBuilder AddMassTransit(this HostApplicationBuilder builder) {
        builder.Services
            .AddMassTransit(x => {
                x.AddConsumers(type => !type.IsAssignableTo(typeof(IMediatorConsumer)),
                    typeof(ShutdownApplicationSubscriptionServiceActivatedConsumer).Assembly);
                var amqpUri = builder.Configuration["AMQP_URI"];
                if (amqpUri is not null) {
                    x.UsingRabbitMq((context, cfg) => {
                        cfg.Host(amqpUri);
                        cfg.ConfigureEndpoints(context);
                    });
                } else {
                    x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
                }
            })
            .AddMediator(x => {
                x.AddConsumers(type => type.IsAssignableTo(typeof(IMediatorConsumer)),
                    typeof(ChatNotificationSubscriptionReceivedConsumer).Assembly,
                    typeof(CommandUpdateReceivedConsumer).Assembly);
            });
        return builder;
    }
}
