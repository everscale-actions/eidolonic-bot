using EidolonicBot.Events.BotCommandReceivedConsumers;
using EidolonicBot.Events.SubscriptionReceivedConsumers;
using EidolonicBot.Events.SubscriptionServiceActivatedConsumers;
using EidolonicBot.Serilog;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog.Enrichers.Sensitive;

namespace EidolonicBot;

public static class HostApplicationBuilderExtensions {
	public static HostApplicationBuilder AddLogging(this HostApplicationBuilder builder) {
		builder.Services.AddLogging(loggingBuilder => {
			loggingBuilder.AddConfiguration(builder.Configuration);
			loggingBuilder.ClearProviders();
			loggingBuilder.AddSerilog(new LoggerConfiguration()
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
				                                             Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "_contracts"));
		return builder;
	}

	public static HostApplicationBuilder AddMassTransit(this HostApplicationBuilder builder) {
		builder.Services
		       .AddMassTransit(x => {
			       x.AddConsumers(type => !type.IsAssignableTo(typeof(IMediatorConsumer)),
			                      typeof(ShutdownApplicationSubscriptionServiceActivatedConsumer).Assembly,
			                      typeof(CommandUpdateReceivedConsumer).Assembly);
			       var amqpUri = builder.Configuration["AMQP_URI"];
			       if (amqpUri is not null) {
				       x.UsingRabbitMq((context, cfg) => {
					       cfg.Host(amqpUri);
					       ConfigureNewtonsoft(cfg);
					       cfg.ConfigureEndpoints(context);
				       });
			       } else {
				       x.UsingInMemory((context, cfg) => {
					       ConfigureNewtonsoft(cfg);
					       cfg.ConfigureEndpoints(context);
				       });
			       }
		       })
		       .AddMediator(x => {
			       x.AddConsumers(type => type.IsAssignableTo(typeof(IMediatorConsumer)),
			                      typeof(ChatNotificationSubscriptionReceivedConsumer).Assembly,
			                      typeof(HelpBotCommandReceivedConsumer).Assembly);
			       x.ConfigureMediator((context, cfg) =>
				                           cfg.UsePublishFilter(typeof(InitUserWalletOnBotCommandReceivedFilter<>), context));
		       });
		return builder;
	}

	private static void ConfigureNewtonsoft(IBusFactoryConfigurator cfg) {
		cfg.UseNewtonsoftJsonSerializer();
		cfg.ConfigureNewtonsoftJsonSerializer(_ => new JsonSerializerSettings {
			NullValueHandling = NullValueHandling.Include,
			ContractResolver = new CamelCasePropertyNamesContractResolver {
				IgnoreSerializableAttribute = true,
				IgnoreShouldSerializeMembers = true
			},
			DateFormatHandling = DateFormatHandling.IsoDateFormat,
			DateTimeZoneHandling = DateTimeZoneHandling.Unspecified
		});
	}
}
