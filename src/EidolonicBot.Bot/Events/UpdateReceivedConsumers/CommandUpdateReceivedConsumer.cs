namespace EidolonicBot.Events.UpdateReceivedConsumers;

public class CommandUpdateReceivedConsumer : IConsumer<UpdateReceived>, IMediatorConsumer {
    private readonly ITelegramBotClient _botClient;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<CommandUpdateReceivedConsumer> _logger;
    private readonly IScopedMediator _mediator;
    private readonly IMemoryCache _cache;

    public CommandUpdateReceivedConsumer(IScopedMediator mediator,
        ILogger<CommandUpdateReceivedConsumer> logger, IHostEnvironment hostEnvironment,
        ITelegramBotClient botClient, IMemoryCache cache) {
        _mediator = mediator;
        _logger = logger;
        _hostEnvironment = hostEnvironment;
        _botClient = botClient;
        _cache = cache;
    }

    public async Task Consume(ConsumeContext<UpdateReceived> context) {
        var update = context.Message.Update;
        var cancellationToken = context.CancellationToken;

        if (update is not {
                Message : {
                    Text: { } messageText,
                    MessageId: var messageId,
                    Chat.Id: var chatId,
                    From.Id: var userId
                }
            } || !messageText.StartsWith('/')) {
            return;
        }

        var commandAndArgs = messageText.Split(' ');
        var commandAndUserName = commandAndArgs[0].Split('@', 2);
        switch (commandAndUserName.Length) {
            case 1 when update.Message.Chat.Type is not ChatType.Private && _hostEnvironment.IsDevelopment():
                return;
            case 2: {
                var botUsername = await GetBotUsername(cancellationToken);
                if (commandAndUserName[1] != botUsername) {
                    _logger.LogDebug("Command ignored die to wrong bot username Expected: {ExpectedUserName} Actual: {ActualUserName}",
                        botUsername, commandAndUserName[1]);
                    return;
                }

                break;
            }
        }

        var command = CommandHelpers.CommandByText.TryGetValue(commandAndUserName[0], out var cmd)
            ? cmd
            : Command.Unknown;
        var args = commandAndArgs.Length >= 2 ? commandAndArgs[1..] : Array.Empty<string>();

        if (args.Length == 0 || (args.Length == 1 && args[0].Equals("help", StringComparison.OrdinalIgnoreCase))) {
            var help = CommandHelpers.CommandAttributeByCommand[command]?.Help;
            if (help is not null) {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    help,
                    parseMode: ParseMode.Markdown,
                    replyToMessageId: messageId,
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: context.CancellationToken);
                return;
            }
        }

        await _mediator.Publish(new BotCommandReceived(
            Command: command,
            Arguments: args,
            Message: update.Message
        ), cancellationToken);
    }

    private async Task<string?> GetBotUsername(CancellationToken cancellationToken) {
        return await _cache.GetOrCreateAsync("BotUsername", async entry => {
            entry.Size = 1;
            entry.Priority = CacheItemPriority.NeverRemove;
            return (await _botClient.GetMeAsync(cancellationToken: cancellationToken)).Username;
        });
    }
}
