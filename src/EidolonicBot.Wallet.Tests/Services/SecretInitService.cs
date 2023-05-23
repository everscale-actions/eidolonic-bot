using EidolonicBot.Models;

namespace EidolonicBot.Services;

public class SecretInitService : IHostedService {
    private readonly IEverClient _everClient;
    private readonly Secret _secret;

    public SecretInitService(IEverClient everClient, Secret secret) {
        _everClient = everClient;
        _secret = secret;
    }

    public async Task StartAsync(CancellationToken cancellationToken) {
        _secret.Phrase = (await _everClient.Crypto.MnemonicFromRandom(new ParamsOfMnemonicFromRandom(), cancellationToken)).Phrase;
        _secret.KeyPair =
            await _everClient.Crypto.MnemonicDeriveSignKeys(new ParamsOfMnemonicDeriveSignKeys { Phrase = _secret.Phrase }, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }
}
