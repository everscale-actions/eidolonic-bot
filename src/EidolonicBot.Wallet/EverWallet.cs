using System.Numerics;
using System.Text.Json;
using EidolonicBot.Exceptions;
using EidolonicBot.GraphQL;
using EidolonicBot.Models;
using EverscaleNet;
using EverscaleNet.Abstract;
using EverscaleNet.Client.Models;
using EverscaleNet.Serialization;
using EverscaleNet.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EidolonicBot;

internal class EverWallet : IEverWallet {
    private const string WalletContractCodeBoc =
        "te6cckEBBgEA/AABFP8A9KQT9LzyyAsBAgEgAgMABNIwAubycdcBAcAA8nqDCNcY7UTQgwfXAdcLP8j4KM8WI88WyfkAA3HXAQHDAJqDB9cBURO68uBk3oBA1wGAINcBgCDXAVQWdfkQ8qj4I7vyeWa++COBBwiggQPoqFIgvLHydAIgghBM7mRsuuMPAcjL/8s/ye1UBAUAmDAC10zQ+kCDBtcBcdcBeNcB10z4AHCAEASqAhSxyMsFUAXPFlAD+gLLaSLQIc8xIddJoIQJuZgzcAHLAFjPFpcwcQHLABLM4skB+wAAPoIQFp4+EbqOEfgAApMg10qXeNcB1AL7AOjRkzLyPOI+zYS/";

    private const string WalletContractAbiJson =
        """{"ABI version":2,"version":"2.3","header":["pubkey","time","expire"],"functions":[{"name":"sendTransaction","inputs":[{"name":"dest","type":"address"},{"name":"value","type":"uint128"},{"name":"bounce","type":"bool"},{"name":"flags","type":"uint8"},{"name":"payload","type":"cell"}],"outputs":[]},{"name":"sendTransactionRaw","inputs":[{"name":"flags","type":"uint8"},{"name":"message","type":"cell"}],"outputs":[]}],"data":[],"events":[],"fields":[{"name":"_pubkey","type":"uint256"},{"name":"_timestamp","type":"uint64"}]}""";

    private const string TransferAbiJson =
        """{"ABI version":2,"functions":[{"name":"transfer","id":"0x00000000","inputs":[{"name":"comment","type":"bytes"}],"outputs":[]}],"events":[],"data":[]}""";

    private const string DataAbiParamsJson =
        """[{"name":"pubkey","type":"uint256"},{"name":"timestamp","type":"uint64"},{"name":"userHash","type":"uint256"}]""";

    private const string EmptyPayloadBoc = "te6ccgEBAQEAAgAAAA==";

    private static readonly Abi WalletAbi = new Abi.Contract {
        Value = JsonSerializer.Deserialize<AbiContract>(WalletContractAbiJson, JsonOptionsProvider.JsonSerializerOptions)
    };

    private static readonly Abi TransferAbi = new Abi.Contract {
        Value = JsonSerializer.Deserialize<AbiContract>(TransferAbiJson, JsonOptionsProvider.JsonSerializerOptions)
    };

    private static readonly AbiParam[] DataAbiParams =
        JsonSerializer.Deserialize<AbiParam[]>(DataAbiParamsJson, JsonOptionsProvider.JsonSerializerOptions)!;

    private readonly IEverClient _everClient;
    private readonly GraphQLClient _graphQL;
    private readonly ILogger<EverWallet> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly IOptions<EverWalletOptions> _walletOptions;

    private string? _address;
    private KeyPair? _keyPair;
    private string? _stateInitBoc;

    public EverWallet(IEverClient everClient, IOptions<EverWalletOptions> walletOptions, IMemoryCache memoryCache, ILogger<EverWallet> logger,
        GraphQLClient graphQL) {
        _everClient = everClient;
        _walletOptions = walletOptions;
        _memoryCache = memoryCache;
        _logger = logger;
        _graphQL = graphQL;
    }

