namespace EidolonicBot.Services;

internal class PullingService(
  ILogger<PullingService> logger,
  IUpdateHandler updateHandler,
  ITelegramBotClient client,
  IHostEnvironment hostEnvironment
)
  : BackgroundService {
  private readonly ReceiverOptions _receiverOptions = new() {
    AllowedUpdates = [],
    DropPendingUpdates = hostEnvironment.IsDevelopment()
  };

  protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
    while (!stoppingToken.IsCancellationRequested) {
      logger.LogInformation("Starting polling service");

      try {
        await client.ReceiveAsync(updateHandler, _receiverOptions, stoppingToken);
      }
      catch (Exception ex) {
        logger.LogError(ex, "Polling failed");
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
      }
    }
  }
}
