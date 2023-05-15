namespace EidolonicBot.Services;

public interface ISubscriptionService : IHostedService {
    Task Reload(CancellationToken cancellationToken);
}
