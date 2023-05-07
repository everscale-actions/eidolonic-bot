using EverscaleNet.Models;

namespace EidolonicBot;

public interface IEverWallet {
    string Address { get; }
    Task<string> SendCoins(long userId, decimal coins, CancellationToken cancellationToken);
    Task<EverWallet> Init(long userId, CancellationToken cancellationToken);
    Task<decimal?> GetBalance(CancellationToken cancellationToken);
    Task<AccountType?> GetAccountType(CancellationToken cancellationToken);
}