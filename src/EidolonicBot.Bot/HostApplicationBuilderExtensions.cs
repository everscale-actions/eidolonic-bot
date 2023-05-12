namespace EidolonicBot;

public static class HostApplicationBuilderExtensions {
    public static HostApplicationBuilder AddTelegramBot(this HostApplicationBuilder builder) {
        var botToken = builder.Configuration["Bot:Token"];
        if (botToken is null or "BOT_API_TOKEN_HERE") {
            throw new NullReferenceException(
                "Provide token of your Telegram bot with `Bot__Token` environment variable");
        }

        builder.Services.AddMemoryCache();
        builder.Services.AddHttpClient("TelegramBotClient")
            .AddTypedClient<ITelegramBotClient>(client => new TelegramBotClient(botToken, client));
        builder.Services.AddHostedService<BotInit>()
            .AddHostedService<PullingService>()
            .AddSingleton<IUpdateHandler, BotUpdateHandler>();
        return builder;
    }
}
