namespace EidolonicBot.Events;

public class SubscriptionReceivedConsumer : IConsumer<SubscriptionReceived>, IMediatorConsumer {
    private readonly ITelegramBotClient _bot;
    private readonly AppDbContext _db;

    public SubscriptionReceivedConsumer(AppDbContext db, ITelegramBotClient bot) {
        _db = db;
        _bot = bot;
    }

    public async Task Consume(ConsumeContext<SubscriptionReceived> context) {
        var @event = context.Message;
        var cancellationToken = context.CancellationToken;

        var chatIds = await _db.Subscription.Where(s => s.Address == @event.AccountAddr)
            .Select(s => s.SubscriptionByChat.Select(sbc => sbc.ChatId))
            .SelectMany(chatId => chatId)
            .ToArrayAsync(cancellationToken);

        var message =
            $"Transaction:` {@event.TransactionId}`\n" +
            $"Account:` {@event.AccountAddr}`\n" +
            $"Balance change:` {@event.BalanceChange}`";

        await Task.WhenAll(chatIds.Select(chatId =>
            _bot.SendTextMessageAsync(chatId, message, ParseMode.Markdown, cancellationToken: cancellationToken)));
    }
}
