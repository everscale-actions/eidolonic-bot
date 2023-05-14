namespace EidolonicBot.Events.BotCommandReceivedConsumers;

public class SubscriptionBotCommandReceivedConsumer : BotCommandReceivedConsumerBase {
    private readonly AppDbContext _db;
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionBotCommandReceivedConsumer(ITelegramBotClient botClient, IMemoryCache memoryCache, AppDbContext db,
        ISubscriptionService subscriptionService) : base(
        Command.Subscription, botClient,
        memoryCache) {
        _db = db;
        _subscriptionService = subscriptionService;
    }

    protected override async Task<string?> ConsumeAndGetReply(string[] args, Message message, long chatId,
        int messageThreadId, bool isAdmin,
        CancellationToken cancellationToken) {
        return args switch {
            ["add", { } address] when isAdmin && Regex.TvmAddressRegex().IsMatch(address) => await Subscribe(address, chatId, messageThreadId,
                cancellationToken),
            ["remove", { } address] when isAdmin && Regex.TvmAddressRegex().IsMatch(address) => await Unsubscribe(address, chatId,
                messageThreadId, cancellationToken),
            ["list"] => await GetSubscriptionList(chatId, messageThreadId, cancellationToken),
            _ => CommandHelpers.HelpByCommand[Command.Subscription]
        };
    }

    private async Task<string?> GetSubscriptionList(long chatId, int messageThreadId, CancellationToken cancellationToken) {
        var subscriptionStrings = await _db.SubscriptionByChat.Where(s => s.ChatId == chatId && s.MessageThreadId == messageThreadId)
            .Select(s => $"`{s.Subscription.Address}`")
            .ToArrayAsync(cancellationToken);

        if (subscriptionStrings.Length == 0) {
            return "Get your first subscription with\n" +
                   " `/subscription add `address";
        }

        return string.Join('\n', subscriptionStrings.Select<string, string>(s => s));
    }

    private async Task<string> Subscribe(string address, long chatId, int messageThreadId, CancellationToken cancellationToken) {
        var subscription = await _db.Subscription.FirstOrDefaultAsync(s => s.Address == address, cancellationToken);
        subscription ??= (await _db.Subscription.AddAsync(new Subscription { Address = address }, cancellationToken)).Entity;

        var subscriptionByChat = await _db.SubscriptionByChat.FindAsync(
            new object?[] { chatId, messageThreadId, subscription.Id, cancellationToken },
            cancellationToken);
        if (subscriptionByChat is null) {
            await _db.SubscriptionByChat.AddAsync(
                new SubscriptionByChat {
                    SubscriptionId = subscription.Id,
                    ChatId = chatId,
                    MessageThreadId = messageThreadId
                },
                cancellationToken);
        }

        var savedEntries = await _db.SaveChangesAsync(cancellationToken);

        await _subscriptionService.Restart(cancellationToken);

        return savedEntries > 0
            ? $"`{address}` added to subscriptions"
            : $"`{address}` is already added earlier";
    }

    private async Task<string> Unsubscribe(string address, long chatId, int messageThreadId, CancellationToken cancellationToken) {
        var subscription = await _db.Subscription.FirstOrDefaultAsync(s => s.Address == address, cancellationToken);
        if (subscription is null) {
            return "Subscription not found";
        }

        var subscriptionByChat = await _db.SubscriptionByChat.FindAsync(
            new object?[] { chatId, messageThreadId, subscription.Id, cancellationToken },
            cancellationToken);
        if (subscriptionByChat is null) {
            return "Subscription not found";
        }

        _db.SubscriptionByChat.Remove(subscriptionByChat);
        if (_db.SubscriptionByChat.Local.All(s => s.SubscriptionId != subscription.Id)) {
            _db.Subscription.Remove(subscription);
        }

        await _db.SaveChangesAsync(cancellationToken);

        await _subscriptionService.Restart(cancellationToken);

        return $"`{address}` removed from subscriptions";
    }
}
