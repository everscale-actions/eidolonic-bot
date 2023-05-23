using EidolonicBot.Contracts;
using EidolonicBot.GraphQL;
using EidolonicBot.Models;
using EidolonicBot.Services;

namespace EidolonicBot;

public class Startup {
    public void ConfigureHost(IHostBuilder hostBuilder) {
        hostBuilder
            .ConfigureLogging(builder => {
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddXunitOutput();
            })
            .ConfigureServices(services => {
                services.AddMemoryCache();

                services.AddSingleton<NodeSeDockerContainer>()
                    .AddHostedService<InitNodeSeService>();

                services.AddEverClient((sp, options) => {
                    var endpoint = sp.GetRequiredService<NodeSeDockerContainer>().Endpoint;
                    options.Network.Endpoints = new[] { endpoint };
                });

                services.AddHttpClient("GraphQLClient")
                    .AddTypedClient<GraphQLClient>((client, sp) => {
                        var endpoint = sp.GetRequiredService<NodeSeDockerContainer>().Endpoint;
                        client.BaseAddress = new Uri(endpoint);
                        return new GraphQLClient(client);
                    });

                services.AddSingleton<IEverGiver, EverGiverV3>();

                services.AddSingleton<Secret>()
                    .AddHostedService<SecretInitService>();

                services
                    .AddTransient<EverWallet>()
                    .AddSingleton<IConfigureOptions<EverWalletOptions>>(provider => {
                        var secretPhraseService = provider.GetRequiredService<Secret>();
                        return new ConfigureNamedOptions<EverWalletOptions>(null, options =>
                            options.SeedPhrase = secretPhraseService.Phrase);
                    })
                    .AddSingleton<IEverWalletFactory, EverWalletFactory>();

                services
                    .AddTransient<ITokenRoot, TokenRoot>()
                    .AddHostedService<TokenRootInitService>();

                services.AddTransient(_ => new CancellationTokenSource(TimeSpan.FromSeconds(10)));
            });
    }
}
