using MassTransit;

namespace EidolonicBot.Commands;

public class ReloadSubscriptionServiceConsumer : IConsumer<ReloadSubscriptionService>, IMediatorConsumer {
    private readonly ISubscriptionService _subscriptionService;

    public ReloadSubscriptionServiceConsumer(ISubscriptionService subscriptionService) {
        _subscriptionService = subscriptionService;
    }

    public Task Consume(ConsumeContext<ReloadSubscriptionService> context) {
        return _subscriptionService.Reload(context.CancellationToken);
    }
}
