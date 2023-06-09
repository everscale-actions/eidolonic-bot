namespace EidolonicBot.Events.BotCommandReceivedConsumers;

public class WalletBotCommandReceivedConsumer : BotCommandReceivedConsumerBase {
    private readonly IEverWallet _wallet;

    public WalletBotCommandReceivedConsumer(ITelegramBotClient botClient, IEverWallet wallet, IMemoryCache memoryCache) : base(Command.Wallet,
        botClient,
        memoryCache) {
        _wallet = wallet;
    }

    private static string FormatInfoMessage(WalletInfo info) {
        return
            $"`{info.Address}`\n" +
            $"Balance {info.Balance ?? 0:F}{Constants.Currency}".ToEscapedMarkdownV2();
    }


    protected override async Task<string?> ConsumeAndGetReply(string[] args, Message message, long chatId,
        int messageThreadId, bool isAdmin,
        CancellationToken cancellationToken) {
        var info = await _wallet.GetInfo(cancellationToken);

        return FormatInfoMessage(info);
    }
}
