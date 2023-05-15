namespace EidolonicBot.Events.BotCommandReceivedConsumers;

public class SubscriptionBotCommandReceivedConsumer : BotCommandReceivedConsumerBase {
    private readonly AppDbContext _db;
    private readonly IScopedMediator _mediator;

    public SubscriptionBotCommandReceivedConsumer(ITelegramBotClient botClient, IMemoryCache memoryCache, AppDbContext db,
        IScopedMediator mediator) : base(
        Command.Subscription, botClient,
        memoryCache) {
        _db = db;
        _mediator = mediator;
    }

    protected override async Task<string?> ConsumeAndGetReply(string[] args, Message message, long chatId,
        int messageThreadId, bool isAdmin,
        CancellationToken cancellationToken) {
        return args switch {
            ["add", { } address, ..]
                when isAdmin && Regex.TvmAddressRegex().IsMatch(address) =>
                await Subscribe(address, chatId, messageThreadId, GetMinDeltaByArgs(args), cancellationToken),

            ["edit", { } address, ..]
                when isAdmin && Regex.TvmAddressRegex().IsMatch(address) =>
                await EditSubscription(address, chatId, messageThreadId, GetMinDeltaByArgs(args), cancellationToken),

            ["remove", { } address]
                when isAdmin && Regex.TvmAddressRegex().IsMatch(address) =>
                await Unsubscribe(address, chatId, messageThreadId, cancellationToken),

            ["list"] => await GetSubscriptionList(chatId, messageThreadId, cancellationToken),
            _ => CommandHelpers.HelpByCommand[Command.Subscription]
        };
    }

    private static decimal GetMinDeltaByArgs(IReadOnlyList<string> args) {
        return args.Count >= 3 && decimal.TryParse(args[2].Replace(',', '.'), out var result1) ? result1 : 0;
    }

    private async Task<string?> EditSubscription(string address, long chatId, int messageThreadId, decimal minDelta,
        CancellationToken cancellationToken) {
        var subscription = await _db.Subscription.SingleAsync(s => s.Address == address, cancellationToken);

        var subscriptionByChat = await _db.SubscriptionByChat.FindAsync(
            new object?[] { chatId, messageThreadId, subscription.Id, cancellationToken },
            cancellationToken);

        if (subscriptionByChat is null) {
            return "Subscription wasn't found. Add new one first.";
        }

        subscriptionByChat.MinDelta = minDelta;

        _db.SubscriptionByChat.Update(subscriptionByChat);
        var savedEntries = await _db.SaveChangesAsync(cancellationToken);

        return savedEntries > 0
            ? $"`subscription for {address}` was updated"
            : $"`subscription for {address}` is up to date";
    }

    private async Task<string?> GetSubscriptionList(long chatId, int messageThreadId, CancellationToken cancellationToken) {
        var subscriptionStrings = await _db.SubscriptionByChat.Where(s => s.ChatId == chatId && s.MessageThreadId == messageThreadId)
            .Select(s => $"`{s.Subscription.Address} ``| {s.MinDelta}{Constants.Currency}`")
            .ToArrayAsync(cancellationToken);

        if (subscriptionStrings.Length == 0) {
            return "Get your first subscription with\n" +
                   " `/subscription add `address";
        }

        return $"`Address | MinDelta`\n" +
               $"{string.Join('\n', subscriptionStrings.Select<string, string>(s => s))}";
    }

    private async Task<string> Subscribe(string address, long chatId, int messageThreadId, decimal minDelta,
        CancellationToken cancellationToken) {
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
                    MessageThreadId = messageThreadId,
                    MinDelta = minDelta
                },
                cancellationToken);
        }

        var savedEntries = await _db.SaveChangesAsync(cancellationToken);

        await _mediator.Send(new ReloadSubscriptionService(), cancellationToken);

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

        await _mediator.Send(new ReloadSubscriptionService(), cancellationToken);

        return $"`{address}` removed from subscriptions";
    }
}
