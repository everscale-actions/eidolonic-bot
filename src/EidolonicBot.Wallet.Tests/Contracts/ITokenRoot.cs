using System.Numerics;
using EverscaleNet;

namespace EidolonicBot.Contracts;

public interface ITokenRoot : IAccount {
    Task Init(IInternalSender sender, string name, string symbol, ushort decimals, string rootOwner,
        string deployer, CancellationToken cancellationToken);

    Task Deploy(string initialSupplyTo, BigInteger initialSupply, decimal deployWalletValueCoins, bool mintDisabled,
        bool burnByRootDisabled,
        bool burnPaused, string remainingGasTo, CancellationToken cancellationToken);

    Task<ResultOfProcessMessage> Mint(string recipient, BigInteger amount, string remainingGasTo, CancellationToken cancellationToken);
}
