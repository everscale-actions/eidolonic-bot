using EidolonicBot.Configurations;
using Microsoft.Extensions.Options;

namespace EidolonicBot;

internal class LinkFormatter : ILinkFormatter {
    private readonly BlockchainOptions _blockchainOptions;

    public LinkFormatter(IOptions<BlockchainOptions> blockchainOptionsAccessor) {
        _blockchainOptions = blockchainOptionsAccessor.Value;
    }

    public string GetAddressLink(string address, bool @short = true) {
        var explorer = _blockchainOptions.Explorers.Single(e => e.Name == _blockchainOptions.DefaultExplorer);
        var link = string.Format(explorer.AccountLinkTemplate, address);
        if (@short) {
            address = string.Format((IFormatProvider?)ShortStringFormatProvider.Instance, "{0:6..4}", address);
        }

        return string.Format($"[{address}]({link})");
    }

    public string[] GetTransactionLinks(string transactionId) {
        return _blockchainOptions.Explorers
            .Select(e => $"[{e.Name}]({string.Format(e.TransactionLinkTemplate, transactionId)})")
            .ToArray();
    }
}
