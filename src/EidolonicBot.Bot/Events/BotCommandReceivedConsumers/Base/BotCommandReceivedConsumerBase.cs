namespace EidolonicBot.Events.BotCommandReceivedConsumers.Base;

public abstract class BotCommandReceivedConsumerBase : IConsumer<BotCommandReceived>, IMediatorConsumer {
    private readonly ITelegramBotClient _bot;
    private readonly Command _command;
    private readonly IMemoryCache _memoryCache;

    protected BotCommandReceivedConsumerBase(Command command, ITelegramBotClient bot, IMemoryCache memoryCache) {
        _command = command;
        _bot = bot;
        _memoryCache = memoryCache;
    }

    public async Task Consume(ConsumeContext<BotCommandReceived> context) {
        if (context.Message.Command != _command) {
            return;
        }

        var message = context.Message.Message;
        var cancellationToken = context.CancellationToken;

        if (message is not { Chat.Id: var chatId, From.Id: var fromId }) {
            return;
        }

        var isAdmin = chatId == fromId || await IsChatAdmin(chatId, fromId, cancellationToken);

        var replyText = await Consume(context.Message.Arguments, context.Message.Message, chatId, isAdmin,
            cancellationToken);

        if (replyText is null) {
            return;
        }

        await _bot.SendTextMessageAsync(
            message.Chat.Id,
            replyText,
            parseMode: ParseMode.Markdown,
            replyToMessageId: message.MessageId,
            cancellationToken: cancellationToken
        );
    }

    private async Task<bool> IsChatAdmin(long chatId, long userId, CancellationToken cancellationToken) {
        var cache = await _memoryCache.GetOrCreateAsync($"AdminIdsByChatId_{chatId}", async entry => {
            entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(1));
            entry.SetSize(1);
            var admins = await _bot.GetChatAdministratorsAsync(chatId, cancellationToken);
            return admins.Select(a => a.User.Id).ToArray();
        });

        return cache?.Contains(userId) ?? false;
    }

    protected abstract Task<string?> Consume(string[] args, Message message, long chatId, bool isAdmin,
        CancellationToken cancellationToken);
}
