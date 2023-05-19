namespace EidolonicBot.Events.BotCommandReceivedConsumers;

public class WalletBotCommandReceivedConsumer : BotCommandReceivedConsumerBase {
    private readonly IEverWallet _wallet;

    public WalletBotCommandReceivedConsumer(ITelegramBotClient botClient, IEverWallet wallet, IMemoryCache memoryCache) : base(Command.Wallet,
        botClient,
        memoryCache) {
        _wallet = wallet;
    }

    private static string FormatInfoMessage(WalletInfo info) {
        var tokens = info.TokenBalances is not null
            ? string.Join('\n', info.TokenBalances
                .Select(t => $"   {t.Balance}{t.Symbol}"))
            : null;

        return $"`{info.Address}`\n" +
               $"Balance {info.Balance ?? 0:F}{Constants.Currency}\n" +
               tokens;
    }


    protected override async Task<string?> ConsumeAndGetReply(string[] args, Message message, long chatId,
        int messageThreadId, bool isAdmin,
        CancellationToken cancellationToken) {
        var info = await _wallet.GetInfo(cancellationToken);

        return FormatInfoMessage(info);
    }
}
