using EidolonicBot.Configurations;
using MassTransit;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace EidolonicBot.Events.SubscriptionReceivedConsumers;

public class SubscriptionReceivedConsumer : IConsumer<SubscriptionReceived>, IMediatorConsumer {
    private readonly BlockchainOptions _blockchainOptions;
    private readonly ITelegramBotClient _bot;
    private readonly AppDbContext _db;

    public SubscriptionReceivedConsumer(AppDbContext db, ITelegramBotClient bot, IOptions<BlockchainOptions> blockchainOptionsAccessor) {
        _db = db;
        _bot = bot;
        _blockchainOptions = blockchainOptionsAccessor.Value;
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

        var links = _blockchainOptions.Explorers
            .Select(e => $"[{e.Name}]({string.Format(e.TransactionLinkTemplate, transaction.TransactionId)})");

        var message =
            $"❕Subscription alert ❕\n" +
            $"address:` {transaction.AccountAddr}`\n" +
            $"{(transaction.BalanceDelta > 0 ? "from" : "to")}:` {transaction.Сounterparty}`\n" +
            $"balance: ` {transaction.Balance}{Constants.Currency}`\n" +
            $"delta: ` {transaction.BalanceDelta}{Constants.Currency}`\n" +
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
