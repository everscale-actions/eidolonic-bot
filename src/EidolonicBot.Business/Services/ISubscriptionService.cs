using Microsoft.Extensions.Hosting;

namespace EidolonicBot.Services;

public interface ISubscriptionService : IHostedService {
    Task Restart(CancellationToken cancellationToken);
}