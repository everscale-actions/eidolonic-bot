using EidolonicBot.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EidolonicBot;

public static class HostApplicationBuilderExtensions {
    public static HostApplicationBuilder AddLinkFormatter(this HostApplicationBuilder builder) {
        builder.Services.Configure<BlockchainOptions>(builder.Configuration.GetSection("Blockchain"));

        builder.Services.AddSingleton<ILinkFormatter, LinkFormatter>();

        return builder;
    }
}
