namespace EidolonicBot.Events.BotCommandReceivedConsumers;

public class WalletBotCommandReceivedConsumer : BotCommandReceivedConsumerBase {
    private readonly IEverWalletFactory _walletFactory;

    public WalletBotCommandReceivedConsumer(ITelegramBotClient botClient, IMemoryCache memoryCache, IEverWalletFactory walletFactory) : base(
        Command.Wallet,
        botClient,
        memoryCache) {
        _walletFactory = walletFactory;
    }

    private static string FormatInfoMessage(WalletInfo info) {
        var tokens = info.TokenBalances is not null
            ? string.Join('\n', info.TokenBalances
                .Select(t => $"   {t.Balance}{t.Symbol}"))
            : null;

        return $"`{info.Address}`\n" +
               $"Balance {info.Balance ?? 0:F}{Constants.Currency}\n".ToEscapedMarkdownV2() +
               tokens;
    }


    protected override async Task<string?> ConsumeAndGetReply(string[] args, Message message, long chatId,
        int messageThreadId, bool isAdmin,
        CancellationToken cancellationToken) {
        if (message is not { From.Id : var userId }) {
            return "User not found";
        }

        var wallet = await _walletFactory.GetWallet(userId, cancellationToken);
        var info = await wallet.GetInfo(cancellationToken);

        return FormatInfoMessage(info);
    }
}
