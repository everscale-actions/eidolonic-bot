namespace EidolonicBot.Models;

[PrimaryKey(nameof(ChatId), nameof(MessageThreadId), nameof(SubscriptionId))]
public class SubscriptionByChat {
    public long ChatId { get; set; }
    public int MessageThreadId { get; set; }
    public decimal MinDelta { get; set; }
    public Guid SubscriptionId { get; set; }
    public Subscription Subscription { get; set; } = null!;
    public string? Label { get; set; }
}
