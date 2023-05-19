namespace EidolonicBot.Utils;

public static class WalletExtensions {
    public static async Task<WalletInfo> GetInfo(this IEverWallet wallet, CancellationToken cancellationToken) {
        var balance = await wallet.GetBalance(cancellationToken);
        var tokenBalances = await wallet.GetTokenBalances(cancellationToken);

        return new WalletInfo(wallet.Address, balance, tokenBalances);
    }
}
