namespace EidolonicBot;

public interface ILinkFormatter {
  string GetAddressLink(string address, bool @short = true) {
    return GetAddressLink(
      address: address,
      label: @short
        ? string.Format((IFormatProvider?)ShortStringFormatProvider.Instance, "{0:6..4}", address)
        : address.ToEscapedMarkdownV2()
    );
  }

  string GetAddressLink(string address, string label);
  string[] GetTransactionLinks(string transactionId);
}
