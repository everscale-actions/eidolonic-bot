using EidolonicBot.Events.SubscriptionReceivedConsumers;

namespace EidolonicBot;

public static class HostApplicationBuilderExtensions {
    public static HostApplicationBuilder AddLogging(this HostApplicationBuilder builder) {
        builder.Services.AddLogging(loggingBuilder => {
            loggingBuilder.AddConfiguration(builder.Configuration);
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
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
            .AddMassTransit(x =>
                x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context))
            )
            .AddMediator(x => {
                x.AddConsumers(type => type.IsAssignableTo(typeof(IMediatorConsumer)),
                    typeof(SubscriptionReceivedConsumer).Assembly,
                    typeof(CommandUpdateReceivedConsumer).Assembly);
                x.ConfigureMediator((context, cfg) =>
                    cfg.UsePublishFilter(typeof(InitUserWalletOnBotCommandReceivedFilter<>), context));
            });
        return builder;
    }
}
