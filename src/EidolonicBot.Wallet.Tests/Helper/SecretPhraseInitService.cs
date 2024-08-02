namespace EidolonicBot.Helper;

public class SecretPhraseInitService(
  IEverClient everClient,
  SecretPhrase secretPhrase
) : IHostedService {
  public async Task StartAsync(CancellationToken cancellationToken) {
    secretPhrase.Phrase = (await everClient.Crypto.MnemonicFromRandom(new ParamsOfMnemonicFromRandom(), cancellationToken)).Phrase;
  }

  public Task StopAsync(CancellationToken cancellationToken) {
    return Task.CompletedTask;
  }
}
