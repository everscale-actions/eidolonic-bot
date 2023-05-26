using EidolonicBot.Contracts;
using EverscaleNet.Models;
using EverscaleNet.TestSuite.Giver;
using Xunit.DependencyInjection;

namespace EidolonicBot;

public class EverWalletTests {
    private readonly CancellationToken _cancellationToken;
    private readonly IEverClient _everClient;
    private readonly IEverGiver _giver;
    private readonly IEverPackageManager _packageManager;
    private readonly IServiceProvider _sp;
    private readonly IEverWalletFactory _walletFactory;

    public EverWalletTests(IServiceProvider sp, IEverWalletFactory walletFactory, IEverGiver giver,
        CancellationTokenSource cancellationTokenSource, IEverClient everClient, IEverPackageManager packageManager) {
        _sp = sp;
        _walletFactory = walletFactory;
        _giver = giver;
        _everClient = everClient;
        _packageManager = packageManager;
        _cancellationToken = cancellationTokenSource.Token;
    }

    [Fact]
    public void AddressGet_ThrowsNotInitializedException() {
        var wallet = _sp.GetRequiredService<EverWallet>();

        var act = () => wallet.Address;

        act.Should().Throw<NotInitializedException>();
    }

    [Fact]
    public async Task BalanceAndAccountType_ReturnsAddressAndNullBalanceAndNullType() {
        var wallet = await _walletFactory.GetWallet(long.MaxValue, _cancellationToken);

        var balance = await wallet.GetBalance(_cancellationToken);
        var type = await wallet.GetAccountType(_cancellationToken);

        using var scope = new AssertionScope();
        wallet.Address.Should().HaveLength(66);
        balance.Should().Be(null);
        type.Should().Be(null);
    }

    [Fact]
    public async Task SendCoins_DestinationAccountGetsEvers() {
        var wallet = await _walletFactory.GetWallet(10, _cancellationToken);
        var secondWallet = await _walletFactory.GetWallet(11, _cancellationToken);

        await _giver.SendTransaction(wallet.Address, 0.2m, cancellationToken: _cancellationToken);

        var secondBefore = await secondWallet.GetBalance(_cancellationToken) ?? 0;
        await wallet.SendCoins(secondWallet.Address, 1m.NanoToCoins(), false, cancellationToken: _cancellationToken);
        var walletAfterSendAndInit = await wallet.GetBalance(_cancellationToken);
        await wallet.SendCoins(secondWallet.Address, 1m.NanoToCoins(), false, cancellationToken: _cancellationToken);
        var walletAfterSecondSend = await wallet.GetBalance(_cancellationToken);
        var secondAfter = await secondWallet.GetBalance(_cancellationToken);

        using var scope = new AssertionScope();
        (secondAfter - secondBefore).Should().Be(2m.NanoToCoins());
        (0.1m - walletAfterSendAndInit).Should().BeLessThan(0.01m);
        (walletAfterSendAndInit - walletAfterSecondSend).Should().BeLessThan(0.2m - walletAfterSendAndInit!.Value);
    }

    [Theory]
    [InlineData(null)]
    public async Task InitTokenRoot([FromServices] ILogger<EverWalletTests> logger) {
        var wallet = await _walletFactory.GetWallet(20, _cancellationToken);
        if ((await wallet.GetBalance(_cancellationToken) ?? 0) < 10m) {
            await _giver.SendTransaction(wallet.Address, 100, cancellationToken: _cancellationToken);
        }

        ITokenRoot tokenRoot = new TokenRootUpgradeable(_everClient, _packageManager);

        const string owner = "0:b22097ca0d38319416f4c59ca25943a91dd2da311de659af120f452226b26936";

        await tokenRoot.Init(wallet, "Edolonic", "EDLC", 9, owner, wallet.Address, _cancellationToken);
        var accountType = await tokenRoot.GetAccountType(_cancellationToken);
        if (accountType is AccountType.Uninit or AccountType.NonExist) {
            await tokenRoot.Deploy(owner, 1000000, 5m, false, false, false, wallet.Address, _cancellationToken);
        }

        await tokenRoot.Mint(owner, 10000, wallet.Address, _cancellationToken);

        logger.LogInformation("Wallet address: {Address}", wallet.Address);
        logger.LogInformation("Owner address: {Address}", owner);
        logger.LogInformation("Root token address: {Address}", tokenRoot.Address);
    }
}
