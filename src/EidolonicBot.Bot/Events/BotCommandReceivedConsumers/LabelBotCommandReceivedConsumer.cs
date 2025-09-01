namespace EidolonicBot.Events.BotCommandReceivedConsumers;

public class LabelBotCommandReceivedConsumer(
  ITelegramBotClient botClient,
  IMemoryCache memoryCache,
  AppDbContext db,
  ILinkFormatter linkFormatter
)
  : BotCommandReceivedConsumerBase(
    Command.Label, botClient,
    memoryCache) {
  private readonly string[] _adminActions = ["add", "edit", "remove"];

  protected override async Task<string?> ConsumeAndGetReply(string[] args, Message message, long chatId,
    int messageThreadId, bool isAdmin,
    CancellationToken cancellationToken) {
    return args switch {
      [{ } action, ..]
        when _adminActions.Contains(action) && !isAdmin => "Only chat admin can control labels"
          .ToEscapedMarkdownV2(),

      ["assign", { } address, { } label] when RegexList.TvmAddressRegex().IsMatch(address) =>
        await Assign(address, chatId, messageThreadId, label, cancellationToken),

      ["unassign", { } address] when RegexList.TvmAddressRegex().IsMatch(address) =>
        await Unassign(address, chatId, messageThreadId, cancellationToken),

      ["list", ..] => await GetLabelList(
        chatId, messageThreadId,
        args is [_, "full", ..] or [_, "true", ..],
        cancellationToken),

      _ => CommandHelpers.HelpByCommand[Command.Label]
    };
  }

  private async Task<string?> GetLabelList(long chatId, int messageThreadId, bool full,
    CancellationToken cancellationToken) {
    var labelsByChat = await db.LabelByChat
      .Where(s => s.ChatId == chatId && s.MessageThreadId == messageThreadId)
      .ToArrayAsync(cancellationToken: cancellationToken);

    if (labelsByChat.Length == 0) {
      return "Assign your first label with\n" +
             " `/label assign `address label";
    }

    var labelsString = labelsByChat.Select(l => full
      ? $@"`{l.Address}`` \| ``{l.Label.ToEscapedMarkdownV2()}`"
      : $@"{linkFormatter.GetAddressLink(l.Address)} \| {l.Label.ToEscapedMarkdownV2()}"
    );

    return "Address \\| Label\n" +
           $"{string.Join('\n', labelsString)}";
  }

  private async Task<string> Assign(string address, long chatId, int messageThreadId, string label, CancellationToken cancellationToken) {
    var labelByChat = await db.LabelByChat.FindAsync([chatId, messageThreadId, address], cancellationToken);

    if (labelByChat is null) {
      await db.LabelByChat.AddAsync(
        new LabelByChat {
          ChatId = chatId,
          MessageThreadId = messageThreadId,
          Address = address,
          Label = label
        },
        cancellationToken);
    }
    else {
      labelByChat.Label = label;
    }

    var savedEntries = await db.SaveChangesAsync(cancellationToken);

    return savedEntries > 0
      ? $"{label} updated for {linkFormatter.GetAddressLink(address)}"
      : $"{label} was already assigned to {linkFormatter.GetAddressLink(address)} earlier";
  }

  private async Task<string> Unassign(string address, long chatId, int messageThreadId, CancellationToken cancellationToken) {
    var labelByChat = await db.LabelByChat.FindAsync([chatId, messageThreadId, address], cancellationToken);
    if (labelByChat is null) {
      return "Label not found";
    }
    db.LabelByChat.Remove(labelByChat);
    await db.SaveChangesAsync(cancellationToken);
    return $"Label was unassigned for {linkFormatter.GetAddressLink(address)}";
  }
}
