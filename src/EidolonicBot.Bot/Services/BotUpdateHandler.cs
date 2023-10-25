using Telegram.Bot.Exceptions;

namespace EidolonicBot.Services;

internal class BotUpdateHandler : IUpdateHandler {
    private readonly ILogger<BotUpdateHandler> _logger;
    private readonly IServiceProvider _serviceProvider;

    public BotUpdateHandler(ILogger<BotUpdateHandler> logger, IServiceProvider serviceProvider) {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient _, Update update,
        CancellationToken cancellationToken) {
        using var updateScope = _logger.BeginScope("{@Update}", update);
        _logger.LogDebug("Update received");

        await using var scope = _serviceProvider.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IScopedMediator>();
        try {
            await mediator.Publish(new UpdateReceived(update), cancellationToken);
        } catch (Exception ex) {
            _logger.LogError(ex, "UpdateReceived failed: {@Update}", update);
        }
    }

    public async Task HandlePollingErrorAsync(ITelegramBotClient _, Exception exception,
        CancellationToken cancellationToken) {
        _logger.LogError(exception, "Telegram API Error");
        if (exception is ApiRequestException) {
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }
    }
}
