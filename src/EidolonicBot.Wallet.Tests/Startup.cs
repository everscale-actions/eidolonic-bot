using EidolonicBot.GraphQL;
using EidolonicBot.Models;
using EidolonicBot.Services;
using Microsoft.Extensions.Configuration;

namespace EidolonicBot;

public class Startup {
    public void ConfigureHost(IHostBuilder hostBuilder) {
        hostBuilder.ConfigureHostConfiguration(builder => {
            builder.AddInMemoryCollection(new Dictionary<string, string?> {
                { "Wallet:SeedPhrase", "solar hint assume increase monitor enough front ankle inject laptop vicious fortune" }
            });
        });
        hostBuilder
            .ConfigureLogging(builder => {
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddXunitOutput();
            })
            .ConfigureServices((context, services) => {
                services.AddMemoryCache();

                services.AddSingleton<NodeSeDockerContainer>()
                    .AddHostedService<InitNodeSeService>();

                services.AddEverClient((sp, options) => {
                    var endpoint = sp.GetRequiredService<NodeSeDockerContainer>().Endpoint;
                    options.Network.Endpoints = new[] { endpoint };
                });

                services.AddHttpClient(nameof(GraphQLClient))
                    .AddTypedClient<GraphQLClient>((client, sp) => {
                        var endpoint = sp.GetRequiredService<NodeSeDockerContainer>().Endpoint;
                        client.BaseAddress = new Uri(endpoint);
                        return new GraphQLClient(client);
                    });

                services.AddSingleton<Secret>()
                    .AddHostedService<SecretInitService>();

                services.AddSingleton<IEverGiver, EverGiverV3>();

                services
                    .AddTransient<EverWallet>()
                    .Configure<EverWalletOptions>(context.Configuration.GetSection("Wallet"))
                    .AddSingleton<IEverWalletFactory, EverWalletFactory>();

                // services
                //     .AddTransient<ITokenRoot, TokenRoot>()
                //     .AddHostedService<TokenRootInitService>();

                services.AddTransient(_ => new CancellationTokenSource(TimeSpan.FromSeconds(10)));
            });
    }
}
