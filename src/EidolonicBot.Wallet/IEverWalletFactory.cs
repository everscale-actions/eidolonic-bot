namespace EidolonicBot;

public interface IEverWalletFactory {
    Task<IEverWallet> CreateWallet(long userId, CancellationToken cancellationToken);
}
