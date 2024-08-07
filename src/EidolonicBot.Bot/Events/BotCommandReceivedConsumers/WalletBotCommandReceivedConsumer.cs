namespace EidolonicBot.Events.BotCommandReceivedConsumers;

public class WalletBotCommandReceivedConsumer(
  ITelegramBotClient botClient,
  IEverWallet wallet,
  IMemoryCache memoryCache
) : BotCommandReceivedConsumerBase(
  Command.Wallet,
  botClient,
  memoryCache) {
  private static string FormatInfoMessage(WalletInfo info) {
    return
      $"`{info.Address}`\n" +
      $"Balance {(info.Balance ?? 0).ToEvers()}";
  }


  protected override async Task<string?> ConsumeAndGetReply(string[] args, Message message, long chatId,
    int messageThreadId, bool isAdmin,
    CancellationToken cancellationToken) {
    var info = await wallet.GetInfo(cancellationToken);

    return FormatInfoMessage(info);
  }
}
