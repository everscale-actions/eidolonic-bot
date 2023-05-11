using System.Diagnostics;
using System.Text.Json;
using EidolonicBot.Events;
using EverscaleNet.Abstract;
using EverscaleNet.Client.Models;
using EverscaleNet.Models;
using EverscaleNet.Serialization;
using EverscaleNet.Utils;
using MassTransit.Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EidolonicBot.Services;

public class SubscriptionService : ISubscriptionService, IAsyncDisposable {
    private const string SubscriptionQuery =
        @"subscription($addresses:StringFilter){transactions(filter:{account_addr:$addresses, balance_delta:{ne:""0""}}){id account_addr balance_delta(format:DEC)}}";

    private readonly ILogger<SubscriptionService> _logger;
    private readonly IScopedMediator _mediator;
    private readonly AsyncServiceScope _scope;
    private AppDbContext? _db;
    private IEverClient? _everClient;
    private uint? _handler;

    public SubscriptionService(ILogger<SubscriptionService> logger, IServiceProvider serviceProvider) {
        _logger = logger;
        _scope = serviceProvider.CreateAsyncScope();
        _db = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _everClient = _scope.ServiceProvider.GetRequiredService<IEverClient>();
        _mediator = _scope.ServiceProvider.GetRequiredService<IScopedMediator>();
    }

    public async ValueTask DisposeAsync() {
        await _scope.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    public async Task StartAsync(CancellationToken cancellationToken) {
        _logger.LogInformation("Starting subscription service");

        Debug.Assert(_db != null, nameof(_db) + " != null");
        Debug.Assert(_everClient != null, nameof(_everClient) + " != null");

        try {
            var addresses = await _db.Subscription.Select(s => s.Address).ToArrayAsync(cancellationToken);

            var resultOfSubscribeCollection = await _everClient.Net.Subscribe(new ParamsOfSubscribe {
                Subscription = SubscriptionQuery,
                Variables = new { addresses = new { @in = addresses } }.ToJsonElement()
            }, SubscriptionCallback, cancellationToken);
            _handler = resultOfSubscribeCollection.Handle;
        } catch (Exception e) {
            if (!cancellationToken.IsCancellationRequested) {
                _logger.LogError(e, "Subscription service error. Restarting...");
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
        }
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

    private async Task SubscriptionCallback(JsonElement e, uint responseType, CancellationToken cancellationToken) {
        switch ((SubscriptionResponseType)responseType) {
            case SubscriptionResponseType.Ok:
                var prototype = new {
                    result = new {
                        transactions = new { id = string.Empty, account_addr = string.Empty, balance_delta = string.Empty }
                    }
                };
                var transaction = e.ToPrototype(prototype).result.transactions;
                _logger.LogInformation("Got transaction by subscription {@Transaction}", transaction);
                try {
                    await _mediator.Publish(new SubscriptionReceived(
                        transaction.id,
                        transaction.account_addr,
                        transaction.balance_delta.NanoToCoins()
                    ), cancellationToken);
                } catch (Exception exception) {
                    _logger.LogError(exception, "Failed to publish SubscriptionReceived event");
                }

                break;
            case SubscriptionResponseType.Error:
                _logger.LogWarning("Subscription error {Error}", JsonSerializer.Serialize(e, JsonOptionsProvider.JsonSerializerOptions));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(responseType), responseType, null);
        }
    }
}
