using System.Globalization;
using System.Numerics;
using EverscaleNet;

namespace EidolonicBot.Contracts;

public class TokenRoot : AccountBase, ITokenRoot {
    public TokenRoot(IEverClient client, IEverPackageManager packageManager) :
        base(client, packageManager) { }

    protected override string Name => "TokenRoot";


    public async Task Deploy(
        string initialSupplyTo,
        decimal initialSupply,
        decimal deployWalletValue,
        bool mintDisabled,
        bool burnByRootDisabled,
        bool burnPaused,
        string remainingGasTo,
        CancellationToken cancellationToken) {
        var s = initialSupply.ToString(CultureInfo.CurrentCulture);
        await base.Deploy(new {
            initialSupplyTo,
            initialSupply = s,
            deployWalletValue = deployWalletValue.ToString(CultureInfo.CurrentCulture),
            mintDisabled,
            burnByRootDisabled,
            burnPaused,
            remainingGasTo
        }, cancellationToken);
    }

    public async Task Init(IInternalSender sender, string name, string symbol, ushort decimals, string rootOwner, string walletCode,
        BigInteger randomNonce,
        string deployer, CancellationToken cancellationToken) {
        await base.Init(sender, new {
            name_ = name,
            symbol_ = symbol,
            decimals_ = decimals,
            rootOwner_ = rootOwner,
            walletCode_ = walletCode,
            randomNonce_ = randomNonce.ToString(),
            deployer_ = deployer
        }, cancellationToken: cancellationToken);
    }
}
