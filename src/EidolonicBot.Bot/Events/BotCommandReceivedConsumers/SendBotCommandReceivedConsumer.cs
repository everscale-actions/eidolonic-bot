namespace EidolonicBot.Events.BotCommandReceivedConsumers;

public class SendBotCommandReceivedConsumer : BotCommandReceivedConsumerBase {
    private const decimal MinimalCoins = 0.1m;

    private const string SendMessage = "{0} sent to {1} {2}{3}";
    private readonly IEverWalletFactory _everWalletFactory;
    private readonly ILogger<SendBotCommandReceivedConsumer> _logger;

    public SendBotCommandReceivedConsumer(ITelegramBotClient bot, IMemoryCache memoryCache,
        ILogger<SendBotCommandReceivedConsumer> logger, IEverWalletFactory everWalletFactory) : base(
        Command.Send, bot, memoryCache) {
        _logger = logger;
        _everWalletFactory = everWalletFactory;
    }

    private static string FormatSendMessage(User fromUser, User toUser, decimal coins) {
        return string.Format(SendMessage,
            fromUser.ToMentionMarkdownV2(),
            toUser.ToMentionMarkdownV2(),
            coins.ToString("F").ToEscapedMarkdownV2(),
            Constants.Currency);
    }

    protected override async Task<string?> ConsumeAndGetReply(string[] args, Message message, long chatId,
        int messageThreadId, bool isAdmin,
        CancellationToken cancellationToken) {
        if (message is not { From: { } fromUser }) {
            _logger.LogError("There is no From User in telegram message {@Message}", message);
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
            return $"You should send at least {MinimalCoins}{Constants.Currency}";
        }

        using var _ = _logger.BeginScope(new Dictionary<string, object> {
            { "FromUser", fromUser },
            { "ToUser", toUser },
            { "SendCoins", sendCoins },
            { "AllBalance", allBalance }
        });

        var fromWallet = await _everWalletFactory.CreateWallet(fromUser.Id, cancellationToken);
        var toWallet = await _everWalletFactory.CreateWallet(toUser.Id, cancellationToken);

        try {
            var (_, coins) = await fromWallet.SendCoins(toWallet.Address, sendCoins, allBalance, cancellationToken: cancellationToken);
            return FormatSendMessage(fromUser, toUser, coins);
        } catch (AccountInsufficientBalanceException ex) {
            return @$"You balance({ex.Balance:F}{Constants.Currency}) is too low";
        } catch (Exception e) {
            _logger.LogError(e, "Something went wrong");
            return "Something went wrong";
        }
    }
}
