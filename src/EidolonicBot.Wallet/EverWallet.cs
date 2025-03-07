using System.Numerics;
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

internal class EverWallet(
  IEverClient everClient,
  IOptions<EverWalletOptions> walletOptions,
  IMemoryCache memoryCache,
  ILogger<EverWallet> logger
)
  : IEverWallet {
  private const string WalletContractCodeBoc =
    "te6cckEBBgEA/AABFP8A9KQT9LzyyAsBAgEgAgMABNIwAubycdcBAcAA8nqDCNcY7UTQgwfXAdcLP8j4KM8WI88WyfkAA3HXAQHDAJqDB9cBURO68uBk3oBA1wGAINcBgCDXAVQWdfkQ8qj4I7vyeWa++COBBwiggQPoqFIgvLHydAIgghBM7mRsuuMPAcjL/8s/ye1UBAUAmDAC10zQ+kCDBtcBcdcBeNcB10z4AHCAEASqAhSxyMsFUAXPFlAD+gLLaSLQIc8xIddJoIQJuZgzcAHLAFjPFpcwcQHLABLM4skB+wAAPoIQFp4+EbqOEfgAApMg10qXeNcB1AL7AOjRkzLyPOI+zYS/";

  private const string WalletContractAbiJson =
    """{"ABI version":2,"version":"2.3","header":["pubkey","time","expire"],"functions":[{"name":"sendTransaction","inputs":[{"name":"dest","type":"address"},{"name":"value","type":"uint128"},{"name":"bounce","type":"bool"},{"name":"flags","type":"uint8"},{"name":"payload","type":"cell"}],"outputs":[]},{"name":"sendTransactionRaw","inputs":[{"name":"flags","type":"uint8"},{"name":"message","type":"cell"}],"outputs":[]}],"data":[],"events":[],"fields":[{"name":"_pubkey","type":"uint256"},{"name":"_timestamp","type":"uint64"}]}""";

  private const string TransferAbiJson =
    """{"ABI version":2,"functions":[{"name":"transfer","id":"0x00000000","inputs":[{"name":"comment","type":"bytes"}],"outputs":[]}],"events":[],"data":[]}""";

  private const string DataAbiParamsJson =
    """[{"name":"pubkey","type":"uint256"},{"name":"timestamp","type":"uint64"},{"name":"userHash","type":"uint256"}]""";

  private static readonly Abi WalletAbi = new Abi.Contract {
    Value = JsonSerializer.Deserialize<AbiContract>(
      WalletContractAbiJson,
      JsonOptionsProvider.JsonSerializerOptions)
  };

  private static readonly Abi TransferAbi = new Abi.Contract {
    Value = JsonSerializer.Deserialize<AbiContract>(TransferAbiJson, JsonOptionsProvider.JsonSerializerOptions)
  };

  private static readonly AbiParam[] DataAbiParams =
    JsonSerializer.Deserialize<AbiParam[]>(DataAbiParamsJson, JsonOptionsProvider.JsonSerializerOptions)
    ?? throw new InvalidOperationException();

  private string? _address;
  private KeyPair? _keyPair;
  private string? _stateInitBoc;

  public async Task<decimal?> GetBalance(CancellationToken cancellationToken) {
    var result = await everClient.Net.QueryCollection(
      new ParamsOfQueryCollection {
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
    var result = await everClient.Net.QueryCollection(
      new ParamsOfQueryCollection {
        Collection = "accounts",
        Filter = new { id = new { eq = Address } }.ToJsonElement(),
        Result = "acc_type",
        Limit = 1
      }, cancellationToken);

    return result.Result.Length == 1
      ? result.Result[0].Get<AccountType>("acc_type")
      : null;
  }

  public async Task<(string transactionId, decimal totalOutputCoins)> SendCoins(long userId, decimal coins,
    bool allBalance,
    CancellationToken cancellationToken) {
    if (_keyPair is null) {
      throw new NotInitializedException();
    }

    var destStateInitBoc = await GetStateInitBoc(userId, _keyPair, cancellationToken);
    var dest = await GetAddress(destStateInitBoc, cancellationToken);

    return await SendCoins(dest, coins, allBalance, null, cancellationToken);
  }

  public async Task<(string transactionId, decimal totalOutputCoins)> SendCoins(string address, decimal coins,
    bool allBalance,
    string? memo,
    CancellationToken cancellationToken) {
    var balance = await GetBalance(cancellationToken) ?? throw new AccountInsufficientBalanceException(0);
    if (balance - coins < 0.1m) {
      throw new AccountInsufficientBalanceException(balance);
    }

    try {
      var payload = memo is null ? null : await GetPayloadBodyByMemo(memo);
      return await SendTransaction(address, coins.CoinsToNano(), false, allBalance, payload, cancellationToken);
    }
    catch (EverClientException exception) when (exception.Code == (uint)TvmErrorCode.LowBalance) {
      var balanceEx = await GetBalance(cancellationToken) ?? throw new AccountInsufficientBalanceException(0);
      throw new AccountInsufficientBalanceException(balanceEx);
    }
  }

  public async Task<IEverWallet> Init(long userId, CancellationToken cancellationToken) {
    var phrase = walletOptions.Value.SeedPhrase;
    if (phrase is null or "YOUR_SEED_PHRASE_HERE") {
      throw new NullReferenceException("Wallet:SeedPhrase should be provided");
    }

    _keyPair ??= await GetKeyPair(phrase ?? throw new InvalidOperationException(), cancellationToken);
    _stateInitBoc ??= await GetStateInitBoc(userId, _keyPair, cancellationToken);
    _address ??= await GetAddress(_stateInitBoc, cancellationToken);
    return this;
  }


  public string Address => _address ?? throw new NotInitializedException();

  private async Task<string> GetPayloadBodyByMemo(string memo) {
    var result = await everClient.Abi.EncodeMessageBody(
      new ParamsOfEncodeMessageBody {
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

  private async Task<(string transactionId, decimal totalOutputCoins)> SendTransaction(string dest,
    BigInteger nanoCoins, bool bounce,
    bool allBalance,
    string? payload,
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
        payload = payload ?? "te6ccgEBAQEAAgAAAA=="
      }.ToJsonElement()
    };

    var bodyBoc = (await everClient.Abi.EncodeMessageBody(
      new ParamsOfEncodeMessageBody {
        Address = _address,
        Abi = WalletAbi,
        CallSet = callSet,
        Signer = new Signer.Keys { KeysAccessor = _keyPair }
      }, cancellationToken)).Body;

    var accountType = await GetAccountType(cancellationToken);

    var callMessage = (await everClient.Boc.EncodeExternalInMessage(
      new ParamsOfEncodeExternalInMessage {
        Dst = _address,
        Init = accountType is AccountType.Uninit ? _stateInitBoc : null,
        Body = bodyBoc
      }, cancellationToken)).Message;

    var shardBlockId = (await everClient.Processing.SendMessage(
      new ParamsOfSendMessage {
        Message = callMessage
      }, cancellationToken: cancellationToken)).ShardBlockId;

    var resultOfProcessMessage = await everClient.Processing.WaitForTransaction(
      new ParamsOfWaitForTransaction {
        Message = callMessage,
        ShardBlockId = shardBlockId
      }, cancellationToken: cancellationToken);

    var transactionId = resultOfProcessMessage?.Transaction?.Get<string>("id") ?? throw new InvalidOperationException();
    var totalOutputCoins = resultOfProcessMessage.Fees.TotalOutput.NanoToCoins();

    return (transactionId, totalOutputCoins);
  }

  private async Task<KeyPair> GetKeyPair(string seedPhrase, CancellationToken cancellationToken) {
    var keypair = await memoryCache.GetOrCreateAsync(
      $"KeyPairBySeedPhrase_{seedPhrase}", async entity => {
        logger.LogDebug("Getting keypair by seed phrase");

        var keyPair = await everClient.Crypto.MnemonicDeriveSignKeys(
          new ParamsOfMnemonicDeriveSignKeys {
            Phrase = seedPhrase
          }, cancellationToken);

        entity.Size = 1;
        return keyPair;
      });

    return keypair ?? throw new InvalidOperationException();
  }

  private async Task<string> GetStateInitBoc(long userId, KeyPair keyPair,
    CancellationToken cancellationToken) {
    var stateInit = await memoryCache.GetOrCreateAsync<string>(
      $"StateInitBocByUserIdByPublicKey_{userId}_{keyPair.Public}",
      async entity => {
        logger.LogDebug("Getting StateInit for {UserId}", userId);
        var userBoc = (await everClient.Boc.EncodeBoc(
          new ParamsOfEncodeBoc {
            Builder = [
              userId.ToBuilderOp(),
              keyPair.Secret.ToBuilderOp()
            ],
          }, cancellationToken)).Boc;

        var userHash = (await everClient.Boc.GetBocHash(
          new ParamsOfGetBocHash {
            Boc = userBoc
          }, cancellationToken)).Hash;

        var dataBoc = (await everClient.Abi.EncodeBoc(
          new ParamsOfAbiEncodeBoc {
            @params = DataAbiParams,
            Data = new {
              pubkey = $"0x{keyPair.Public}",
              timestamp = 0L,
              userHash = $"0x{userHash}"
            }.ToJsonElement()
          }, cancellationToken)).Boc;

        var stateInitBoc = (await everClient.Boc.EncodeStateInit(
          new ParamsOfEncodeStateInit {
            Code = WalletContractCodeBoc,
            Data = dataBoc
          }, cancellationToken)).StateInit;

        entity.Size = 1;
        return stateInitBoc;
      });

    return stateInit ?? throw new InvalidOperationException();
  }

  private async Task<string> GetAddress(string stateInitBoc, CancellationToken cancellationToken) {
    var address = await memoryCache.GetOrCreateAsync<string>(
      $"AddressByStateInitBoc_{stateInitBoc}",
      async entity => {
        logger.LogDebug("Getting Address for {StateInit}", stateInitBoc);
        var resultOfGetBocHash =
          await everClient.Boc.GetBocHash(new ParamsOfGetBocHash { Boc = stateInitBoc }, cancellationToken);

        entity.Size = 1;
        return $"0:{resultOfGetBocHash.Hash}";
      });

    return address ?? throw new InvalidOperationException();
  }
}
