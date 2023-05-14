namespace EidolonicBot.Events.BotCommandReceivedConsumers;

public class WithdrawBotCommandReceivedConsumer : BotCommandReceivedConsumerBase {
    private const string WithdrawalMessage = "{0} withdrawal to {1} {2:F}{3}";
    private readonly ILogger<WithdrawBotCommandReceivedConsumer> _logger;

    private readonly IEverWallet _wallet;

    public WithdrawBotCommandReceivedConsumer(ITelegramBotClient bot, IEverWallet wallet, IMemoryCache memoryCache,
        ILogger<WithdrawBotCommandReceivedConsumer> logger) : base(
        Command.Withdraw, bot, memoryCache) {
        _wallet = wallet;
        _logger = logger;
    }

    private static string FormatSendMessage(User fromUser, string dest, decimal coins) {
        return string.Format(WithdrawalMessage,
            fromUser.ToMentionString(),
            $"`{dest}`",
            coins,
            Constants.Currency);
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
                sendCoins = 0.1m;
                allBalance = true;
                break;
            case [{ } coinsStr, ..]
                when decimal.TryParse(coinsStr.Replace(',', '.'), out sendCoins):
                allBalance = false;
                break;
            default:
                return CommandHelpers.HelpByCommand[Command.Withdraw];
        }

        if (sendCoins < 0.1m) {
            return $"You should send at least {0.1:F}{Constants.Currency}";
        }

        if (args is not [_, { } dest, ..] || !Regex.TvmAddressRegex().IsMatch(dest)) {
            return "Provide valid destination address";
        }

        var memo = args is [_, _, { } memoStr] && !string.IsNullOrWhiteSpace(memoStr) ? memoStr : null;

        try {
            var (_, coins) = await _wallet.SendCoins(dest, sendCoins, allBalance, memo, cancellationToken);
            return FormatSendMessage(fromUser, dest, coins);
        } catch (AccountInsufficientBalanceException ex) {
            return @$"You balance({ex.Balance:F}{Constants.Currency}) is too low";
        } catch (Exception e) {
            _logger.LogError(e, "Something went wrong");
            return "Something went wrong";
        } finally {
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        }
    }
}
