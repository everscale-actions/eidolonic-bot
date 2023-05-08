using EidolonicBot.Exceptions;
using EidolonicBot.Notifications.CommandConsumers.Base;
using Microsoft.Extensions.Caching.Memory;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace EidolonicBot.Notifications.CommandConsumers;

public class SendCommandConsumer : CommandConsumerBase {
    private const string SendMessage = "{0} send to {1} {2}{3}";
    private const decimal DefaultTipCoins = 1m;

    private readonly ITelegramBotClient _botClient;
    private readonly IEverWallet _wallet;

    public SendCommandConsumer(ITelegramBotClient botClient, IEverWallet wallet, IMemoryCache memoryCache) : base(Command.Send, botClient,
        memoryCache) {
        _botClient = botClient;
        _wallet = wallet;
    }

    private static string FormatSendMessage(User fromUser, User toUser, decimal coins) {
        return string.Format(SendMessage,
            fromUser.ToMentionString(),
            toUser.ToMentionString(),
            coins,
            Constants.Currency);
    }

    protected override async Task<string?> Consume(string[] args, Message message, long chatId, bool isAdmin,
        CancellationToken cancellationToken) {
        if (message is not ({ ReplyToMessage.From: { } toUser } and { From: { } fromUser })) {
            return null;
        }

        var tipCoins = args.Length >= 1
                       && decimal.TryParse(args[0], out var coins)
            ? coins
            : DefaultTipCoins;

        try {
            await _wallet.SendCoins(toUser.Id, tipCoins, cancellationToken);
        } catch (AccountInsufficientBalanceException ex) {
            return @$"You balance\({ex.Balance}{Constants.Currency}\) is too low to send {tipCoins}{Constants.Currency}";
        }

        return FormatSendMessage(fromUser, toUser, tipCoins);
    }
}