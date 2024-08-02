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

    var fromString = from is not null ? $"from: {linkFormatter.GetAddressLink(from)}\n" : null;
    var toString = to.Length > 0
      ? $"to: {string.Join(',', to.Select(t => linkFormatter.GetAddressLink(t)))}\n"
      : null;

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

  private string CreateMessage(string address, decimal balance, decimal balanceDelta, string? label,
    string? fromString, string? toString, string[] links) {
    var labelStr = label is not null ? $"label: {label}\n".ToEscapedMarkdownV2() : null;
    return "\u2755Subscription alert \u2755\n" +
           labelStr +
           $"address: {linkFormatter.GetAddressLink(address)}\n" +
           fromString +
           toString +
           $"balance: {balance}{Constants.Currency}\n".ToEscapedMarkdownV2() +
           $"delta: {balanceDelta}{Constants.Currency}\n".ToEscapedMarkdownV2() + string.Join(" \\| ", links);
  }
}
