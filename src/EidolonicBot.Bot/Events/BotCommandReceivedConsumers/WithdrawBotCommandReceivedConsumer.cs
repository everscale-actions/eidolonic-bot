namespace EidolonicBot.Events.BotCommandReceivedConsumers;

public class WithdrawBotCommandReceivedConsumer(
  ITelegramBotClient bot,
  IEverWallet wallet,
  IMemoryCache memoryCache,
  ILogger<WithdrawBotCommandReceivedConsumer> logger,
  ILinkFormatter linkFormatter
)
  : BotCommandReceivedConsumerBase(Command.Withdraw, bot, memoryCache) {
  private const decimal MinimalCoins = 0.1m;

  private const string WithdrawalMessage = "{0} withdrew to {1} {2}";

  private string FormatSendMessage(User fromUser, string dest, decimal coins) {
    return string.Format(
      WithdrawalMessage,
      fromUser.ToMentionMarkdownV2(),
      $"{linkFormatter.GetAddressLink(dest)}",
      coins.ToEvers()
    );
  }

  protected override async Task<string?> ConsumeAndGetReply(string[] args, Message message, long chatId,
    int messageThreadId, bool isAdmin,
    CancellationToken cancellationToken) {
    if (message is not { From: { } fromUser }) {
      return null;
    }

    bool allBalance;
    decimal sendCoins;
    switch (args) {
      case ["all", ..]:
        sendCoins = MinimalCoins;
        allBalance = true;
        break;
      case [{ } coinsStr, ..]
        when decimal.TryParse(coinsStr.Replace(',', '.'), out sendCoins):
        allBalance = false;
        break;
      default:
        return CommandHelpers.HelpByCommand[Command.Withdraw];
    }

    if (sendCoins < MinimalCoins) {
      return $"You should send at least {MinimalCoins.ToEvers()}";
    }

    if (args is not [_, { } dest, ..] || !Regex.TvmAddressRegex().IsMatch(dest)) {
      return "Provide valid destination address";
    }

    using var _ = logger.BeginScope(
      new Dictionary<string, object> {
        {
          "@WithdrawData", new {
            FromUser = fromUser,
            Dest = dest,
            SendCoins = sendCoins,
            AllBalance = allBalance
          }
        }
      });

    var memo = args is [_, _, { } memoStr] && !string.IsNullOrWhiteSpace(memoStr) ? memoStr : null;
    try {
      var (_, coins) = await wallet.SendCoins(dest, sendCoins, allBalance, memo, cancellationToken);
      return FormatSendMessage(fromUser, dest, coins);
    }
    catch (AccountInsufficientBalanceException ex) {
      return $"You balance({ex.Balance.ToEvers()}) is too low";
    }
    catch (Exception e) {
      logger.LogError(e, "Something went wrong");
      return "Something went wrong";
    } finally {
      await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
    }
  }
}
