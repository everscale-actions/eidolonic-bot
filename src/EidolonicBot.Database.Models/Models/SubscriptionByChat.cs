namespace EidolonicBot.Models;

[PrimaryKey(nameof(ChatId), nameof(SubscriptionId))]
public class SubscriptionByChat {
    public long ChatId { get; set; }

    public Guid SubscriptionId { get; set; }
    public Subscription Subscription { get; set; } = null!;
}
