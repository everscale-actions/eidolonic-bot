namespace EidolonicBot.Events.BotCommandReceivedConsumers;

public class SubscriptionBotCommandReceivedConsumer : BotCommandReceivedConsumerBase {
    private readonly string[] _adminActions = { "add", "edit", "remove" };

    private readonly AppDbContext _db;
    private readonly ILinkFormatter _linkFormatter;
    private readonly IScopedMediator _mediator;

    public SubscriptionBotCommandReceivedConsumer(ITelegramBotClient botClient, IMemoryCache memoryCache, AppDbContext db,
        IScopedMediator mediator, ILinkFormatter linkFormatter) : base(
        Command.Subscription, botClient,
        memoryCache) {
        _db = db;
        _mediator = mediator;
        _linkFormatter = linkFormatter;
    }

    protected override async Task<string?> ConsumeAndGetReply(string[] args, Message message, long chatId,
        int messageThreadId, bool isAdmin,
        CancellationToken cancellationToken) {
        return args switch {
            [{ } action, ..]
                when _adminActions.Contains(action) && !isAdmin => "Only chat admin can control subscriptions".ToEscapedMarkdownV2(),

            ["add", { } address, ..] when Regex.TvmAddressRegex().IsMatch(address) =>
                await Subscribe(address, chatId, messageThreadId, GetMinDeltaByArgs(args), cancellationToken),

            ["edit", { } address, ..] when isAdmin && Regex.TvmAddressRegex().IsMatch(address) =>
                await EditSubscription(address, chatId, messageThreadId, GetMinDeltaByArgs(args), cancellationToken),

            ["remove", { } address] when isAdmin && Regex.TvmAddressRegex().IsMatch(address) =>
                await Unsubscribe(address, chatId, messageThreadId, cancellationToken),

            ["list", ..] => await GetSubscriptionList(chatId, messageThreadId, args is [_, "full", ..] or [_, "true", ..], cancellationToken),

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
            return "Subscription wasn't found\\. Add new one first";
        }

        subscriptionByChat.MinDelta = minDelta;

        _db.SubscriptionByChat.Update(subscriptionByChat);
        var savedEntries = await _db.SaveChangesAsync(cancellationToken);

        return savedEntries > 0
            ? $"subscription for {_linkFormatter.GetAddressLink(address)} was updated"
            : $"subscription for {_linkFormatter.GetAddressLink(address)} is up to date";
    }

    private async Task<string?> GetSubscriptionList(long chatId, int messageThreadId, bool full, CancellationToken cancellationToken) {
        var subscriptionStrings = (await _db.SubscriptionByChat.Where(s => s.ChatId == chatId && s.MessageThreadId == messageThreadId)
                .Select(s => new {
                    s.Subscription.Address,
                    MinDeltaStr = s.MinDelta.ToString(CultureInfo.CurrentCulture).ToEscapedMarkdownV2()
                })
                .ToArrayAsync(cancellationToken))
            .Select(s => full
                ? $"`{s.Address}`` | ``{s.MinDeltaStr}{Constants.Currency}`"
                : $"{_linkFormatter.GetAddressLink(s.Address)} \\| {s.MinDeltaStr}{Constants.Currency}")
            .ToArray();

        if (subscriptionStrings.Length == 0) {
            return "Get your first subscription with\n" +
                   " `/subscription add `address";
        }

        return "Address \\| MinDelta\n" +
               $"{string.Join('\n', subscriptionStrings)}";
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
            ? $"{_linkFormatter.GetAddressLink(address)} added to subscriptions"
            : $"{_linkFormatter.GetAddressLink(address)} is already added earlier";
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

        return $"{_linkFormatter.GetAddressLink(address)} removed from subscriptions";
    }
}
