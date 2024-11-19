using EverscaleNet.Models;
using EverscaleNet.TestSuite.Giver;

namespace EidolonicBot;

public class WalletTests(
  IEverWallet wallet,
  IEverWallet secondWallet,
  IEverGiver giver,
  CancellationTokenSource cancellationTokenSource
) {
  private readonly CancellationToken _cancellationToken = cancellationTokenSource.Token;

  [Fact]
  public void AddressGet_ThrowsNotInitializedException() {
    var act = () => wallet.Address;

    act.Should().Throw<NotInitializedException>();
  }

  [Fact]
  public async Task BalanceAndAccountType_ReturnsAddressAndNullBalanceAndNullType() {
    await wallet.Init(long.MaxValue, _cancellationToken);

    var balance = await wallet.GetBalance(_cancellationToken);
    var type = await wallet.GetAccountType(_cancellationToken);

    using var scope = new AssertionScope();
    wallet.Address.Should().HaveLength(66);
    balance.Should().Be(0);
    type.Should().Be(AccountType.NonExist);
  }

  [Fact]
  public async Task SendCoins_DestinationAccountGetsEvers() {
    await wallet.Init(1, default);
    await secondWallet.Init(2, default);
    await giver.SendTransaction(wallet.Address, 0.2m, cancellationToken: _cancellationToken);

    var secondBefore = await secondWallet.GetBalance(_cancellationToken) ?? 0;
    await wallet.SendCoins(2, 1m.NanoToCoins(), false, _cancellationToken);
    await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken: _cancellationToken);
    var walletAfterSendAndInit = await wallet.GetBalance(_cancellationToken) ?? 0;
    await wallet.SendCoins(2, 2m.NanoToCoins(), false, _cancellationToken);
    await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken: _cancellationToken);
    var walletAfterSecondSend = await wallet.GetBalance(_cancellationToken);
    var secondAfter = await secondWallet.GetBalance(_cancellationToken);

    using var scope = new AssertionScope();
    (secondAfter - secondBefore).Should().Be(3m.NanoToCoins());
    (0.1m - walletAfterSendAndInit).Should().BeLessThan(0.01m);
    (walletAfterSendAndInit - walletAfterSecondSend).Should().BeLessThan(0.2m - walletAfterSendAndInit);
  }
}
