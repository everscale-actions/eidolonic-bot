using System.Numerics;
using EverscaleNet;

namespace EidolonicBot.Contracts;

public class TokenRootUpgradeable : AccountBase, ITokenRoot {
    private readonly IEverClient _everClient;
    private readonly IEverPackageManager _packageManager;

    public TokenRootUpgradeable(IEverClient everClient, IEverPackageManager packageManager) :
        base(everClient, packageManager) {
        _everClient = everClient;
        _packageManager = packageManager;
    }

    protected override string Name => nameof(TokenRootUpgradeable);

    public async Task Deploy(
        string initialSupplyTo,
        BigInteger initialSupply,
        decimal deployWalletValueCoins,
        bool mintDisabled,
        bool burnByRootDisabled,
        bool burnPaused,
        string remainingGasTo,
        CancellationToken cancellationToken) {
        var deployWalletValue = deployWalletValueCoins.CoinsToNano().ToString();

        await base.Deploy(new {
            initialSupplyTo,
            initialSupply = initialSupply.ToString(),
            deployWalletValue,
            mintDisabled,
            burnByRootDisabled,
            burnPaused,
            remainingGasTo
        }, cancellationToken);
    }

    public async Task<ResultOfProcessMessage> Mint(string recipient, BigInteger amount, string remainingGasTo, CancellationToken cancellationToken) {
        return await Run("mint",
            new {
                amount = amount.ToString(),
                recipient,
                deployWalletValue = 5m.CoinsToNano().ToString(),
                remainingGasTo,
                notify = false,
                payload = Constants.EmptyCellBoc
            }, cancellationToken);
    }

    public async Task Init(IInternalSender sender, string name, string symbol, ushort decimals, string rootOwner,
        string deployer, CancellationToken cancellationToken) {
        var tokenWalletTvc = await _packageManager.LoadTvc("TokenWalletUpgradeable", cancellationToken) ??
                             throw new InvalidOperationException("TokenWalletUpgradeable code file should be provided");

        var tokenWalletCode = (await _everClient.Boc.DecodeStateInit(new ParamsOfDecodeStateInit {
            StateInit = tokenWalletTvc
        }, cancellationToken)).Code;

        var tokenWalletPlatformTvc = await _packageManager.LoadTvc("TokenWalletPlatform", cancellationToken) ??
                                     throw new InvalidOperationException("TokenWalletPlatform code file should be provided");

        var tokenWalletPlatformCode = (await _everClient.Boc.DecodeStateInit(new ParamsOfDecodeStateInit {
            StateInit = tokenWalletPlatformTvc
        }, cancellationToken)).Code;

        await base.Init(sender, new {
            name_ = name,
            symbol_ = symbol,
            decimals_ = decimals,
            rootOwner_ = rootOwner,
            walletCode_ = tokenWalletCode,
            randomNonce_ = Random.Shared.NextInt64(),
            deployer_ = deployer,
            platformCode_ = tokenWalletPlatformCode
        }, cancellationToken);
    }
}