    public async Task<decimal?> GetBalance(CancellationToken cancellationToken) {
        if (_address is null) {
            throw new NotInitializedException();
        }

        var variables = new { address = _address };
        var result = await _graphQL.Query(variables,
            static (i, o) =>
                o.Blockchain(query =>
                    query.Account(i.address, accountQuery =>
                        accountQuery.Info(account => new {
                            Balance = account.Balance(BigIntFormat.Dec)
                        }))),
            cancellationToken);

        return result.Data is { Balance: { } balance }
            ? balance.NanoToCoins()
            : null;
    }

    public async Task<AccountType?> GetAccountType(CancellationToken cancellationToken) {
        if (_address is null) {
            throw new NotInitializedException();
        }

        var variables = new { address = _address };
        var result = await _graphQL.Query(variables,
            static (i, o) =>
                o.Blockchain(query =>
                    query.Account(i.address, accountQuery =>
                        accountQuery.Info(account => new {
                            account.Acc_type
                        }))),
            cancellationToken);

        return result.Data is { Acc_type: { } accType }
            ? (AccountType)accType
            : null;
    }

    public async Task<TokenBalance[]?> GetTokenBalances(CancellationToken cancellationToken) {
        if (_address is null) {
            throw new NotInitializedException();
        }

        var variables = new { address = _address };
        var result = await _graphQL.Query(variables, static (i, o) =>
            o.Blockchain(query =>
                query.Account(i.address, accountQuery =>
                    accountQuery.Info(account =>
                        account.TokenHolder(holder =>
                            holder.Wallets(null, null, null, null, null, connection =>
                                connection.Nodes(wallet => new {
                                    balance = wallet.Balance,
                                    symbol = wallet.Token(token => token.Symbol)
                                })))))), cancellationToken);

        return result.Data is [] tokens
            ? tokens.Select(t => new TokenBalance(t.balance!.NanoToCoins(), t.symbol!)).ToArray()
            : null;
    }


    public async Task<(string transactionId, decimal totalOutputCoins)> SendCoins(string address, decimal coins, bool allBalance,
        string? memo,
        CancellationToken cancellationToken) {
        var balance = await GetBalance(cancellationToken) ?? throw new AccountInsufficientBalanceException(0);
        if (balance - coins < 0.1m) {
            throw new AccountInsufficientBalanceException(balance);
        }

        ResultOfProcessMessage result;
        try {
            var payload = memo is null ? null : await GetPayloadBodyByMemo(memo);
            result = await SendTransaction(address, new BigInteger(coins) << 9, false, allBalance, payload, cancellationToken);
        } catch (EverClientException exception) when (exception.Code == (uint)TvmErrorCode.LowBalance) {
            var balanceEx = await GetBalance(cancellationToken) ?? throw new AccountInsufficientBalanceException(0);
            throw new AccountInsufficientBalanceException(balanceEx);
        }

        var transactionId = result.Transaction!.Get<string>("id");
        var totalOutputCoins = result.Fees.TotalOutput.NanoToCoins();
        return (transactionId, totalOutputCoins);
    }

    public async Task<IEverWallet> Init(long userId, CancellationToken cancellationToken) {
        var phrase = _walletOptions.Value.SeedPhrase;
        if (phrase is null or "YOUR_SEED_PHRASE_HERE") {
            throw new NullReferenceException("Wallet:SeedPhrase should be provided");
        }

        _keyPair ??= await GetKeyPair(phrase ?? throw new InvalidOperationException(), cancellationToken);
        _stateInitBoc ??= await GetStateInitBoc(userId, _keyPair, cancellationToken);
        _address ??= await GetAddress(_stateInitBoc, cancellationToken);
        return this;
    }


    public string Address => _address ?? throw new NotInitializedException();

