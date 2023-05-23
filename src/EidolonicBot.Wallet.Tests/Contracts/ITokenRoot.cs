using System.Numerics;
using EverscaleNet;

namespace EidolonicBot.Contracts;

public interface ITokenRoot {
    string Address { get; }

    Task Init(IInternalSender sender, string name, string symbol, ushort decimals, string rootOwner, string walletCode, BigInteger randomNonce,
        string deployer, CancellationToken cancellationToken);

    Task Deploy(string initialSupplyTo, decimal initialSupply, decimal deployWalletValue, bool mintDisabled,
        bool burnByRootDisabled,
        bool burnPaused, string remainingGasTo, CancellationToken cancellationToken);
}
