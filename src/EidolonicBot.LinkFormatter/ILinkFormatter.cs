namespace EidolonicBot;

public interface ILinkFormatter {
    string GetAddressLink(string address, bool @short = true);
    string[] GetTransactionLinks(string transactionId);
}