    public async Task<ResultOfProcessMessage> Send(string dest, decimal valueCoins, bool bounce, bool allBalance, Abi abi, CallSet callSet,
        string? stateInit = null,
        CancellationToken cancellationToken = default) {
        var value = valueCoins.CoinsToNano();

        // if (stateInit is null) {
        //     var resultOfEncodeMessageBody = await _everClient.Abi.EncodeMessageBody(new ParamsOfEncodeMessageBody {
        //         Abi = abi,
        //         CallSet = callSet,
        //         IsInternal = true,
        //         Signer = new Signer.None()
        //     }, cancellationToken);
        //
        //     var payload = resultOfEncodeMessageBody.Body;
        //
        //     return await SendTransaction(dest, value, bounce, allBalance, payload, cancellationToken);
        // }

        var body = (await _everClient.Abi.EncodeInternalMessage(new ParamsOfEncodeInternalMessage {
            Abi = abi,
            Value = value.ToString(),
            Address = dest,
            Bounce = bounce,
            DeploySet = stateInit is not null ? new DeploySet { StateInit = stateInit } : null,
            CallSet = callSet
        }, cancellationToken)).Message;

        return await SendTransactionRaw(allBalance, body, cancellationToken);
    }

    private async Task<string> GetPayloadBodyByMemo(string memo) {
        var result = await _everClient.Abi.EncodeMessageBody(new ParamsOfEncodeMessageBody {
            Abi = TransferAbi,
            CallSet = new CallSet {
                FunctionName = "transfer",
                Input = new { comment = memo.ToHexString() }.ToJsonElement()
            },
            IsInternal = true,
            Signer = new Signer.None()
        });

        return result.Body;
    }

    private async Task<ResultOfProcessMessage> SendTransaction(string dest, BigInteger value, bool bounce,
        bool allBalance,
        string? payload,
        CancellationToken cancellationToken) {
        if (_keyPair is null || _stateInitBoc is null || _address is null) {
            throw new NotInitializedException();
        }

        var flags = SendTransactionFlags.SenderWantsToPayTransferFeesSeparately | SendTransactionFlags.IgnoreSomeErrors;
        if (allBalance) {
            flags |= SendTransactionFlags.CarryAllRemainingBalance;
        }

        var callSet = new CallSet {
            FunctionName = "sendTransaction",
            Input = new {
                dest,
                value = value.ToString(),
                bounce,
                flags = (byte)flags,
                payload = payload ?? EmptyPayloadBoc
            }.ToJsonElement()
        };

        var bodyBoc = (await _everClient.Abi.EncodeMessageBody(new ParamsOfEncodeMessageBody {
            Address = _address,
            Abi = WalletAbi,
            CallSet = callSet,
            Signer = new Signer.Keys { KeysAccessor = _keyPair }
        }, cancellationToken)).Body;

        var accountType = await GetAccountType(cancellationToken);

        var callMessage = (await _everClient.Boc.EncodeExternalInMessage(new ParamsOfEncodeExternalInMessage {
            Dst = _address,
            Init = accountType is AccountType.Uninit ? _stateInitBoc : null,
            Body = bodyBoc
        }, cancellationToken)).Message;

        var shardBlockId = (await _everClient.Processing.SendMessage(new ParamsOfSendMessage {
            Message = callMessage,
            SendEvents = false
        }, cancellationToken: cancellationToken)).ShardBlockId;

        var resultOfProcessMessage = await _everClient.Processing.WaitForTransaction(new ParamsOfWaitForTransaction {
            Message = callMessage,
            ShardBlockId = shardBlockId
        }, cancellationToken: cancellationToken);

        return resultOfProcessMessage;
    }

    private async Task<KeyPair> GetKeyPair(string seedPhrase, CancellationToken cancellationToken) {
        var keypair = await _memoryCache.GetOrCreateAsync($"KeyPairBySeedPhrase_{seedPhrase}", async entity => {
            _logger.LogDebug("Getting keypair by seed phrase");

            var keyPair = await _everClient.Crypto.MnemonicDeriveSignKeys(new ParamsOfMnemonicDeriveSignKeys {
                Phrase = seedPhrase
            }, cancellationToken);

            entity.Size = 1;
            return keyPair;
        });
        return keypair ?? throw new InvalidOperationException();
    }

