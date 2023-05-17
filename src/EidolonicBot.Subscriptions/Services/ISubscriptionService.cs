namespace EidolonicBot.Services;

public interface ISubscriptionService {
    Task Reload(CancellationToken cancellationToken);
}
