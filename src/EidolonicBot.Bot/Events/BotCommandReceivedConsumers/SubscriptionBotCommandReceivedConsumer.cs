namespace EidolonicBot.Events.BotCommandReceivedConsumers;

public class SubscriptionBotCommandReceivedConsumer(
  ITelegramBotClient botClient,
  IMemoryCache memoryCache,
  AppDbContext db,
  IScopedMediator mediator,
  ILinkFormatter linkFormatter
)
  : BotCommandReceivedConsumerBase(
    Command.Subscription, botClient,
    memoryCache) {
  private readonly string[] _adminActions = ["add", "edit", "remove"];

  protected override async Task<string?> ConsumeAndGetReply(string[] args, Message message, long chatId,
    int messageThreadId, bool isAdmin,
    CancellationToken cancellationToken) {
    return args switch {
      [{ } action, ..]
        when _adminActions.Contains(action) && !isAdmin => "Only chat admin can control subscriptions"
          .ToEscapedMarkdownV2(),

      ["add", { } address, ..] when RegexList.TvmAddressRegex().IsMatch(address) =>
        await Subscribe(
          address, chatId, messageThreadId, GetMinDeltaByArgs(args), GetLabelByArgs(args),
          cancellationToken),

      ["edit", { } address, ..] when isAdmin && RegexList.TvmAddressRegex().IsMatch(address) =>
        await EditSubscription(
          address, chatId, messageThreadId, GetMinDeltaByArgs(args), GetLabelByArgs(args),
          cancellationToken),

      ["remove", { } address] when isAdmin && RegexList.TvmAddressRegex().IsMatch(address) =>
        await Unsubscribe(address, chatId, messageThreadId, cancellationToken),

      ["list", ..] => await GetSubscriptionList(
        chatId, messageThreadId,
        args is [_, "full", ..] or [_, "true", ..],
        cancellationToken),

      _ => CommandHelpers.HelpByCommand[Command.Subscription]
    };
  }

  private static string? GetLabelByArgs(IReadOnlyList<string> args) {
    return args.Count >= 4 ? args[3] : null;
  }

  private static decimal GetMinDeltaByArgs(IReadOnlyList<string> args) {
    return args.Count >= 3 && decimal.TryParse(args[2].Replace(',', '.'), out var result1) ? result1 : 0;
  }

  private async Task<string?> EditSubscription(string address, long chatId, int messageThreadId, decimal minDelta,
    string? label,
    CancellationToken cancellationToken) {
    var subscription = await db.Subscription.SingleAsync(s => s.Address == address, cancellationToken);

    var subscriptionByChat = await db.SubscriptionByChat.FindAsync(
      [chatId, messageThreadId, subscription.Id, cancellationToken],
      cancellationToken);

    if (subscriptionByChat is null) {
      return "Subscription wasn't found\\. Add new one first";
    }

    subscriptionByChat.MinDelta = minDelta;
    subscriptionByChat.Label = label;

    var savedEntries = await db.SaveChangesAsync(cancellationToken);

    return savedEntries > 0
      ? $"subscription for {linkFormatter.GetAddressLink(address)} was updated"
      : $"subscription for {linkFormatter.GetAddressLink(address)} is up to date";
  }

  private async Task<string?> GetSubscriptionList(long chatId, int messageThreadId, bool full,
    CancellationToken cancellationToken) {
    var subscriptionStrings = (await db.SubscriptionByChat
        .Where(s => s.ChatId == chatId && s.MessageThreadId == messageThreadId)
        .Select(
          s => new {
            s.Subscription.Address,
            MinDeltaStr = s.MinDelta.ToEvers(),
            s.Label
          })
        .ToArrayAsync(cancellationToken))
      .Select(
        s => full
          ? $@"`{s.Address}`` \| ``{s.MinDeltaStr}`` \| ``{s.Label?.ToEscapedMarkdownV2()}`"
          : $@"{linkFormatter.GetAddressLink(s.Address)} \| {s.MinDeltaStr} \| {s.Label?.ToEscapedMarkdownV2()}")
      .ToArray();

    if (subscriptionStrings.Length == 0) {
      return "Get your first subscription with\n" +
             " `/subscription add `address";
    }

    return "Address \\| MinDelta\\| Label\n" +
           $"{string.Join('\n', subscriptionStrings)}";
  }

  private async Task<string> Subscribe(string address, long chatId, int messageThreadId, decimal minDelta,
    string? label,
    CancellationToken cancellationToken) {
    var subscription = await db.Subscription.FirstOrDefaultAsync(s => s.Address == address, cancellationToken);
    subscription ??= (await db.Subscription.AddAsync(new Subscription { Address = address }, cancellationToken))
      .Entity;

    var subscriptionByChat = await db.SubscriptionByChat.FindAsync(
      [chatId, messageThreadId, subscription.Id, cancellationToken],
      cancellationToken);

    if (subscriptionByChat is null) {
      await db.SubscriptionByChat.AddAsync(
        new SubscriptionByChat {
          SubscriptionId = subscription.Id,
          ChatId = chatId,
          MessageThreadId = messageThreadId,
          MinDelta = minDelta,
          Label = label
        },
        cancellationToken);
    }

    var savedEntries = await db.SaveChangesAsync(cancellationToken);

    await mediator.Send(new ReloadSubscriptionService(), cancellationToken);

    return savedEntries > 0
      ? $"{linkFormatter.GetAddressLink(address)} added to subscriptions"
      : $"{linkFormatter.GetAddressLink(address)} is already added earlier";
  }

  private async Task<string> Unsubscribe(string address, long chatId, int messageThreadId,
    CancellationToken cancellationToken) {
    var subscription = await db.Subscription.FirstOrDefaultAsync(s => s.Address == address, cancellationToken);
    if (subscription is null) {
      return "Subscription not found";
    }

    switch (await db.SubscriptionByChat.CountAsync(
              s => s.SubscriptionId == subscription.Id,
              cancellationToken)) {
      case 0:
        return "Subscription not found";
      case 1:
        db.Subscription.Remove(subscription);
        break;
      default:
        var subscriptionByChat = await db.SubscriptionByChat.FindAsync(
          [chatId, messageThreadId, subscription.Id, cancellationToken],
          cancellationToken) ?? throw new InvalidOperationException();

        db.SubscriptionByChat.Remove(subscriptionByChat);
        break;
    }

    await db.SaveChangesAsync(cancellationToken);

    await mediator.Send(new ReloadSubscriptionService(), cancellationToken);

    return $"{linkFormatter.GetAddressLink(address)} removed from subscriptions";
  }
}
