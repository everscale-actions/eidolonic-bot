namespace EidolonicBot;

public interface IEverWalletFactory {
    Task<IEverWallet> GetWallet(long userId, CancellationToken cancellationToken);
}
