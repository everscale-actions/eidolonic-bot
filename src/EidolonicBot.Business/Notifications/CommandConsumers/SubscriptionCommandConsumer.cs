namespace EidolonicBot.Notifications.CommandConsumers;

public class SubscriptionCommandConsumer : CommandConsumerBase {
    private readonly AppDbContext _db;
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionCommandConsumer(ITelegramBotClient botClient, IMemoryCache memoryCache, AppDbContext db,
        ISubscriptionService subscriptionService) : base(
        Command.Subscription, botClient,
        memoryCache) {
        _db = db;
        _subscriptionService = subscriptionService;
    }

    protected override async Task<string?> Consume(string[] args, Message message, long chatId, bool isAdmin,
        CancellationToken cancellationToken) {
        return args switch {
            ["add", { } address] when isAdmin && Regex.TvmAddressRegex().IsMatch(address) => await Subscribe(address, chatId, cancellationToken),
            ["remove", { } address] when isAdmin && Regex.TvmAddressRegex().IsMatch(address) => await Unsubscribe(address, chatId,
                cancellationToken),
            ["list"] => await GetSubscriptionList(chatId, cancellationToken),
            _ => CommandHelpers.CommandAttributeByCommand[Command.Subscription]?.Help
        };
    }

    private async Task<string?> GetSubscriptionList(long chatId, CancellationToken cancellationToken) {
        var subscriptionStrings = await _db.SubscriptionByChat.Where(s => s.ChatId == chatId)
            .Select(s => $"`{s.Subscription.Address}`")
            .ToArrayAsync(cancellationToken);

        if (subscriptionStrings.Length == 0) {
            return "Get your first subscription with\n" +
                   " `/subscription add `address";
        }

        return string.Join('\n', subscriptionStrings.Select(s => s));
    }

    private async Task<string> Subscribe(string address, long chatId, CancellationToken cancellationToken) {
        var subscription = await _db.Subscription.FirstOrDefaultAsync(s => s.Address == address, cancellationToken);
        subscription ??= (await _db.Subscription.AddAsync(new Subscription { Address = address }, cancellationToken)).Entity;

        var subscriptionByChat = await _db.SubscriptionByChat.FindAsync(new object?[] { chatId, subscription.Id, cancellationToken },
            cancellationToken);
        if (subscriptionByChat is null) {
            await _db.SubscriptionByChat.AddAsync(new SubscriptionByChat { SubscriptionId = subscription.Id, ChatId = chatId },
                cancellationToken);
        }

        var savedEntries = await _db.SaveChangesAsync(cancellationToken);

        await _subscriptionService.Restart(cancellationToken);

        return savedEntries > 0
            ? $"`{address}` added to subscriptions"
            : $"`{address}` is already added earlier";
    }

    private async Task<string> Unsubscribe(string address, long chatId, CancellationToken cancellationToken) {
        var subscription = await _db.Subscription.FirstOrDefaultAsync(s => s.Address == address, cancellationToken);
        if (subscription is null) {
            return "Subscription not found";
        }

        var subscriptionByChat = await _db.SubscriptionByChat.FindAsync(new object?[] { chatId, subscription.Id, cancellationToken },
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
