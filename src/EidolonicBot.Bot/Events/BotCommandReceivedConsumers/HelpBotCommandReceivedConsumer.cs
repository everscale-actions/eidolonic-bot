namespace EidolonicBot.Events.BotCommandReceivedConsumers;

public class HelpBotCommandReceivedConsumer(
  ITelegramBotClient botClient,
  IMemoryCache memoryCache
) : BotCommandReceivedConsumerBase(Command.Help, botClient, memoryCache) {
  protected override Task<string?> ConsumeAndGetReply(string[] args, Message message, long chatId, int messageThreadId,
    bool isAdmin,
    CancellationToken cancellationToken) {
    var text = "Usage:\n" +
               string.Join(
                 '\n', CommandHelpers.CommandAttributeByCommand
                   .Select(c => c.Value)
                   .Select(a => $"{a.Text} - {a.Description}"));

    return Task.FromResult<string?>(text.ToEscapedMarkdownV2());
  }
}
