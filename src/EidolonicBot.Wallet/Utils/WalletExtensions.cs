namespace EidolonicBot.Utils;

public static class WalletExtensions {
    public static async Task<WalletInfo> GetInfo(this IEverWallet wallet, CancellationToken cancellationToken) {
        return new WalletInfo(wallet.Address, await wallet.GetBalance(cancellationToken));
    }
}