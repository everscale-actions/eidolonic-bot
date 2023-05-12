namespace EidolonicBot.Events.BotCommandReceivedConsumers;

public class WalletBotCommandReceivedConsumers : BotCommandReceivedConsumerBase {
    private readonly IEverWallet _wallet;

    public WalletBotCommandReceivedConsumers(ITelegramBotClient botClient, IEverWallet wallet, IMemoryCache memoryCache) : base(Command.Wallet, botClient,
        memoryCache) {
        _wallet = wallet;
    }

    private static string FormatInfoMessage(WalletInfo info) {
        return new StringBuilder()
            .AppendLine($"`{info.Address}`")
            .AppendLine($"Balance {info.Balance ?? 0:F}{Constants.Currency}")
            .ToString();
    }


    protected override async Task<string?> Consume(string[] args, Message message, long chatId, bool isAdmin,
        CancellationToken cancellationToken) {
        var info = await _wallet.GetInfo(cancellationToken);

        return FormatInfoMessage(info);
    }
}
