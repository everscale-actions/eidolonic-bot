using Microsoft.Extensions.DependencyInjection;

namespace EidolonicBot;

internal class EverWalletFactory : IEverWalletFactory {
    private readonly IServiceProvider _serviceProvider;

    public EverWalletFactory(IServiceProvider serviceProvider) {
        _serviceProvider = serviceProvider;
    }

    public async Task<IEverWallet> GetWallet(long userId, CancellationToken cancellationToken) {
        var everWallet = _serviceProvider.GetRequiredService<EverWallet>();
        return await everWallet.Init(userId, cancellationToken);
    }
}
