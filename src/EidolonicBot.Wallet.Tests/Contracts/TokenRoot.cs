using System.Globalization;
using System.Numerics;
using EidolonicBot.Models;
using EverscaleNet;

namespace EidolonicBot.Contracts;

public class TokenRoot : AccountBase, ITokenRoot {
    public TokenRoot(IEverClient client, IEverPackageManager packageManager, Secret secret) :
        base(client, packageManager, secret.KeyPair) { }

    protected override string Name => "TokenRoot";

    public async Task Init(string name, string symbol, ushort decimals, string rootOwner, string walletCode, BigInteger randomNonce,
        string deployer, CancellationToken cancellationToken) {
        await base.Init(initialData: new {
            name_ = name,
            symbol_ = symbol,
            decimals_ = decimals,
            rootOwner_ = rootOwner,
            walletCode_ = walletCode,
            randomNonce_ = randomNonce,
            deployer_ = deployer
        }, cancellationToken: cancellationToken);
    }

    public async Task Deploy(string initialSupplyTo, decimal initialSupply, decimal deployWalletValue, bool mintDisabled,
        bool burnByRootDisabled,
        bool burnPaused, string remainingGasTo, CancellationToken cancellationToken) {
        await base.Deploy(new {
            initialSupplyTo_ = initialSupplyTo,
            initialSupply = initialSupply.ToString(CultureInfo.CurrentCulture),
            deployWalletValue = deployWalletValue.ToString(CultureInfo.CurrentCulture),
            mintDisabled_ = mintDisabled,
            burnByRootDisabled_ = burnByRootDisabled,
            burnPaused_ = burnPaused,
            remainingGasTo_ = remainingGasTo
        }, cancellationToken);
    }
}
