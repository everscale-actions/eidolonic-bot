namespace EidolonicBot;

public interface IEverWallet {
    string Address { get; }

    Task<(string transactionId, decimal totalOutputCoins)> SendCoins(long userId, decimal coins, bool allBalance,
        CancellationToken cancellationToken);

    Task<(string transactionId, decimal totalOutputCoins)> SendCoins(string address, decimal coins, bool allBalance,
        string? memo,
        CancellationToken cancellationToken);

    Task<EverWallet> Init(long userId, CancellationToken cancellationToken);
    Task<decimal?> GetBalance(CancellationToken cancellationToken);
    Task<AccountType?> GetAccountType(CancellationToken cancellationToken);
}
