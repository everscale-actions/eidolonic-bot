namespace EidolonicBot.Events.BotCommandReceivedConsumers;

public class SendBotCommandReceivedConsumer(
  ITelegramBotClient bot,
  IEverWallet wallet,
  IMemoryCache memoryCache,
  ILogger<SendBotCommandReceivedConsumer> logger
)
  : BotCommandReceivedConsumerBase(Command.Send, bot, memoryCache) {
  private const decimal MinimalCoins = 0.1m;

  private const string SendMessage = "{0} sent to {1} {2}";

  private static string FormatSendMessage(User fromUser, User toUser, decimal coins) {
    return string.Format(
      SendMessage,
      fromUser.ToMentionMarkdownV2(),
      toUser.ToMentionMarkdownV2(),
      coins.ToEvers()
    );
  }

  protected override async Task<string?> ConsumeAndGetReply(string[] args, Message message, long chatId,
    int messageThreadId, bool isAdmin,
    CancellationToken cancellationToken) {
    if (message is not { From: { } fromUser }) {
      logger.LogError("There is no From User in telegram message {@Message}", message);
      return "Ops, something went wrong..";
    }

    if (message is not { ReplyToMessage: { From: { } toUser, Type: var replyMessageType } }) {
      return "Reply to some message of user to send tokens";
    }

    if (replyMessageType is MessageType.ForumTopicCreated) {
      return "Reply to some message of user to send tokens";
    }

    bool allBalance;
    decimal sendCoins;
    switch (args) {
      case ["all"]:
        sendCoins = MinimalCoins;
        allBalance = true;
        break;
      case [{ } coinsStr]
        when decimal.TryParse(coinsStr.Replace(',', '.'), out sendCoins):
        allBalance = false;
        break;
      default:
        return CommandHelpers.HelpByCommand[Command.Send];
    }

    if (sendCoins < MinimalCoins) {
      return $"You should send at least {MinimalCoins.ToEvers()}";
    }

    using var _ = logger.BeginScope(
      new Dictionary<string, object> {
        {
          "@SendData", new {
            FromUser = fromUser,
            ToUser = toUser,
            SendCoins = sendCoins,
            AllBalance = allBalance
          }
        }
      });

    try {
      var (_, coins) = await wallet.SendCoins(toUser.Id, sendCoins, allBalance, cancellationToken);
      return FormatSendMessage(fromUser, toUser, coins);
    }
    catch (AccountInsufficientBalanceException ex) {
      return $@"You balance\({ex.Balance.ToEvers()}\) is too low";
    }
    catch (Exception e) {
      logger.LogError(e, "Something went wrong");
      return "Something went wrong";
    }
  }
}
