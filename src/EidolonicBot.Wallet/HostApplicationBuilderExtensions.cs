using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EidolonicBot;

public static class HostApplicationBuilderExtensions {
    public static HostApplicationBuilder AddEverWallet(this HostApplicationBuilder builder) {
        builder.Services.AddTransient<EverWallet>()
            .Configure<EverWalletOptions>(builder.Configuration.GetSection("Wallet"))
            .AddSingleton<IEverWalletFactory, EverWalletFactory>();
        return builder;
    }
}
