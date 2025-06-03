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

    act.ShouldThrow<NotInitializedException>();
  }

  [Fact]
  public async Task BalanceAndAccountType_ReturnsAddressAndNullBalanceAndNullType() {
    await wallet.Init(long.MaxValue, _cancellationToken);

    var balance = await wallet.GetBalance(_cancellationToken);
    var type = await wallet.GetAccountType(_cancellationToken);

    wallet.ShouldSatisfyAllConditions(
      () => wallet.Address.Length.ShouldBe(66),
      () => balance.ShouldBe(0),
      () => type.ShouldBe(AccountType.NonExist)
    );
  }

  [Fact]
  public async Task SendCoins_DestinationAccountGetsEvers() {
    await wallet.Init(1, TestContext.Current.CancellationToken);
    await secondWallet.Init(2, TestContext.Current.CancellationToken);
    await giver.SendTransaction(wallet.Address, 0.2m, cancellationToken: _cancellationToken);

    var secondBefore = await secondWallet.GetBalance(_cancellationToken) ?? 0;
    await wallet.SendCoins(2, 1m.NanoToCoins(), false, _cancellationToken);
    await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken: _cancellationToken);
    var walletAfterSendAndInit = await wallet.GetBalance(_cancellationToken) ?? 0;
    await wallet.SendCoins(2, 2m.NanoToCoins(), false, _cancellationToken);
    await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken: _cancellationToken);
    var walletAfterSecondSend = await wallet.GetBalance(_cancellationToken);
    var secondAfter = await secondWallet.GetBalance(_cancellationToken);

    wallet.ShouldSatisfyAllConditions(
      () => (secondAfter - secondBefore).ShouldBe(3m.NanoToCoins()),
      () => (0.1m - walletAfterSendAndInit).ShouldBeLessThan(0.01m),
      () => (walletAfterSendAndInit - walletAfterSecondSend)!.Value.ShouldBeLessThan(0.2m - walletAfterSendAndInit)
    );
  }
}
