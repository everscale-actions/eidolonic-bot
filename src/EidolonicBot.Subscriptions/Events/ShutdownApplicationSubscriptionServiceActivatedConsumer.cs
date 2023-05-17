using MassTransit;

namespace EidolonicBot.Events;

public class ShutdownApplicationSubscriptionServiceActivatedConsumer : IConsumer<SubscriptionServiceActivated> {
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly ILogger<ShutdownApplicationSubscriptionServiceActivatedConsumer> _logger;

    public ShutdownApplicationSubscriptionServiceActivatedConsumer(
        ILogger<ShutdownApplicationSubscriptionServiceActivatedConsumer> logger,
        IHostApplicationLifetime applicationLifetime) {
        _logger = logger;
        _applicationLifetime = applicationLifetime;
    }

    public Task Consume(ConsumeContext<SubscriptionServiceActivated> context) {
        if (Constants.ApplicationId == context.Message.ApplicationId) {
            return Task.CompletedTask;
        }

        _logger.LogInformation("There is a new instance of application was started, so shutdown this one");
        _applicationLifetime.StopApplication();
        return Task.CompletedTask;
    }
}
