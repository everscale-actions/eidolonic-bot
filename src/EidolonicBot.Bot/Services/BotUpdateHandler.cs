using Telegram.Bot.Exceptions;

namespace EidolonicBot.Services;

internal class BotUpdateHandler(
  ILogger<BotUpdateHandler> logger,
  IServiceProvider serviceProvider
) : IUpdateHandler {
  public async Task HandleUpdateAsync(ITelegramBotClient _, Update update,
    CancellationToken cancellationToken) {
    using var updateScope = logger.BeginScope("{@Update}", update);
    logger.LogDebug("Update received");

    await using var scope = serviceProvider.CreateAsyncScope();
    var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
    try {
      await publishEndpoint.Publish(new UpdateReceived(update), cancellationToken);
    }
    catch (Exception ex) {
      logger.LogError(ex, "UpdateReceived failed: {@Update}", update);
    }
  }

  public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source,
    CancellationToken cancellationToken) {
    if (exception is ApiRequestException { ErrorCode: 409 }) {
      logger.LogWarning(exception, "Telegram API Error");
      await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
    }
    logger.LogError(exception, "Telegram API Error");
  }
}
