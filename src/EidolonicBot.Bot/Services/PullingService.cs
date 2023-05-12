namespace EidolonicBot.Services;

public class PullingService : BackgroundService {
    private readonly ITelegramBotClient _client;
    private readonly ILogger<PullingService> _logger;
    private readonly ReceiverOptions _receiverOptions;
    private readonly IUpdateHandler _updateHandler;

    public PullingService(ILogger<PullingService> logger, IUpdateHandler updateHandler, ITelegramBotClient client,
        IHostEnvironment hostEnvironment) {
        _logger = logger;
        _updateHandler = updateHandler;
        _client = client;
        _receiverOptions = new ReceiverOptions {
            AllowedUpdates = Array.Empty<UpdateType>(),
            ThrowPendingUpdates = hostEnvironment.IsDevelopment()
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        while (!stoppingToken.IsCancellationRequested) {
            _logger.LogInformation("Starting polling service");

            try {
                await _client.ReceiveAsync(_updateHandler, _receiverOptions, stoppingToken);
            } catch (Exception ex) {
                _logger.LogError(ex, "Polling failed");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
