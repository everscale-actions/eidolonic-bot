using MassTransit;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace EidolonicBot.Events.SubscriptionReceivedConsumers;

public class ChatNotificationSubscriptionReceivedConsumer(
  AppDbContext db,
  ITelegramBotClient bot,
  ILinkFormatter linkFormatter
) : IConsumer<SubscriptionReceived>, IMediatorConsumer {
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
      .SelectMany(
        s => s.SubscriptionByChat
          .Where(sbc => sbc.MinDelta <= Math.Abs(balanceDelta))
          .Select(sbc => new { sbc.ChatId, sbc.MessageThreadId, sbc.MinDelta, sbc.Label }))
      .ToArrayAsync(cancellationToken);

    var links = linkFormatter.GetTransactionLinks(transactionId);
    var fromString = from is not null ? $"\ud83d\udce4 {linkFormatter.GetAddressLink(from)}\n" : null;
    var toString = to.Length > 0 ? $"\ud83d\udce5 {string.Join(',', to.Select(t => linkFormatter.GetAddressLink(t)))}\n" : null;

    await Task.WhenAll(
      chatAndThreadIds.Select(
        chat =>
          bot.SendTextMessageAsync(
            chat.ChatId,
            CreateMessage(address, balance, balanceDelta, chat.Label, fromString, toString, links),
            chat.MessageThreadId,
            ParseMode.MarkdownV2,
            linkPreviewOptions: true,
            cancellationToken: cancellationToken)));
  }

  private string CreateMessage(string address, decimal balance, decimal balanceDelta, string? label, string? fromString, string? toString, string[] links) {
    var addressLink = label is null
      ? linkFormatter.GetAddressLink(address)
      : linkFormatter.GetAddressLink(address, label);

    var direction = balanceDelta > 0 ? "\u2795" : "\u2796";
    var message = _alertMessages[Random.Shared.Next(0, _alertMessages.Length - 1)].ToEscapedMarkdownV2();
    return $"\ud83d\udd75\ufe0f {message}\n" +
           $"\ud83c\udfe0 {addressLink}\n" +
           fromString +
           toString +
           $"\ud83d\udcb0 {balance.ToEvers()}\n" +
           $"{direction} {Math.Abs(balanceDelta).ToEvers()}\n" +
           string.Join(" \\| ", links);
  }
}
