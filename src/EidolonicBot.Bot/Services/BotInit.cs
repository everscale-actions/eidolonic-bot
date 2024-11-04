namespace EidolonicBot.Services;

internal class BotInit(
  ITelegramBotClient botClient,
  ILogger<BotInit> logger
) : IHostedService {
  public async Task StartAsync(CancellationToken cancellationToken) {
    logger.LogInformation("Initialize bot (commands, etc)");
    await InitCommands(cancellationToken);
  }

  public Task StopAsync(CancellationToken cancellationToken) {
    return Task.CompletedTask;
  }

  private async Task InitCommands(CancellationToken cancellationToken) {
    var commands = CommandHelpers.CommandAttributeByCommand.Values
      .Where(d => d is { IsBotInitCommand: true })
      .Select(
        d => new BotCommand {
          Command = d.Text,
          Description = d.Description ?? string.Empty
        });

    await botClient.SetMyCommands(commands, cancellationToken: cancellationToken);
  }
}