    private async Task<string> GetStateInitBoc(long userId, KeyPair keyPair,
        CancellationToken cancellationToken) {
        var stateInit = await _memoryCache.GetOrCreateAsync<string>($"StateInitBocByUserIdByPublicKey_{userId}_{keyPair.Public}",
            async entity => {
                _logger.LogDebug("Getting StateInit for {UserId}", userId);
                var userBoc = (await _everClient.Boc.EncodeBoc(new ParamsOfEncodeBoc {
                    Builder = new[] {
                        userId.ToBuilderOp()
                        // keyPair.Secret.ToBuilderOp()
                    }
                }, cancellationToken)).Boc;
                var userHash = (await _everClient.Boc.GetBocHash(new ParamsOfGetBocHash {
                    Boc = userBoc
                }, cancellationToken)).Hash;
                var dataBoc = (await _everClient.Abi.EncodeBoc(new ParamsOfAbiEncodeBoc {
                    @params = DataAbiParams,
                    Data = new {
                        pubkey = $"0x{keyPair.Public}",
                        timestamp = 0L,
                        userHash = $"0x{userHash}"
                    }.ToJsonElement()
                }, cancellationToken)).Boc;
                var stateInitBoc = (await _everClient.Boc.EncodeStateInit(new ParamsOfEncodeStateInit {
                    Code = WalletContractCodeBoc,
                    Data = dataBoc
                }, cancellationToken)).StateInit;
                entity.Size = 1;
                return stateInitBoc;
            });
        return stateInit ?? throw new InvalidOperationException();
    }

    private async Task<string> GetAddress(string stateInitBoc, CancellationToken cancellationToken) {
        var address = await _memoryCache.GetOrCreateAsync<string>($"AddressByStateInitBoc_{stateInitBoc}", async entity => {
            _logger.LogDebug("Getting Address for {StateInit}", stateInitBoc);
            var resultOfGetBocHash =
                await _everClient.Boc.GetBocHash(new ParamsOfGetBocHash { Boc = stateInitBoc }, cancellationToken);
            entity.Size = 1;
            return $"0:{resultOfGetBocHash.Hash}";
        });
        return address ?? throw new InvalidOperationException();
    }

    private async Task<ResultOfProcessMessage> SendTransactionRaw(bool allBalance, string message, CancellationToken cancellationToken) {
        if (_keyPair is null || _stateInitBoc is null || _address is null) {
            throw new NotInitializedException();
        }

        var flags = SendTransactionFlags.SenderWantsToPayTransferFeesSeparately | SendTransactionFlags.IgnoreSomeErrors;
        if (allBalance) {
            flags |= SendTransactionFlags.CarryAllRemainingBalance;
        }

        var callSet = new CallSet {
            FunctionName = "sendTransactionRaw",
            Input = new {
                flags = (byte)flags,
                message
            }.ToJsonElement()
        };

        var bodyBoc = (await _everClient.Abi.EncodeMessageBody(new ParamsOfEncodeMessageBody {
            Address = _address,
            Abi = WalletAbi,
            CallSet = callSet,
            Signer = new Signer.Keys { KeysAccessor = _keyPair }
        }, cancellationToken)).Body;

        var accountType = await GetAccountType(cancellationToken);

        var callMessage = (await _everClient.Boc.EncodeExternalInMessage(new ParamsOfEncodeExternalInMessage {
            Dst = _address,
            Init = accountType is AccountType.Uninit ? _stateInitBoc : null,
            Body = bodyBoc
        }, cancellationToken)).Message;

        var shardBlockId = (await _everClient.Processing.SendMessage(new ParamsOfSendMessage {
            Message = callMessage,
            SendEvents = false
        }, cancellationToken: cancellationToken)).ShardBlockId;

        var resultOfProcessMessage = await _everClient.Processing.WaitForTransaction(new ParamsOfWaitForTransaction {
            Message = callMessage,
            ShardBlockId = shardBlockId
        }, cancellationToken: cancellationToken);

        return resultOfProcessMessage;
    }
}
