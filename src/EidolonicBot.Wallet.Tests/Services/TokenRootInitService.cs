using EidolonicBot.Contracts;
using EverscaleNet.TestSuite.Giver;

namespace EidolonicBot.Services;

public class TokenRootInitService : IHostedService {
    private readonly IEverGiver _giver;
    private readonly IEverPackageManager _packageManager;
    private readonly ITokenRoot _tokenRoot;
    private readonly IEverWalletFactory _walletFactory;

    public TokenRootInitService(ITokenRoot tokenRoot, IEverWalletFactory walletFactory, IEverGiver giver, IEverPackageManager packageManager) {
        _tokenRoot = tokenRoot;
        _walletFactory = walletFactory;
        _giver = giver;
        _packageManager = packageManager;
    }

    public async Task StartAsync(CancellationToken cancellationToken) {
        // var tokenWalletTvc = await _packageManager.LoadTvc("TokenWallet", cancellationToken) ??
        //                      throw new InvalidOperationException("TokenWallet code file should be provided");
        //
        // var tokenWalletPlatformTvc = await _packageManager.LoadTvc("TokenWalletPlatform", cancellationToken) ??
        //                               throw new InvalidOperationException("TokenWalletPlatform code file should be provided");
        //
        //
        // var wallet = await _walletFactory.GetWallet(1, cancellationToken);
        // await _giver.SendTransaction(wallet.Address, 20m, cancellationToken: cancellationToken);
        //
        // await _tokenRoot.Init(wallet, "Edolonic", "EDLC", 9, wallet.Address, tokenWalletTvc, 0, wallet.Address, tokenWalletPlatformTvc, cancellationToken);
        // await _tokenRoot.Deploy(Constants.ZeroAddress, 1, 2, false, false, false, wallet.Address, cancellationToken);
        await Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }
}
