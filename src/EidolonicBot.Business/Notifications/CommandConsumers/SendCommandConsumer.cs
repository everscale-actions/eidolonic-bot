using EidolonicBot.Exceptions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace EidolonicBot.Notifications.CommandConsumers;

public class SendCommandConsumer : CommandHandlerBase {
    private const string SendMessage = "{0} send to {1} {2}{3}";
    private const decimal DefaultTipCoins = 1m;

    private readonly ITelegramBotClient _botClient;
    private readonly IEverWallet _wallet;

    public SendCommandConsumer(ITelegramBotClient botClient, IEverWallet wallet) {
        _botClient = botClient;
        _wallet = wallet;
    }

    protected override Task<bool> Check(Command command, string[]? args, Message message, CancellationToken cancellationToken) {
        return Task.FromResult(command == Command.Send);
    }

    protected override async Task Consume(Command command, string[]? args, Message message, CancellationToken cancellationToken) {
        if (message is not ({ ReplyToMessage.From: { } toUser } and { From: { } fromUser })) {
            return;
        }

        var tipCoins = args is not null
                       && args.Length >= 1
                       && decimal.TryParse(args[0], out var coins)
            ? coins
            : DefaultTipCoins;

        try {
            await _wallet.SendCoins(toUser.Id, tipCoins, cancellationToken);
        } catch (AccountInsufficientBalanceException ex) {
            try {
                await _botClient.SendTextMessageAsync(
                    fromUser.Id,
                    @$"You balance\({ex.Balance}{Constants.Currency}\) is too low to send {tipCoins}{Constants.Currency}",
                    ParseMode.MarkdownV2,
                    cancellationToken: cancellationToken);
                //todo: should be more concrete
            } catch (ApiRequestException) { }

            return;
        }

        var tipText = FormatSendMessage(fromUser, toUser, tipCoins);
        await _botClient.SendTextMessageAsync(
            message.Chat.Id,
            tipText,
            ParseMode.MarkdownV2,
            cancellationToken: cancellationToken);
    }

    private static string FormatSendMessage(User fromUser, User toUser, decimal coins) {
        return string.Format(SendMessage,
            fromUser.ToMentionString(),
            toUser.ToMentionString(),
            coins,
            Constants.Currency);
    }
}