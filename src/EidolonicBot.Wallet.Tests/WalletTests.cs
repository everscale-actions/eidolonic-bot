using EidolonicBot.Exceptions;
using EverscaleNet;
using EverscaleNet.Utils;
using FluentAssertions.Execution;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EidolonicBot;

public class WalletTests : IAsyncLifetime {
    private static readonly string[] NetworkEndpoints = { "http://localhost/graphql" };
    private readonly TimeSpan _cancelAfter = TimeSpan.FromSeconds(10);
    private readonly CancellationToken _cancellationToken;
    private readonly IEverClient _everClient;
    private readonly EverNodeSeGiver _giver;

    private readonly ServiceProvider _sp;
    private readonly IEverWallet _wallet;

    public WalletTests(ITestOutputHelper output) {
        var services = new ServiceCollection();
        services.AddLogging(builder =>
            builder.AddSerilog(new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(output)
                .CreateLogger(), true));
        services.AddMemoryCache();
        services.AddEverClient(options => options.Network.Endpoints = NetworkEndpoints);
        services.AddOptions();
        services
            .AddScoped<IEverWallet, EverWallet>()
            .AddSingleton<IConfigureOptions<EverWalletOptions>>(provider => {
                var everClient = provider.GetRequiredService<IEverClient>();
                var phrase = everClient.Crypto.MnemonicFromRandom(new ParamsOfMnemonicFromRandom())
                    .GetAwaiter().GetResult().Phrase;
                return new ConfigureNamedOptions<EverWalletOptions>(null, options =>
                    options.SeedPhrase = phrase);
            });
        services.AddTransient<EverNodeSeGiver>();
        _sp = services.BuildServiceProvider();
        _wallet = _sp.GetRequiredService<IEverWallet>();
        _giver = _sp.GetRequiredService<EverNodeSeGiver>();
        _everClient = _sp.GetRequiredService<IEverClient>();
        var cts = new CancellationTokenSource();
        cts.CancelAfter(_cancelAfter);
        _cancellationToken = cts.Token;
    }

    public async Task InitializeAsync() {
        await _giver.InitByPackage(cancellationToken: _cancellationToken);
    }

    async Task IAsyncLifetime.DisposeAsync() {
        await _sp.DisposeAsync();
    }

    [Fact]
    public void GetInfo_ThrowsNotInitializedException() {
        var act = () => _wallet.Address;

        act.Should().Throw<NotInitializedException>();
    }

    [Fact]
    public async Task GetInfo_ReturnsAddressAndZeroBalance() {
        await _wallet.Init(long.MaxValue, _cancellationToken);

        var balance = await _wallet.GetBalance(_cancellationToken);
        var type = await _wallet.GetAccountType(_cancellationToken);

        using var scope = new AssertionScope();
        _wallet.Address.Should().HaveLength(66);
        balance.Should().Be(null);
        type.Should().Be(null);
    }

    [Fact]
    public async Task SendCoins_DestinationAccountReturnsEvers() {
        await _wallet.Init(1, default);
        var secondWallet = await CreateAnotherEverWallet(2);
        await _giver.SendTransaction(_wallet.Address, 0.1m, cancellationToken: _cancellationToken);

        var secondBefore = await secondWallet.GetBalance(_cancellationToken) ?? 0;
        await _wallet.SendCoins(2, 1m.NanoToCoins(), false, _cancellationToken);
        var walletAfterSendAndInit = await _wallet.GetBalance(_cancellationToken);
        await _wallet.SendCoins(2, 1m.NanoToCoins(), false, _cancellationToken);
        var walletAfterSecondSend = await _wallet.GetBalance(_cancellationToken);
        var secondAfter = await secondWallet.GetBalance(_cancellationToken);

        using var scope = new AssertionScope();
        (secondAfter - secondBefore).Should().Be(2m.NanoToCoins());
        (0.1m - walletAfterSendAndInit).Should().BeLessThan(0.01m);
        (walletAfterSendAndInit - walletAfterSecondSend).Should().BeLessThan(0.1m - walletAfterSendAndInit!.Value);
    }

    private async Task<EverWallet> CreateAnotherEverWallet(int secondUserId) {
        var wallet = new EverWallet(_everClient,
            _sp.GetRequiredService<IOptions<EverWalletOptions>>(),
            _sp.GetRequiredService<IMemoryCache>(),
            _sp.GetRequiredService<ILogger<EverWallet>>());
        return await wallet.Init(secondUserId, _cancellationToken);
    }
}
