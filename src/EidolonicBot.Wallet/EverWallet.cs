using System.Text.Json;
using EidolonicBot.Exceptions;
using EverscaleNet.Abstract;
using EverscaleNet.Client.Models;
using EverscaleNet.Serialization;
using EverscaleNet.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EidolonicBot;

public class EverWallet : IEverWallet {
    private const string WalletContractCodeBoc =
        "te6cckEBBgEA/AABFP8A9KQT9LzyyAsBAgEgAgMABNIwAubycdcBAcAA8nqDCNcY7UTQgwfXAdcLP8j4KM8WI88WyfkAA3HXAQHDAJqDB9cBURO68uBk3oBA1wGAINcBgCDXAVQWdfkQ8qj4I7vyeWa++COBBwiggQPoqFIgvLHydAIgghBM7mRsuuMPAcjL/8s/ye1UBAUAmDAC10zQ+kCDBtcBcdcBeNcB10z4AHCAEASqAhSxyMsFUAXPFlAD+gLLaSLQIc8xIddJoIQJuZgzcAHLAFjPFpcwcQHLABLM4skB+wAAPoIQFp4+EbqOEfgAApMg10qXeNcB1AL7AOjRkzLyPOI+zYS/";

    private const string WalletContractAbiJson =
        "{\"ABI version\":2,\"version\":\"2.3\",\"header\":[\"pubkey\",\"time\",\"expire\"],\"functions\":[{\"name\":\"sendTransaction\",\"inputs\":[{\"name\":\"dest\",\"type\":\"address\"},{\"name\":\"value\",\"type\":\"uint128\"},{\"name\":\"bounce\",\"type\":\"bool\"},{\"name\":\"flags\",\"type\":\"uint8\"},{\"name\":\"payload\",\"type\":\"cell\"}],\"outputs\":[]},{\"name\":\"sendTransactionRaw\",\"inputs\":[{\"name\":\"flags\",\"type\":\"uint8\"},{\"name\":\"message\",\"type\":\"cell\"}],\"outputs\":[]}],\"data\":[],\"events\":[],\"fields\":[{\"name\":\"_pubkey\",\"type\":\"uint256\"},{\"name\":\"_timestamp\",\"type\":\"uint64\"}]}";

    private static readonly Abi Abi = new Abi.Contract {
        Value = JsonSerializer.Deserialize<AbiContract>(WalletContractAbiJson, JsonOptionsProvider.JsonSerializerOptions)
    };

    private readonly IEverClient _everClient;
    private readonly ILogger<EverWallet> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly IOptions<EverWalletOptions> _walletOptions;

    private string? _address;
    private KeyPair? _keyPair;
    private string? _stateInitBoc;


