using MassTransit;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace EidolonicBot.Events.SubscriptionReceivedConsumers;

public class ChatNotificationSubscriptionReceivedConsumer(
  AppDbContext db,
  ITelegramBotClient bot,
  ILinkFormatter linkFormatter
) : IConsumer<SubscriptionReceived>, IMediatorConsumer {
  private const string WhaleSymbol = "\ud83d\udc33";
  private const string DolphinSymbol = "\ud83d\udc0b";

  private readonly string[] _alertMessages = [
    "Dude, check this out!",
    "Man, take a look at this!",
    "Hey, have a look at this, bro!",
    "Yo, peep this, homie!",
    "Bruh, feast your eyes on this!",
    "Bro, check this out!",
    "Dude, look at this!",
    "Hey man, see this!",
    "Yo, look over here!",
    "Buddy, take a gander at this!",
    "Pal, you gotta see this!",
    "Check this out, bro!",
    "Man, you need to see this!",
    "Yo, take a look at this!",
    "Bro, you won’t believe this!"
  ];

  public async Task Consume(ConsumeContext<SubscriptionReceived> context) {
    var (transactionId, address, balanceDelta, from, to, balance) = context.Message;
    var cancellationToken = context.CancellationToken;

    var chatAndThreadIds = await db.Subscription
      .Where(s => s.Address == address)
      .SelectMany(s => s.SubscriptionByChat
        .Where(sbc => sbc.MinDelta <= Math.Abs(balanceDelta))
        .Select(sbc => new { sbc.ChatId, sbc.MessageThreadId, sbc.MinDelta }))
      .ToArrayAsync(cancellationToken);

    var links = linkFormatter.GetTransactionLinks(transactionId)
      .Append(linkFormatter.GetAddressLink(address, "snipa.finance", "snipa.finance"))
      .ToArray();


    await Task.WhenAll(
      chatAndThreadIds.Select(async chat => {
        var label = (await db.LabelByChat.FindAsync([chat.ChatId, chat.MessageThreadId, address], cancellationToken))?.Label;
        var toLabels = await Task.WhenAll(to.Select(async t => new { address = t, label = (await db.LabelByChat.FindAsync([chat.ChatId, chat.MessageThreadId, t], cancellationToken))?.Label }));
        var fromLabel = from is not null ? (await db.LabelByChat.FindAsync([chat.ChatId, chat.MessageThreadId, from], cancellationToken))?.Label : null;


        var addressLink = label is null
          ? linkFormatter.GetAddressLink(address)
          : linkFormatter.GetAddressLink(address, label);
        var fromLink = from is not null ? $" \u2b05\ufe0f {(fromLabel is null ? linkFormatter.GetAddressLink(from) : linkFormatter.GetAddressLink(from, fromLabel))}" : null;
        var toLink = from is null && to.Length > 0 ? $" \u27a1\ufe0f {string.Join(',', toLabels.Select(t => t.label is null ? linkFormatter.GetAddressLink(t.address) : linkFormatter.GetAddressLink(t.address, t.label)))}" : null;
        var correspondentLink = fromLink + toLink;

        await bot.SendMessage(
          chat.ChatId,
          CreateMessage(address, balance, balanceDelta, addressLink, correspondentLink, links),
          ParseMode.MarkdownV2,
          messageThreadId: chat.MessageThreadId,
          linkPreviewOptions: true,
          cancellationToken: cancellationToken);
      }));
  }

  private string CreateMessage(string address, decimal balance, decimal balanceDelta, string addressLink, string? correspondentLink, string[] links) {
    var direction = balanceDelta > 0 ? "\u2795" : "\u2796";
    var message = _alertMessages[Random.Shared.Next(0, _alertMessages.Length - 1)].ToEscapedMarkdownV2();
    return $"\ud83d\udd75\ufe0f {message}\n" +
           $"\ud83c\udfe0 {addressLink}" + correspondentLink + "\n" +
           $"{direction} {Math.Abs(balanceDelta).ToEvers()} {GetWhileScale(balanceDelta)}\n" +
           $"\ud83d\udcb0 {balance.ToEvers()} {GetWhileScale(balance)}\n" +
           string.Join(" \\- ", links);
  }

  private static string GetWhileScale(decimal amount) {
    return Math.Round(Math.Abs(amount), 2) switch {
      >= 500_000_000M => GetFishString(6, WhaleSymbol),
      >= 100_000_000M => GetFishString(5, WhaleSymbol),
      >= 50_000_000M => GetFishString(4, WhaleSymbol),
      >= 10_000_000M => GetFishString(3, WhaleSymbol),
      >= 5_000_000M => GetFishString(2, WhaleSymbol),
      >= 1_000_000M => GetFishString(1, WhaleSymbol),
      >= 500_000M => GetFishString(6, DolphinSymbol),
      >= 100_000M => GetFishString(5, DolphinSymbol),
      >= 50_000M => GetFishString(4, DolphinSymbol),
      >= 10_000M => GetFishString(3, DolphinSymbol),
      >= 1_000M => GetFishString(2, DolphinSymbol),
      >= 0M => GetFishString(1, DolphinSymbol),
      _ => string.Empty
    };
  }

  private static string GetFishString(int count, string symbol) {
    return string.Join(string.Empty, Enumerable.Repeat(0, count).Select(_ => symbol).ToArray());
  }
}
