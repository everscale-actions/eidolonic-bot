using System.Diagnostics;
using System.Text.Json;
using EverscaleNet.Abstract;
using EverscaleNet.Client.Models;
using EverscaleNet.Models;
using EverscaleNet.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EidolonicBot.Services;

public class SubscriptionService : ISubscriptionService, IAsyncDisposable {
    private const string SubscriptionQuery = "subscription($addresses:StringFilter){transactions(filter:{account_addr:$addresses}){id}}";

    private readonly ILogger<SubscriptionService> _logger;
    private readonly AsyncServiceScope _scope;
    private AppDbContext? _db;
    private IEverClient? _everClient;
    private uint? _handler;

    public SubscriptionService(ILogger<SubscriptionService> logger, IServiceProvider serviceProvider) {
        _logger = logger;
        _scope = serviceProvider.CreateAsyncScope();
        _db = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _everClient = _scope.ServiceProvider.GetRequiredService<IEverClient>();
    }

    public async ValueTask DisposeAsync() {
        await _scope.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    public async Task StartAsync(CancellationToken cancellationToken) {
        _logger.LogInformation("Starting subscription service");

        Debug.Assert(_db != null, nameof(_db) + " != null");
        Debug.Assert(_everClient != null, nameof(_everClient) + " != null");

        var subscription = await _db.Subscription.ToArrayAsync(cancellationToken);
        var addresses = subscription.Select(s => s.Address).ToArray();
        var resultOfSubscribeCollection = await _everClient.Net.Subscribe(new ParamsOfSubscribe {
            Subscription = SubscriptionQuery,
            Variables = new { addresses = new { @in = addresses } }.ToJsonElement()
        }, LogCallback, cancellationToken);
        _handler = resultOfSubscribeCollection.Handle;
    }

    public async Task Restart(CancellationToken cancellationToken) {
        await StopAsync(cancellationToken);
        await StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken) {
        _logger.LogInformation("Stopping subscription service");

        Debug.Assert(_everClient != null, nameof(_everClient) + " != null");
        Debug.Assert(_handler != null, nameof(_handler) + " != null");

        await _everClient.Net.Unsubscribe(new ResultOfSubscribeCollection { Handle = _handler.Value }, cancellationToken);
    }

    private void LogCallback(JsonElement e, uint responseType) {
        switch ((SubscriptionResponseType)responseType) {
            case SubscriptionResponseType.Ok:
                _logger.LogInformation("{Transaction}", JsonSerializer.Serialize(e));
                break;
            case SubscriptionResponseType.Error:
                _logger.LogInformation("Subscription error {@Error}", e);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(responseType), responseType, null);
        }
    }
}