namespace EidolonicBot.Events;

public record SubscriptionReceived(string TransactionId, string AccountAddr, decimal BalanceDelta, string Сounterparty, decimal Balance);
