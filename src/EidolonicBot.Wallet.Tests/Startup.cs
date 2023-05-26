using EidolonicBot.GraphQL;
using EverscaleNet.Adapter.Rust;
using EverscaleNet.Client;
using EverscaleNet.Client.PackageManager;
using EverscaleNet.Models;
using EverscaleNet.TestSuite.Giver;
using EverscaleNet.TestSuite.Services;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Extensions.Logging;
using Xunit.DependencyInjection;

namespace EidolonicBot;

public class Startup {
    public void ConfigureHost(IHostBuilder hostBuilder) {
        hostBuilder
            .ConfigureHostConfiguration(builder => {
                builder.AddInMemoryCollection(new Dictionary<string, string?> {
                    { "Wallet:SeedPhrase", "solar hint assume increase monitor enough front ankle inject laptop vicious fortune" },
                    { "Endpoint", "http://localhost/graphql" }
                });
            })
            .ConfigureLogging(builder => {
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddXunitOutput();
            })
            .ConfigureServices((context, services) => {
                services.AddMemoryCache();

                var endpoint = context.Configuration["Endpoint"];

                if (endpoint is null) {
                    services.AddSingleton<NodeSeDockerContainer>()
                        .AddHostedService<InitNodeSeService>();
                }

                AddEverClientWithSerilog(endpoint, services);


                // services.AddEverClient((sp, options) => {
                //     var endpoint = sp.GetRequiredService<NodeSeDockerContainer>().Endpoint;
                //     options.Network.Endpoints = new[] { endpoint };
                // });

                services.AddHttpClient(nameof(GraphQLClient))
                    .AddTypedClient<GraphQLClient>((client, sp) => {
                        client.BaseAddress = new Uri(endpoint ?? sp.GetRequiredService<NodeSeDockerContainer>().Endpoint);
                        return new GraphQLClient(client);
                    });
                //
                // services.AddSingleton<Secret>()
                //     .AddHostedService<SecretInitService>();

                services.AddSingleton<IEverGiver, GiverV3>();

                services
                    .AddTransient<EverWallet>()
                    .Configure<EverWalletOptions>(context.Configuration.GetSection("Wallet"))
                    .AddSingleton<IEverWalletFactory, EverWalletFactory>();

                // services
                //     .AddTransient<ITokenRoot, TokenRootUpgradeable>()
                //     .AddHostedService<TokenRootInitService>();

                services.AddTransient(_ => new CancellationTokenSource(TimeSpan.FromSeconds(10)));
            });
    }

    private static void AddEverClientWithSerilog(string? endpoint, IServiceCollection services) {
        services.AddOptions();
        services.AddSingleton<IConfigureOptions<EverClientOptions>>(
                provider => new ConfigureOptions<EverClientOptions>(options => {
                    options.Network.Endpoints = new[] {
                        endpoint ?? provider.GetRequiredService<NodeSeDockerContainer>().Endpoint
                    };
                }))
            .AddSingleton<IEverClientAdapter>(provider => {
                var optionsAccessor = provider.GetRequiredService<IOptions<EverClientOptions>>();
                var output = provider.GetRequiredService<ITestOutputHelperAccessor>();
                var loggerFactory = new LoggerFactory(new[] {
                    new SerilogLoggerProvider(new LoggerConfiguration()
                        .MinimumLevel.Verbose()
                        .WriteTo.TestOutput(output.Output)
                        .CreateLogger(), true)
                });
                return new EverClientRustAdapter(optionsAccessor, loggerFactory.CreateLogger<EverClientRustAdapter>());
            })
            .AddTransient<IEverClient, EverClient>()
            .AddTransient<IEverPackageManager, FilePackageManager>();
    }
}
