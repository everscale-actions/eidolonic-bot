using EidolonicBot.Configurations;
using Microsoft.Extensions.Options;

namespace EidolonicBot;

internal class LinkFormatter(
  IOptions<BlockchainOptions> blockchainOptionsAccessor
) : ILinkFormatter {
  private readonly BlockchainOptions _blockchainOptions = blockchainOptionsAccessor.Value;

  public string GetAddressLink(string address, string label, string? explorerName = default) {
    var explorer = _blockchainOptions.Explorers.Single(e => e.Name == (explorerName ?? _blockchainOptions.DefaultExplorer));
    var link = string.Format(explorer.AccountLinkTemplate, address);
    return string.Format($"[{label.ToEscapedMarkdownV2()}]({link})");
  }

  public string[] GetTransactionLinks(string transactionId) {
    return _blockchainOptions.Explorers
      .Where(e => !string.IsNullOrEmpty(e.TransactionLinkTemplate))
      .Select(e => $"[{e.Name.ToEscapedMarkdownV2()}]({string.Format(e.TransactionLinkTemplate, transactionId)})")
      .ToArray();
  }
}
