using EidolonicBot.Exceptions;
using EidolonicBot.Notifications.CommandConsumers.Base;
using Microsoft.Extensions.Caching.Memory;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace EidolonicBot.Notifications.CommandConsumers;

public class SendCommandConsumer : CommandConsumerBase {
    private const string SendMessage = "{0} sent to {1} {2:F}{3}";

    private readonly IEverWallet _wallet;

    public SendCommandConsumer(ITelegramBotClient botClient, IEverWallet wallet, IMemoryCache memoryCache) : base(Command.Send, botClient,
        memoryCache) {
        _wallet = wallet;
    }

    private static string FormatSendMessage(User fromUser, User toUser, decimal coins) {
        return string.Format(SendMessage,
            fromUser.ToMentionString(),
            toUser.ToMentionString(),
            coins,
            Constants.Currency);
    }

    private static string FormatSendMessage(User fromUser, string dest, decimal coins) {
        return string.Format(SendMessage,
            fromUser.ToMentionString(),
            $"`{dest}`",
            coins,
            Constants.Currency);
    }

    protected override async Task<string?> Consume(string[] args, Message message, long chatId, bool isAdmin,
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
                return CommandHelpers.CommandAttributeByCommand[Command.Send]?.Help;
        }

        if (sendCoins < 0.1m) {
            return $"You should send at least {0.1:F}{Constants.Currency}";
        }

        try {
            if (args is [.., { } dest] && Regex.TvmAddressRegex().IsMatch(dest)) {
                var (transactionId, coins) = await _wallet.SendCoins(dest, sendCoins, allBalance, cancellationToken);
                return FormatSendMessage(fromUser, dest, coins);
            }

            if (message is { ReplyToMessage.From: { } toUser }) {
                var (transactionId, coins) = await _wallet.SendCoins(toUser.Id, sendCoins, allBalance, cancellationToken);
                return FormatSendMessage(fromUser, toUser, coins);
            }

            return CommandHelpers.CommandAttributeByCommand[Command.Send]?.Help;
        } catch (AccountInsufficientBalanceException ex) {
            return @$"You balance({ex.Balance:F}{Constants.Currency}) is too low";
        }
    }
}
