using MassTransit;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace EidolonicBot.Events.SubscriptionReceivedConsumers;

public class ChatNotificationSubscriptionReceivedConsumer(
  AppDbContext db,
  ITelegramBotClient bot,
  ILinkFormatter linkFormatter
) : IConsumer<SubscriptionReceived>, IMediatorConsumer {
  private const byte MaxWhileScale = 6;
  private const int MinWhileScale = 3;

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

  private readonly IReadOnlyDictionary<decimal, string> whileScale = new Dictionary<decimal, string>() {
    { 0, "\ud83d\udc0b\ud83d\udc0b\ud83d\udc0b\ud83d\udc0b\ud83d\udc0b\ud83d\udc0b\ud83d\udc0b" },
    { 1_000, "\ud83d\udc33\ud83d\udc0b\ud83d\udc0b\ud83d\udc0b\ud83d\udc0b\ud83d\udc0b\ud83d\udc0b" },
    { 10_000, "\ud83d\udc33\ud83d\udc33\ud83d\udc0b\ud83d\udc0b\ud83d\udc0b\ud83d\udc0b\ud83d\udc0b" },
    { 100_000, "\ud83d\udc33\ud83d\udc33\ud83d\udc33\ud83d\udc0b\ud83d\udc0b\ud83d\udc0b\ud83d\udc0b" },
    { 1_000_000, "\ud83d\udc33\ud83d\udc33\ud83d\udc33\ud83d\udc33\ud83d\udc0b\ud83d\udc0b\ud83d\udc0b" },
    { 10_000_000, "\ud83d\udc33\ud83d\udc33\ud83d\udc33\ud83d\udc33\ud83d\udc33\ud83d\udc0b\ud83d\udc0b" },
    { 100_000_000, "\ud83d\udc33\ud83d\udc33\ud83d\udc33\ud83d\udc33\ud83d\udc33\ud83d\udc33\ud83d\udc33" }
  };

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
    var fromString = from is not null ? $" \u2b05\ufe0f {linkFormatter.GetAddressLink(from)}" : null;
    var toString = from is null && to.Length > 0 ? $" \u27a1\ufe0f {string.Join(',', to.Select(t => linkFormatter.GetAddressLink(t)))}" : null;

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
           $"\ud83c\udfe0 {addressLink}" + fromString + toString + "\n" +
           $"{direction} {Math.Abs(balanceDelta).ToEvers()} {GetWhileScale(balanceDelta)}\n" +
           $"\ud83d\udcb0 {balance.ToEvers()} {GetWhileScale(balance)}\n" +
           string.Join(" \\| ", links);
  }

  private static string GetWhileScale(decimal amount) {
    var deltaLength = Math.Abs(amount).ToString("#").Length;
    var whiles = deltaLength switch {
      >= MaxWhileScale + MinWhileScale => MaxWhileScale,
      >= MinWhileScale + 1 => deltaLength - MinWhileScale,
      >= 0 => 0,
      _ => throw new ArgumentOutOfRangeException()
    };
    return string.Join(
      string.Empty,
      Enumerable.Repeat(0, whiles).Select(_ => "\ud83d\udc33")
        .Concat(Enumerable.Repeat(0, MaxWhileScale - whiles).Select(_ => "\ud83d\udc0b"))
        .ToArray());
  }
}
