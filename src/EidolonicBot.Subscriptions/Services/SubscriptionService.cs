using MassTransit;
using Polly;

namespace EidolonicBot.Services;

internal class SubscriptionService : IHostedService, ISubscriptionService, IAsyncDisposable
{
    private const string SubscriptionQuery =
        @"subscription($addresses:StringFilter){transactions(filter:{account_addr:$addresses,balance_delta:{ne:""0""}}){id account_addr account{balance(format:DEC)} balance_delta(format:DEC) out_messages{dst} in_message{src}}}";

    private readonly ILogger<SubscriptionService> _logger;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly AsyncServiceScope _scope;
    private AppDbContext? _db;
    private IEverClient? _everClient;
    private uint? _handler;

    public SubscriptionService(ILogger<SubscriptionService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _scope = serviceProvider.CreateAsyncScope();
        _db = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _everClient = _scope.ServiceProvider.GetRequiredService<IEverClient>();
        _publishEndpoint = _scope.ServiceProvider.GetRequiredService<IBus>();
    }

    public async ValueTask DisposeAsync()
    {
        await _scope.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting subscription service");

        try
        {
            await StartSubscription(cancellationToken);
        }
        catch (Exception e)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(e, "Subscription service error. Restarting in 10 seconds..");
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                await StartAsync(cancellationToken);
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping subscription service");

        if (_everClient is null || _handler is null)
        {
            return;
        }

        await _everClient.Net.Unsubscribe(new ResultOfSubscribeCollection { Handle = _handler.Value },
            cancellationToken);
    }

    public async Task Reload(CancellationToken cancellationToken)
    {
        Debug.Assert(_handler != null, nameof(_handler) + " != null");
        Debug.Assert(_everClient != null, nameof(_everClient) + " != null");

        uint? oldHandler = null;
        if (_handler is not null)
        {
            oldHandler = _handler.Value;
            await Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(10, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
                .ExecuteAsync(StartSubscription, cancellationToken);
        }

        if (oldHandler is not null)
        {
            await Policy
                .Handle<EverClientException>()
                .WaitAndRetryAsync(10, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
                .ExecuteAsync(ct => _everClient.Net.Unsubscribe(
                    new ResultOfSubscribeCollection { Handle = oldHandler.Value },
                    ct), cancellationToken);
        }
    }

    private async Task StartSubscription(CancellationToken cancellationToken)
    {
        Debug.Assert(_db != null, nameof(_db) + " != null");
        Debug.Assert(_everClient != null, nameof(_everClient) + " != null");

        var addresses = await _db.Subscription.Select(s => s.Address).ToArrayAsync(cancellationToken);
        _handler = null;
        var resultOfSubscribeCollection = await _everClient.Net.Subscribe(new ParamsOfSubscribe
        {
            Subscription = SubscriptionQuery,
            Variables = new { addresses = new { @in = addresses } }.ToJsonElement()
        }, SubscriptionCallback, cancellationToken);
        _handler = resultOfSubscribeCollection.Handle;

        // notify another instances of application 
        await _publishEndpoint.Publish(new SubscriptionServiceActivated(Constants.ApplicationStartDate),
            cancellationToken);
    }


    private async Task SubscriptionCallback(JsonElement e, uint responseType, CancellationToken cancellationToken)
    {
        switch ((SubscriptionResponseType)responseType)
        {
            case SubscriptionResponseType.Ok:
            {
                var prototype = new
                {
                    result = new
                    {
                        transactions = new
                        {
                            id = string.Empty,
                            account_addr = string.Empty,
                            account = new { balance = string.Empty },
                            balance_delta = string.Empty,
                            out_messages = ArrayExtensions.EmptyNullable(new { dst = string.Empty }),
                            in_message = new
                            {
                                src = string.Empty
                            }
                        }
                    }
                };
                var transaction = e.ToPrototype(prototype).result.transactions;
                using var subscriptionScope = _logger.BeginScope("{@Transaction}", transaction);
                _logger.LogInformation("Got transaction by subscription");
                try
                {
                    await using var scope = _scope.ServiceProvider.CreateAsyncScope();
                    var mediator = scope.ServiceProvider.GetRequiredService<IScopedMediator>();

                    var balanceDeltaCoins = transaction.balance_delta.NanoToCoins();
                    var from = string.IsNullOrEmpty(transaction.in_message.src) ? null : transaction.in_message.src;
                    var to = transaction.out_messages?.Select(m => m.dst).ToArray() ?? [];

                    await mediator.Publish(new SubscriptionReceived(
                        transaction.id,
                        transaction.account_addr,
                        balanceDeltaCoins,
                        from,
                        to,
                        transaction.account.balance.NanoToCoins()
                    ), cancellationToken);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Failed to publish SubscriptionReceived event");
                }

                break;
            }
            case SubscriptionResponseType.Error:
                _logger.LogWarning("Subscription error {@Error}", e.ToObject<ClientError>());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(responseType), responseType, null);
        }
    }
}
