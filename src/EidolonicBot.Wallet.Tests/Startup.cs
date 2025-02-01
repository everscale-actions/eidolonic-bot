using EidolonicBot.Helper;
using EverscaleNet.TestSuite.Giver;
using EverscaleNet.TestSuite.Services;

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
          options.Network.Endpoints = [endpoint];
        });

        services.AddSingleton<IEverGiver, GiverV3>();

        services.AddSingleton<SecretPhrase>()
          .AddHostedService<SecretPhraseInitService>();

        services
          .AddTransient<IEverWallet, EverWallet>()
          .AddSingleton<IConfigureOptions<EverWalletOptions>>(provider => {
            var secretPhraseService = provider.GetRequiredService<SecretPhrase>();
            return new ConfigureNamedOptions<EverWalletOptions>(
              null, options =>
                options.SeedPhrase = secretPhraseService.Phrase);
          });

        services.AddTransient(_ => new CancellationTokenSource(TimeSpan.FromSeconds(10)));
      });
  }
}
