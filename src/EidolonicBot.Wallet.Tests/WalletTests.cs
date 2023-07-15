using EverscaleNet.TestSuite.Giver;

namespace EidolonicBot;

public class WalletTests {
    private readonly CancellationToken _cancellationToken;
    private readonly IEverGiver _giver;
    private readonly IEverWallet _secondWallet;
    private readonly IEverWallet _wallet;

    public WalletTests(IEverWallet wallet, IEverWallet secondWallet, IEverGiver giver, CancellationTokenSource cancellationTokenSource) {
        _wallet = wallet;
        _secondWallet = secondWallet;
        _giver = giver;
        _cancellationToken = cancellationTokenSource.Token;
    }

    [Fact]
    public void AddressGet_ThrowsNotInitializedException() {
        var act = () => _wallet.Address;

        act.Should().Throw<NotInitializedException>();
    }

    [Fact]
    public async Task BalanceAndAccountType_ReturnsAddressAndNullBalanceAndNullType() {
        await _wallet.Init(long.MaxValue, _cancellationToken);

        var balance = await _wallet.GetBalance(_cancellationToken);
        var type = await _wallet.GetAccountType(_cancellationToken);

        using var scope = new AssertionScope();
        _wallet.Address.Should().HaveLength(66);
        balance.Should().Be(null);
        type.Should().Be(null);
    }

    [Fact]
    public async Task SendCoins_DestinationAccountGetsEvers() {
        await _wallet.Init(1, default);
        await _secondWallet.Init(2, default);
        await _giver.SendTransaction(_wallet.Address, 0.2m, cancellationToken: _cancellationToken);

        var secondBefore = await _secondWallet.GetBalance(_cancellationToken) ?? 0;
        await _wallet.SendCoins(2, 1m.NanoToCoins(), false, _cancellationToken);
        var walletAfterSendAndInit = await _wallet.GetBalance(_cancellationToken);
        await _wallet.SendCoins(2, 1m.NanoToCoins(), false, _cancellationToken);
        var walletAfterSecondSend = await _wallet.GetBalance(_cancellationToken);
        var secondAfter = await _secondWallet.GetBalance(_cancellationToken);

        using var scope = new AssertionScope();
        (secondAfter - secondBefore).Should().Be(2m.NanoToCoins());
        (0.1m - walletAfterSendAndInit).Should().BeLessThan(0.01m);
        (walletAfterSendAndInit - walletAfterSecondSend).Should().BeLessThan(0.2m - walletAfterSendAndInit!.Value);
    }
}
