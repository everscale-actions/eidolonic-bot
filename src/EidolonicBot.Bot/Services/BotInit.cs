namespace EidolonicBot.Services;

internal class BotInit : IHostedService {
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<BotInit> _logger;

    public BotInit(ITelegramBotClient botClient, ILogger<BotInit> logger) {
        _botClient = botClient;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken) {
        _logger.LogInformation("Initialize bot (commands, etc)");
        await InitCommands(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }

    private async Task InitCommands(CancellationToken cancellationToken) {
        var commands = CommandHelpers.CommandAttributeByCommand.Values
            .Where(d => d is { IsBotInitCommand: true })
            .Select(d => new BotCommand {
                Command = d!.Text,
                Description = d.Description ?? string.Empty
            });
        await _botClient.SetMyCommandsAsync(commands, cancellationToken: cancellationToken);
    }
}
