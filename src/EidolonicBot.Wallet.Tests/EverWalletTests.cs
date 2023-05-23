namespace EidolonicBot;

public class EverWalletTests {
    private readonly CancellationToken _cancellationToken;
    private readonly IEverGiver _giver;
    private readonly IServiceProvider _sp;
    private readonly IEverWalletFactory _walletFactory;

    public EverWalletTests(IServiceProvider sp, IEverWalletFactory walletFactory, IEverGiver giver,
        CancellationTokenSource cancellationTokenSource) {
        _sp = sp;
        _walletFactory = walletFactory;
        _giver = giver;
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

    // [Fact]
    // public async Task SendCoins_DestinationAccountGetsEvers() {
    //     await _wallet.Init(1, default);
    // }
}
