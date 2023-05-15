namespace EidolonicBot.Events;

public record SubscriptionReceived(string TransactionId, string AccountAddr, decimal BalanceDelta, string? From, string[] To, decimal Balance);
