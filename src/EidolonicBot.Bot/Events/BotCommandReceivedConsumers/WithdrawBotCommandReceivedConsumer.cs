namespace EidolonicBot.Events.BotCommandReceivedConsumers;

public class WithdrawBotCommandReceivedConsumer : BotCommandReceivedConsumerBase {
    private const decimal MinimalCoins = 0.1m;

    private const string WithdrawalMessage = "{0} withdrew to {1} {2}{3}";
    private readonly ILinkFormatter _linkFormatter;
    private readonly ILogger<WithdrawBotCommandReceivedConsumer> _logger;
    private readonly IEverWalletFactory _walletFactory;

    public WithdrawBotCommandReceivedConsumer(ITelegramBotClient bot, IMemoryCache memoryCache,
        ILogger<WithdrawBotCommandReceivedConsumer> logger, ILinkFormatter linkFormatter, IEverWalletFactory walletFactory) : base(
        Command.Withdraw, bot, memoryCache) {
        _logger = logger;
        _linkFormatter = linkFormatter;
        _walletFactory = walletFactory;
    }

    private string FormatSendMessage(User fromUser, string dest, decimal coins) {
        return string.Format(WithdrawalMessage,
            fromUser.ToMentionMarkdownV2(),
            $"{_linkFormatter.GetAddressLink(dest)}",
            coins.ToString("F").ToEscapedMarkdownV2(),
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
            return $"You should send at least {MinimalCoins}{Constants.Currency}";
        }

        if (args is not [_, { } dest, ..] || !Regex.TvmAddressRegex().IsMatch(dest)) {
            return "Provide valid destination address";
        }

        using var _ = _logger.BeginScope(new Dictionary<string, object> {
            { "FromUser", fromUser },
            { "Dest", dest },
            { "SendCoins", sendCoins },
            { "AllBalance", allBalance }
        });

        var memo = args is [_, _, { } memoStr] && !string.IsNullOrWhiteSpace(memoStr) ? memoStr : null;

        var wallet = await _walletFactory.CreateWallet(fromUser.Id, cancellationToken);

        try {
            var (_, coins) = await wallet.SendCoins(dest, sendCoins, allBalance, memo, cancellationToken);
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
