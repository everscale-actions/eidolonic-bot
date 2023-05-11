using System.Reflection;
using EverscaleNet.Client.PackageManager;
using EverscaleNet.Models;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace EidolonicBot;

public static class HostApplicationBuilderExtensions {
    public static HostApplicationBuilder AddLogging(this HostApplicationBuilder builder) {
        builder.Services.AddLogging(loggingBuilder => {
            loggingBuilder.AddConfiguration(builder.Configuration);

            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .CreateLogger());

            loggingBuilder.AddSentry(options => options.Environment = builder.Environment.EnvironmentName);
        });
        return builder;
    }

    public static HostApplicationBuilder AddMemoryCache(this HostApplicationBuilder builder) {
        builder.Services.AddMemoryCache();
        builder.Services.Configure<MemoryCacheOptions>(builder.Configuration.GetSection("MemoryCache"));

        return builder;
    }

    public static HostApplicationBuilder AddEverClient(this HostApplicationBuilder builder) {
        builder.Services.AddSingleton<IStaticService, StaticService>();

        builder.Services.AddEverClient()
            .Configure<EverClientOptions>(builder.Configuration.GetSection("EverClient"))
            .Configure<FilePackageManagerOptions>(options =>
                options.PackagesPath =
                    Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "_contracts"));

        return builder;
    }

    public static HostApplicationBuilder AddEverWallet(this HostApplicationBuilder builder) {
        builder.Services.AddScoped<IEverWallet, EverWallet>()
            .Configure<EverWalletOptions>(builder.Configuration.GetSection("Wallet"));


        return builder;
    }
}