    public EverWallet(IEverClient everClient, IOptions<EverWalletOptions> walletOptions, IMemoryCache memoryCache, ILogger<EverWallet> logger) {
        _everClient = everClient;
        _walletOptions = walletOptions;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<decimal?> GetBalance(CancellationToken cancellationToken) {
        var result = await _everClient.Net.QueryCollection(new ParamsOfQueryCollection {
            Collection = "accounts",
            Filter = new { id = new { eq = Address } }.ToJsonElement(),
            Result = "balance(format: DEC)",
            Limit = 1
        }, cancellationToken);

        return result.Result.Length == 1
            ? result.Result[0].Get<string>("balance").NanoToCoins()
            : null;
    }

    public async Task<AccountType?> GetAccountType(CancellationToken cancellationToken) {
        var result = await _everClient.Net.QueryCollection(new ParamsOfQueryCollection {
            Collection = "accounts",
            Filter = new { id = new { eq = Address } }.ToJsonElement(),
            Result = "acc_type",
            Limit = 1
        }, cancellationToken);

        return result.Result.Length == 1
            ? result.Result[0].Get<AccountType>("acc_type")
            : null;
    }

    public async Task<(string transactionId, decimal totalOutputCoins)> SendCoins(long userId, decimal coins, bool allBalance,
        CancellationToken cancellationToken) {
        if (_keyPair is null) {
            throw new NotInitializedException();
        }

        var destStateInitBoc = await GetStateInitBoc(userId, _keyPair, cancellationToken);
        var dest = await GetAddress(destStateInitBoc, cancellationToken);

        return await SendCoins(dest, coins, allBalance, cancellationToken);
    }

    public async Task<(string transactionId, decimal totalOutputCoins)> SendCoins(string address, decimal coins, bool allBalance,
        CancellationToken cancellationToken) {
        var balance = await GetBalance(cancellationToken) ?? throw new AccountInsufficientBalanceException(0);
        if (balance - coins < 0.1m) {
            throw new AccountInsufficientBalanceException(balance);
        }

        try {
            return await SendTransaction(address, coins.CoinsToNano(), false, allBalance, cancellationToken);
        } catch (EverClientException exception) when (exception.Code == (uint)TvmErrorCode.LowBalance) {
            var balanceEx = await GetBalance(cancellationToken) ?? throw new AccountInsufficientBalanceException(0);
            throw new AccountInsufficientBalanceException(balanceEx);
        }
    }

    public async Task<EverWallet> Init(long userId, CancellationToken cancellationToken) {
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

    private async Task<(string transactionId, decimal totalOutputCoins)> SendTransaction(string dest, decimal nanoCoins, bool bounce,
        bool allBalance,
        CancellationToken cancellationToken) {
        if (_keyPair is null || _stateInitBoc is null || _address is null) {
            throw new NotInitializedException();
        }

        var value = $"{nanoCoins:0}";
        var callSet = new CallSet {
            FunctionName = "sendTransaction",
            Input = new {
                dest,
                value,
                bounce,
                flags = allBalance ? 130 : 1,
                payload = "te6ccgEBAQEAAgAAAA=="
            }.ToJsonElement()
        };

        var bodyBoc = (await _everClient.Abi.EncodeMessageBody(new ParamsOfEncodeMessageBody {
            Address = _address,
            Abi = Abi,
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
            Message = callMessage
        }, cancellationToken: cancellationToken)).ShardBlockId;

        var resultOfProcessMessage = await _everClient.Processing.WaitForTransaction(new ParamsOfWaitForTransaction {
            Message = callMessage,
            ShardBlockId = shardBlockId
        }, cancellationToken: cancellationToken);

        var transactionId = resultOfProcessMessage.Transaction!.Get<string>("id");
        var totalOutputCoins = resultOfProcessMessage.Fees.TotalOutput.NanoToCoins();

        return (transactionId, totalOutputCoins);
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
                        userId.ToBuilderOp(),
                        keyPair.Secret.ToBuilderOp()
                    }
                }, cancellationToken)).Boc;
                var userHash = (await _everClient.Boc.GetBocHash(new ParamsOfGetBocHash {
                    Boc = userBoc
                }, cancellationToken)).Hash;
                var dataBoc = (await _everClient.Boc.EncodeBoc(new ParamsOfEncodeBoc {
                    Builder = new[] {
                        $"x{keyPair.Public}".ToBuilderOp(),
                        0L.ToBuilderOp(),
                        userHash.ToBuilderOp()
                    }
                }, cancellationToken)).Boc;
                var stateInitBoc = (await _everClient.Boc.EncodeBoc(new ParamsOfEncodeBoc {
                    Builder = new[] {
                        false.ToBuilderOp(),
                        false.ToBuilderOp(),
                        true.ToBuilderOp(),
                        true.ToBuilderOp(),
                        false.ToBuilderOp(),
                        WalletContractCodeBoc.ToBuilderOpCellBoc(),
                        dataBoc.ToBuilderOpCellBoc()
                    }
                }, cancellationToken)).Boc;
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
}
