namespace EidolonicBot.Events.BotCommandReceivedConsumers.Base;

public abstract class BotCommandReceivedConsumerBase(
  Command command,
  ITelegramBotClient bot,
  IMemoryCache memoryCache
) : IConsumer<BotCommandReceived>, IMediatorConsumer {
  public async Task Consume(ConsumeContext<BotCommandReceived> context) {
    if (context.Message.Command != command) {
      return;
    }

    var message = context.Message.Message;
    var cancellationToken = context.CancellationToken;

    if (message is not { Chat.Id: var chatId, MessageThreadId: var messageThreadId, From.Id: var fromId }) {
      return;
    }

    var isAdmin = chatId == fromId || await IsChatAdmin(chatId, fromId, cancellationToken);

    // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
    var replyText = await ConsumeAndGetReply(
      context.Message.Arguments, context.Message.Message, chatId,
      messageThreadId ?? 0, isAdmin,
      cancellationToken);

    if (replyText is null) {
      return;
    }

    await bot.SendTextMessageAsync(
      message.Chat.Id,
      replyText,
      parseMode: ParseMode.MarkdownV2,
      linkPreviewOptions: true,
      disableNotification: true,
      replyParameters: message.MessageId,
      cancellationToken: cancellationToken
    );
  }

  private async Task<bool> IsChatAdmin(long chatId, long userId, CancellationToken cancellationToken) {
    var cache = await memoryCache.GetOrCreateAsync(
      $"AdminIdsByChatId_{chatId}", async entry => {
        entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(1));
        entry.SetSize(1);
        var admins = await bot.GetChatAdministratorsAsync(chatId, cancellationToken);
        return admins.Select(a => a.User.Id).ToArray();
      });

    return cache?.Contains(userId) ?? false;
  }

  protected abstract Task<string?> ConsumeAndGetReply(string[] args, Message message, long chatId,
    int messageThreadId,
    bool isAdmin,
    CancellationToken cancellationToken);
}
