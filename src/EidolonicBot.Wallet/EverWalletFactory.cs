using Microsoft.Extensions.DependencyInjection;

namespace EidolonicBot;

internal class EverWalletFactory : IEverWalletFactory {
    private readonly IEverWallet _everWallet;

    public EverWalletFactory(IServiceProvider serviceProvider) {
        _everWallet = serviceProvider.GetRequiredService<EverWallet>();
    }

    public async Task<IEverWallet> CreateWallet(long userId, CancellationToken cancellationToken) {
        return await _everWallet.Init(userId, cancellationToken);
    }
}
