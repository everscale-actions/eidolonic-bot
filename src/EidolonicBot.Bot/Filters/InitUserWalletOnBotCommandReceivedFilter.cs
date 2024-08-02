namespace EidolonicBot.Filters;

public class InitUserWalletOnBotCommandReceivedFilter<T>(
  IEverWallet wallet
) : IFilter<PublishContext<T>>
  where T : class {
  public async Task Send(PublishContext<T> context, IPipe<PublishContext<T>> next) {
    if (context.Message is BotCommandReceived botCommandReceived
        && botCommandReceived.Command.IsWalletNeeded()
        && botCommandReceived.Message is { From.Id : var userId }) {
      await wallet.Init(userId, context.CancellationToken);
    }

    await next.Send(context);
  }

  public void Probe(ProbeContext context) { }
}
