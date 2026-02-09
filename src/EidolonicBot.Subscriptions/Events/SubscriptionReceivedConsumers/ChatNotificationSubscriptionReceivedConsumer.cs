using MassTransit;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace EidolonicBot.Events.SubscriptionReceivedConsumers;

public class ChatNotificationSubscriptionReceivedConsumer(
  AppDbContext db,
  ITelegramBotClient bot,
  ILinkFormatter linkFormatter
) : IConsumer<SubscriptionReceived>, IMediatorConsumer {
  private static class EmojiMarineAnimals {
    // Константы с понятными именами
    public const string SpoutingWhale = "\uD83D\uDC33"; // 🐳 U+1F433
    public const string Whale = "\uD83D\uDC0B"; // 🐋 U+1F40B
    public const string Shark = "\uD83E\uDD88"; // 🦈 U+1F988
    public const string Dolphin = "\uD83D\uDC2C"; // 🐬 U+1F42C
    public const string Seal = "\uD83E\uDDAD"; // 🦭 U+1F9AD
    public const string Fish = "\uD83D\uDC1F"; // 🐟 U+1F41F
    public const string TropicalFish = "\uD83D\uDC20"; // 🐠 U+1F420
    public const string Blowfish = "\uD83D\uDC21"; // 🐡 U+1F421
    public const string Octopus = "\uD83D\uDC19"; // 🐙 U+1F419
    public const string SpiralShell = "\uD83D\uDC1A"; // 🐚 U+1F41Aб
    public const string Squid = "\ud83e\udd91"; // 🦑 U+1F991
    public const string Oyster = "\ud83e\uddaa"; // 🦪 U+1F9AA
  }

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
    "Bro, you won’t believe this!",
    "Whoa, check this out, man!",
    "Hey, get a load of this!",
    "Yo, scope this out, dude!",
    "Bro, you have to see this!",
    "Man, cast your eyes on this!",
    "Dude, wait till you see this!",
    "Hey buddy, look what we have here!",
    "Yo, witness this!",
    "Brother, check this madness out!",
    "Mate, take a peek at this!",
    "Homie, look at this right here!",
    "Amigo, echa un vistazo a esto!",
    "Chief, you gotta check this!",
    "My guy, look at this!",
    "Listen, look at this thing!",
    "No way, look!",
    "Okay, focus – look at this!",
    "Hold up, look at this!",
    "Check it, check it, check it!",
    "Ay, look at this joint!"
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


    var list = new List<(long ChatId, int MessageThreadId, decimal MinDelta, string? Label, IReadOnlyCollection<(string Address, string? Label)> ToLabels, string? FromLabel)>();

    foreach (var chatAndThreadId in chatAndThreadIds) {
      var label = (await db.LabelByChat.FindAsync([chatAndThreadId.ChatId, chatAndThreadId.MessageThreadId, address], cancellationToken))?.Label;
      var toLabels = new List<(string Address, string? Label)>();
      foreach (var t in to) {
        var l = await db.LabelByChat.FindAsync([chatAndThreadId.ChatId, chatAndThreadId.MessageThreadId, t], cancellationToken);
        toLabels.Add((t, l?.Label));
      }
      var fromLabel = from is not null ? (await db.LabelByChat.FindAsync([chatAndThreadId.ChatId, chatAndThreadId.MessageThreadId, from], cancellationToken))?.Label : null;
      list.Add((chatAndThreadId.ChatId, chatAndThreadId.MessageThreadId, chatAndThreadId.MinDelta, Label: label, ToLabels: toLabels, FromLabel: fromLabel));
    }

    await Task.WhenAll(
      list.Select(async c => {
        var addressLink = c.Label is null
          ? linkFormatter.GetAddressLink(address)
          : linkFormatter.GetAddressLink(address, c.Label);
        var fromLink = from is not null ? $" \u2b05\ufe0f {(c.FromLabel is null ? linkFormatter.GetAddressLink(from) : linkFormatter.GetAddressLink(from, c.FromLabel))}" : null;
        var toLink = to is not null && to.Length > 0 ? $" \u27a1\ufe0f {string.Join(',', c.ToLabels.Select(t => t.Label is null ? linkFormatter.GetAddressLink(t.Address) : linkFormatter.GetAddressLink(t.Address, t.Label)))}" : null;
        var correspondentLink = fromLink + toLink;

        await bot.SendMessage(
          c.ChatId,
          CreateMessage(balance, balanceDelta, addressLink, correspondentLink, links),
          ParseMode.MarkdownV2,
          messageThreadId: c.MessageThreadId,
          linkPreviewOptions: true,
          cancellationToken: cancellationToken);
      }));
  }

  private string CreateMessage(decimal balance, decimal balanceDelta, string addressLink, string? correspondentLink, string[] links) {
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
      >= 500_000_000M => EmojiMarineAnimals.SpoutingWhale,
      >= 100_000_000M => EmojiMarineAnimals.Whale,
      >= 50_000_000M => EmojiMarineAnimals.Shark,
      >= 10_000_000M => EmojiMarineAnimals.Dolphin,
      >= 5_000_000M => EmojiMarineAnimals.Seal,
      >= 1_000_000M => EmojiMarineAnimals.Octopus,
      >= 500_000M => EmojiMarineAnimals.SpiralShell,
      >= 100_000M => EmojiMarineAnimals.Blowfish,
      >= 50_000M => EmojiMarineAnimals.TropicalFish,
      >= 10_000M => EmojiMarineAnimals.Fish,
      >= 1_000M => EmojiMarineAnimals.Squid,
      >= 0M => EmojiMarineAnimals.Oyster,
      _ => string.Empty
    };
  }
}
