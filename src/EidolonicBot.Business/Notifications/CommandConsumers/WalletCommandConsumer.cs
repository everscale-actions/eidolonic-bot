using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace EidolonicBot.Notifications.CommandConsumers;

public class WalletCommandConsumer : CommandHandlerBase {
    private readonly ITelegramBotClient _botClient;
    private readonly IEverWallet _wallet;

    public WalletCommandConsumer(ITelegramBotClient botClient, IEverWallet wallet) {
        _botClient = botClient;
        _wallet = wallet;
    }

    private static string FormatInfoMessage(WalletInfo info) {
        return new StringBuilder()
            .AppendLine($"`{info.Address}`")
            .AppendLine($"Balance {info.Balance ?? 0}{Constants.Currency}")
            .ToString();
    }

    protected override Task<bool> Check(Command command, string[]? args, Message message, CancellationToken cancellationToken) {
        return Task.FromResult(command == Command.Wallet);
    }

    protected override async Task Consume(Command command, string[]? args, Message message, CancellationToken cancellationToken) {
        var info = await _wallet.GetInfo(cancellationToken);

        var messageText = FormatInfoMessage(info);

        await _botClient.SendTextMessageAsync(
            message.Chat.Id,
            messageText,
            ParseMode.MarkdownV2,
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken);
    }
}