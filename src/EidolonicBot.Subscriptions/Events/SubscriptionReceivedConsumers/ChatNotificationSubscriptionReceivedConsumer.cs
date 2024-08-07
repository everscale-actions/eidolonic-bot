using MassTransit;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace EidolonicBot.Events.SubscriptionReceivedConsumers;

public class ChatNotificationSubscriptionReceivedConsumer(
  AppDbContext db,
  ITelegramBotClient bot,
  ILinkFormatter linkFormatter
) : IConsumer<SubscriptionReceived>, IMediatorConsumer {
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
    var fromString = from is not null ? $"from       {linkFormatter.GetAddressLink(from)}\n" : null;
    var toString = to.Length > 0 ? $"to            {string.Join(',', to.Select(t => linkFormatter.GetAddressLink(t)))}\n" : null;

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


    var direction = balanceDelta > 0 ? "📥" : "\ud83d\udce4";
    return $"\ud83d\udd7a Subscription alert {direction}\n" +
           $"address {addressLink}\n" +
           fromString +
           toString +
           $"balance {balance.ToEvers()}\n" +
           $"delta      {balanceDelta.ToEvers()}\n" +
           string.Join(" \\| ", links);
  }
}
