using MassTransit;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace EidolonicBot.Events.SubscriptionReceivedConsumers;

public class ChatNotificationSubscriptionReceivedConsumer : IConsumer<SubscriptionReceived>, IMediatorConsumer {
    private readonly ITelegramBotClient _bot;
    private readonly AppDbContext _db;
    private readonly ILinkFormatter _linkFormatter;

    public ChatNotificationSubscriptionReceivedConsumer(AppDbContext db, ITelegramBotClient bot, ILinkFormatter linkFormatter) {
        _db = db;
        _bot = bot;
        _linkFormatter = linkFormatter;
    }

    public async Task Consume(ConsumeContext<SubscriptionReceived> context) {
        var transaction = context.Message;
        var cancellationToken = context.CancellationToken;

        var chatAndThreadIds = await _db.Subscription
            .Where(s => s.Address == transaction.AccountAddr)
            .SelectMany(s => s.SubscriptionByChat
                .Where(sbc => sbc.MinDelta <= Math.Abs(transaction.BalanceDelta))
                .Select(sbc => new { sbc.ChatId, sbc.MessageThreadId, sbc.MinDelta }))
            .ToArrayAsync(cancellationToken);

        var links = _linkFormatter.GetTransactionLinks(transaction.TransactionId);

        var from = transaction.From is not null ? $"from: {_linkFormatter.GetAddressLink(transaction.From)}\n" : null;
        var to = transaction.To.Length > 0 ? $"to: {string.Join(',', transaction.To.Select(t => _linkFormatter.GetAddressLink(t)))}\n" : null;

        var message =
            $"❕Subscription alert ❕\n" +
            $"address: {_linkFormatter.GetAddressLink(transaction.AccountAddr)}\n" +
            from +
            to +
            $"balance: {transaction.Balance}{Constants.Currency}\n" +
            $"delta: {transaction.BalanceDelta}{Constants.Currency}\n" +
            string.Join(" | ", links);

        await Task.WhenAll(chatAndThreadIds.Select(chat =>
            _bot.SendTextMessageAsync(chat.ChatId,
                message,
                chat.MessageThreadId,
                ParseMode.Markdown,
                disableWebPagePreview: true,
                cancellationToken: cancellationToken)));
    }
}
