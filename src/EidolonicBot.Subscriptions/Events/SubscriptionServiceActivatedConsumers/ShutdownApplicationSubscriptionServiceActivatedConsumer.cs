using MassTransit;

namespace EidolonicBot.Events.SubscriptionServiceActivatedConsumers;

public class ShutdownApplicationSubscriptionServiceActivatedConsumer(
  ILogger<ShutdownApplicationSubscriptionServiceActivatedConsumer> logger,
  IHostApplicationLifetime applicationLifetime
) : IConsumer<SubscriptionServiceActivated> {
  public Task Consume(ConsumeContext<SubscriptionServiceActivated> context) {
    if (Constants.ApplicationStartDate >= context.Message.ApplicationStartDate) {
      return Task.CompletedTask;
    }

    logger.LogInformation("There is a new instance of application was started, so shutdown this one");
    applicationLifetime.StopApplication();
    return Task.CompletedTask;
  }
}
