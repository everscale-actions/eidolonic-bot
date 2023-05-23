using EidolonicBot.Contracts;

namespace EidolonicBot.Services;

public class TokenRootInitService : IHostedService {
    private const string ZeroAddress = "0:0000000000000000000000000000000000000000000000000000000000000000";
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
        var tokenWalletTvc = await _packageManager.LoadTvc("TokenWallet", cancellationToken) ??
                             throw new InvalidOperationException("TokenWallet code file should be provided");


        var wallet = await _walletFactory.GetWallet(1, cancellationToken);
        await _tokenRoot.Init(wallet, "Edolonic", "EDLC", 9, wallet.Address, tokenWalletTvc, 0, wallet.Address, cancellationToken);
        await _giver.SendTransaction(_tokenRoot.Address, 10m.CoinsToNano(), cancellationToken: cancellationToken);
        await _tokenRoot.Deploy(ZeroAddress, 0m, 1m.CoinsToNano(), false, false, false, wallet.Address, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }
}
