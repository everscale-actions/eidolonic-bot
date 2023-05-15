namespace EidolonicBot.Services;

internal class SubscriptionService : ISubscriptionService, IAsyncDisposable {
    private const string SubscriptionQuery =
        @"subscription($addresses:StringFilter){transactions(filter:{account_addr:$addresses,balance_delta:{ne:""0""}}){id account_addr balance_delta(format:DEC)out_messages{dst}in_message{src}}}";

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

        try {
            var addresses = await _db.Subscription.Select(s => s.Address).ToArrayAsync(cancellationToken);

            var resultOfSubscribeCollection = await _everClient.Net.Subscribe(new ParamsOfSubscribe {
                Subscription = SubscriptionQuery,
                Variables = new { addresses = new { @in = addresses } }.ToJsonElement()
            }, SubscriptionCallback, cancellationToken);
            _handler = resultOfSubscribeCollection.Handle;
        } catch (Exception e) {
            if (!cancellationToken.IsCancellationRequested) {
                _logger.LogError(e, "Subscription service error. Restarting in 10 seconds..");
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                await StartAsync(cancellationToken);
            }
        }
    }

    public async Task Reload(CancellationToken cancellationToken) {
        //todo: make sure all transactions was caught
        await StopAsync(cancellationToken);
        await StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken) {
        _logger.LogInformation("Stopping subscription service");

        if (_everClient is null || _handler is null) {
            return;
        }

        await _everClient.Net.Unsubscribe(new ResultOfSubscribeCollection { Handle = _handler.Value }, cancellationToken);
    }


    private async Task SubscriptionCallback(JsonElement e, uint responseType, CancellationToken cancellationToken) {
        switch ((SubscriptionResponseType)responseType) {
            case SubscriptionResponseType.Ok:
                var prototype = new {
                    result = new {
                        transactions = new {
                            id = string.Empty,
                            account_addr = string.Empty,
                            balance_delta = string.Empty,
                            out_messages = ArrayExtensions.Empty(new {
                                dst = string.Empty
                            }),
                            in_message = new {
                                src = string.Empty
                            }
                        }
                    }
                };
                var transaction = e.ToPrototype(prototype).result.transactions;
                _logger.LogInformation("Got transaction by subscription {@Transaction}", transaction);
                try {
                    await using var scope = _scope.ServiceProvider.CreateAsyncScope();
                    var mediator = scope.ServiceProvider.GetRequiredService<IScopedMediator>();

                    var balanceDeltaCoins = transaction.balance_delta.NanoToCoins();
                    var counterparty = balanceDeltaCoins > 0 
                        ? transaction.in_message.src 
                        : transaction.out_messages[0].dst;

                    await mediator.Publish(new SubscriptionReceived(
                        TransactionId: transaction.id,
                        AccountAddr: transaction.account_addr,
                        BalanceDelta: balanceDeltaCoins,
                        Сounterparty: counterparty
                    ), cancellationToken);
                } catch (Exception exception) {
                    _logger.LogError(exception, "Failed to publish SubscriptionReceived event");
                }

                break;
            case SubscriptionResponseType.Error:
                _logger.LogWarning("Subscription error {@Error}", e.ToObject<ClientError>());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(responseType), responseType, null);
        }
    }
}
