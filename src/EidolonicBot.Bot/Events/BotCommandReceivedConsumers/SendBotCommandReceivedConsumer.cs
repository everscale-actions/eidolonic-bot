namespace EidolonicBot.Events.BotCommandReceivedConsumers;

public class SendBotCommandReceivedConsumer : BotCommandReceivedConsumerBase {
    private const string SendMessage = "{0} sent to {1} {2:F}{3}";
    private readonly ILogger<SendBotCommandReceivedConsumer> _logger;

    private readonly IEverWallet _wallet;

    public SendBotCommandReceivedConsumer(ITelegramBotClient bot, IEverWallet wallet, IMemoryCache memoryCache,
        ILogger<SendBotCommandReceivedConsumer> logger) : base(
        Command.Send, bot, memoryCache) {
        _wallet = wallet;
        _logger = logger;
    }

    private static string FormatSendMessage(User fromUser, User toUser, decimal coins) {
        return string.Format(SendMessage,
            fromUser.ToMentionString(),
            toUser.ToMentionString(),
            coins,
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
                sendCoins = 0.1m;
                allBalance = true;
                break;
            case [{ } coinsStr]
                when decimal.TryParse(coinsStr.Replace(',', '.'), out sendCoins):
                allBalance = false;
                break;
            default:
                return CommandHelpers.HelpByCommand[Command.Send];
        }

        if (sendCoins < 0.1m) {
            return $"You should send at least {0.1:F}{Constants.Currency}";
        }

        try {
            var (_, coins) = await _wallet.SendCoins(toUser.Id, sendCoins, allBalance, cancellationToken);
            return FormatSendMessage(fromUser, toUser, coins);
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
