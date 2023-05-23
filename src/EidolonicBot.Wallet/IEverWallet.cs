using EidolonicBot.Models;

namespace EidolonicBot;

public interface IEverWallet {
    string Address { get; }

    Task<(string transactionId, decimal totalOutputCoins)> SendCoins(string address, decimal coins, bool allBalance,
        string? mem = null, CancellationToken cancellationToken = default);

    Task<IEverWallet> Init(long userId, CancellationToken cancellationToken);
    Task<decimal?> GetBalance(CancellationToken cancellationToken);
    Task<AccountType?> GetAccountType(CancellationToken cancellationToken);
    Task<TokenBalance[]?> GetTokenBalances(CancellationToken cancellationToken);
}
