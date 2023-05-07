using EidolonicBot.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace EidolonicBot;

public static class HostApplicationBuilderExtensions {
    public static HostApplicationBuilder AddTelegramBot<T>(this HostApplicationBuilder builder) where T : class, IUpdateHandler {
        var botToken = builder.Configuration["Bot:Token"];
        if (botToken is null or "BOT_API_TOKEN_HERE") {
            throw new NullReferenceException(
                "Provide token of your Telegram bot with `Bot__Token` environment variable");
        }

        builder.Services.AddHttpClient("TelegramBotClient")
            .AddTypedClient<ITelegramBotClient>(client => new TelegramBotClient(botToken, client));
        builder.Services.AddHostedService<BotInit>()
            .AddHostedService<PullingService>()
            .AddSingleton<IUpdateHandler, T>();

        return builder;
    }
}