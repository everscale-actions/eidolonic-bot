using MassTransit;

namespace EidolonicBot.Commands;

public class ReloadSubscriptionServiceConsumer(
  ISubscriptionService subscriptionService
) : IConsumer<ReloadSubscriptionService>, IMediatorConsumer {
  public Task Consume(ConsumeContext<ReloadSubscriptionService> context) {
    return subscriptionService.Reload(context.CancellationToken);
  }
}
