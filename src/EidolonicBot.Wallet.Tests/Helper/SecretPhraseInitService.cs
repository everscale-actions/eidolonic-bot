namespace EidolonicBot.Helper;

public class SecretPhraseInitService : IHostedService {
    private readonly IEverClient _everClient;
    private readonly SecretPhrase _secretPhrase;

    public SecretPhraseInitService(IEverClient everClient, SecretPhrase secretPhrase) {
        _everClient = everClient;
        _secretPhrase = secretPhrase;
    }

    public async Task StartAsync(CancellationToken cancellationToken) {
        _secretPhrase.Phrase = (await _everClient.Crypto.MnemonicFromRandom(new ParamsOfMnemonicFromRandom(), cancellationToken)).Phrase;
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }
}
