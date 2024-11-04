using System.Diagnostics.CodeAnalysis;

namespace EidolonicBot.Models;

[PrimaryKey(nameof(ChatId), nameof(MessageThreadId), nameof(SubscriptionId))]
[SuppressMessage("ReSharper", "NullableWarningSuppressionIsUsed")]
[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")]
public class SubscriptionByChat {
  public long ChatId { get; set; }
  public int MessageThreadId { get; set; }
  public decimal MinDelta { get; set; }
  public Guid SubscriptionId { get; set; }
  public Subscription Subscription { get; set; } = null!;

  [MaxLength(1000)] public string? Label { get; set; }
}
