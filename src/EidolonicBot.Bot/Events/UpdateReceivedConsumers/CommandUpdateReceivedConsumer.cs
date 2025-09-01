namespace EidolonicBot.Events.UpdateReceivedConsumers;

public class CommandUpdateReceivedConsumer(
  IScopedMediator mediator,
  ILogger<CommandUpdateReceivedConsumer> logger,
  IHostEnvironment hostEnvironment,
  ITelegramBotClient botClient,
  IMemoryCache cache
) : IConsumer<UpdateReceived> {
  public async Task Consume(ConsumeContext<UpdateReceived> context) {
    var update = context.Message.Update;
    var cancellationToken = context.CancellationToken;

    if (update is not {
          Message : {
            Text: { } messageText,
            MessageId: var messageId,
            Chat.Id: var chatId
          }
        } || !messageText.StartsWith('/')) {
      return;
    }

    // split command from text arg passed as multiline plain text
    messageText = string.Join(' ', messageText.Split('\n', 2));

    var commandAndArgs = messageText.Split(' ');
    var commandAndUserName = commandAndArgs[0].Split('@', 2);
    switch (commandAndUserName.Length) {
      case 1 when update.Message.Chat.Type is not ChatType.Private && hostEnvironment.IsDevelopment():
        return;
      case 2: {
        var botUsername = await GetBotUsername(cancellationToken);
        if (commandAndUserName[1] != botUsername) {
          logger.LogDebug(
            "Command ignored die to wrong bot username Expected: {ExpectedUserName} Actual: {ActualUserName}",
            botUsername, commandAndUserName[1]);
          return;
        }
        break;
      }
    }

    var command = CommandHelpers.CommandByText.GetValueOrDefault(commandAndUserName[0], Command.Unknown);

    if (command is Command.Unknown) {
      return;
    }

    var args = commandAndArgs.Length >= 2 ? commandAndArgs[1..] : [];

    using var _ = logger.BeginScope("Command:{Command} Args:{args}", command, string.Join(' ', args));

    if (args.Length == 1 && args[0].Equals("help", StringComparison.InvariantCultureIgnoreCase)) {
      var help = CommandHelpers.HelpByCommand[command];
      if (help is not null) {
        await botClient.SendMessage(
          chatId,
          help,
          parseMode: ParseMode.MarkdownV2,
          replyParameters: messageId,
          replyMarkup: new ReplyKeyboardRemove(),
          cancellationToken: context.CancellationToken
        );

        return;
      }
    }

    await mediator.Publish(
      new BotCommandReceived(
        command,
        args,
        update.Message
      ), cancellationToken);
  }

  private async Task<string?> GetBotUsername(CancellationToken cancellationToken) {
    return await cache.GetOrCreateAsync(
      "BotUsername", async entry => {
        entry.Size = 1;
        entry.Priority = CacheItemPriority.NeverRemove;
        return (await botClient.GetMe(cancellationToken)).Username;
      });
  }
}
