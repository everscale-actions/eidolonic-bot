namespace EidolonicBot.Filters;

public class InitUserWalletOnBotCommandReceivedFilter<T> : IFilter<PublishContext<T>>
    where T : class {
    private readonly IEverWallet _wallet;

    public InitUserWalletOnBotCommandReceivedFilter(IEverWallet wallet) {
        _wallet = wallet;
    }

    public async Task Send(PublishContext<T> context, IPipe<PublishContext<T>> next) {
        if (context.Message is BotCommandReceived botCommandReceived
            && botCommandReceived.Command.IsWalletNeeded()
            && botCommandReceived.Message is { From.Id : var userId }) {
            await _wallet.Init(userId, context.CancellationToken);
        }

        await next.Send(context);
    }

    public void Probe(ProbeContext context) { }
}
