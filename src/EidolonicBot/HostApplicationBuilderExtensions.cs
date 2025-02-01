using System.Text.Json;
using EidolonicBot.Events.BotCommandReceivedConsumers;
using EidolonicBot.Events.SubscriptionReceivedConsumers;
using EidolonicBot.Events.SubscriptionServiceActivatedConsumers;
using EidolonicBot.Serilog;
using Serilog.Enrichers.Sensitive;
using Telegram.Bot;

namespace EidolonicBot;

public static class HostApplicationBuilderExtensions {
  public static HostApplicationBuilder AddLogging(this HostApplicationBuilder builder) {
    builder.Services.AddLogging(loggingBuilder => {
      loggingBuilder.AddConfiguration(builder.Configuration);
      loggingBuilder.ClearProviders();
      loggingBuilder.AddSerilog(
        new LoggerConfiguration()
          .ReadFrom.Configuration(builder.Configuration)
          .Enrich.WithSensitiveDataMasking(options => {
            options.MaskingOperators.Clear();
            options.MaskProperties.Clear();
            options.MaskingOperators.Add(
              new RegexWithSecretMaskingOperator(@"https:\/\/api.telegram.org\/bot(?'secret'.*?)\/")
            );
          })
          .Destructure.With<IncludePublicNotNullFieldsPolicy>()
          .Destructure.With<SerializeJsonElementPolicy>()
          .CreateLogger());

      loggingBuilder.AddSentry(options => {
        if (builder.Environment.IsDevelopment()) {
          options.Dsn = "";
          return;
        }

        options.Environment = builder.Environment.EnvironmentName;
      });
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
          Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException(), "_contracts"));

    return builder;
  }

  public static HostApplicationBuilder AddMassTransit(this HostApplicationBuilder builder) {
    builder.Services
      .AddMassTransit(x => {
        x.AddConsumers(
          type => !type.IsAssignableTo(typeof(IMediatorConsumer)),
          typeof(ShutdownApplicationSubscriptionServiceActivatedConsumer).Assembly,
          typeof(CommandUpdateReceivedConsumer).Assembly);

        var amqpUri = builder.Configuration["AMQP_URI"];
        if (amqpUri is not null) {
          x.UsingRabbitMq((context, cfg) => {
            cfg.Host(amqpUri);
            cfg.ConfigureJsonSerializerOptions(AddJsonBotApiJsonSerializerOptions);
            cfg.ConfigureEndpoints(context);
          });
        }
        else {
          x.UsingInMemory((context, cfg) => {
            cfg.ConfigureJsonSerializerOptions(AddJsonBotApiJsonSerializerOptions);
            cfg.ConfigureEndpoints(context);
          });
        }
      })
      .AddMediator(x => {
        x.AddConsumers(
          type => type.IsAssignableTo(typeof(IMediatorConsumer)),
          typeof(ChatNotificationSubscriptionReceivedConsumer).Assembly,
          typeof(HelpBotCommandReceivedConsumer).Assembly);

        x.ConfigureMediator((context, cfg) =>
          cfg.UsePublishFilter(typeof(InitUserWalletOnBotCommandReceivedFilter<>), context));
      });

    return builder;
  }

  private static JsonSerializerOptions AddJsonBotApiJsonSerializerOptions(JsonSerializerOptions options) {
    JsonBotAPI.Configure(options);
    return options;
  }
}
