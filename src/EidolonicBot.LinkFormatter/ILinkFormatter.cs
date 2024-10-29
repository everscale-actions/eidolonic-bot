namespace EidolonicBot;

public interface ILinkFormatter {
  string GetAddressLink(string address, bool @short = true, string? explorerName = default) {
    return GetAddressLink(
      address: address,
      label: @short
        ? string.Format((IFormatProvider?)ShortStringFormatProvider.Instance, "{0:6..4}", address)
        : address.ToEscapedMarkdownV2(),
      explorerName: explorerName
    );
  }

  string GetAddressLink(string address, string label, string? explorerName = default);
  string[] GetTransactionLinks(string transactionId);
}
