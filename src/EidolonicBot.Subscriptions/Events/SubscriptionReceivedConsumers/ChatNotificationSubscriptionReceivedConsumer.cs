using MassTransit;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace EidolonicBot.Events.SubscriptionReceivedConsumers;

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
        
        var chatAndThreadIds = await _db.Subscription
            .Where(s => s.Address == @event.AccountAddr)
            .SelectMany(s => s.SubscriptionByChat.Select(sbc => new { sbc.ChatId, sbc.MessageThreadId }))
            .ToArrayAsync(cancellationToken: cancellationToken);

        var message =
            $"Transaction:` {@event.TransactionId}`\n" +
            $"Account:` {@event.AccountAddr}`\n" +
            $"Balance change:` {@event.BalanceChange}`";

        await Task.WhenAll(chatAndThreadIds.Select(chat =>
            _bot.SendTextMessageAsync(chat.ChatId, message, chat.MessageThreadId, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken)));
    }
}
