namespace EidolonicBot.Events.BotCommandReceivedConsumers;

public class StartCommandConsumer : BotCommandReceivedConsumerBase {
    private readonly ITelegramBotClient _botClient;

    public StartCommandConsumer(ITelegramBotClient botClient, IMemoryCache memoryCache) : base(Command.Start, botClient, memoryCache) {
        _botClient = botClient;
    }

    protected override async Task<string?> ConsumeAndGetReply(string[] args, Message message, long chatId, int messageThreadId, bool isAdmin, CancellationToken cancellationToken) {
        if (message is not { From.Id: var fromId }
            || chatId != fromId) {
            return null;
        }

        var commandMenu = CommandHelpers.CommandAttributeByCommand
            .Where(pair => pair.Value is not null && pair.Value.IsInlineCommand)
            .Select(pair => new InlineKeyboardButton(pair.Key.ToString()) {
                CallbackData = pair.Key.ToString()
            })
            .ToArray();

        var commandRows = commandMenu.Split(3).ToArray();

        await _botClient.SendTextMessageAsync(chatId, "Main menu",
            replyMarkup: new InlineKeyboardMarkup(commandRows), cancellationToken: cancellationToken);

        return null;
    }
}