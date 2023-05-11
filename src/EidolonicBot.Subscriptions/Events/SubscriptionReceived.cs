namespace EidolonicBot.Events;

public record SubscriptionReceived(string TransactionId, string AccountAddr, decimal BalanceChange);
