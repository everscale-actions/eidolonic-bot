using System.Diagnostics.CodeAnalysis;

namespace EidolonicBot.Models;

[PrimaryKey(nameof(ChatId), nameof(MessageThreadId), nameof(Address))]
[SuppressMessage("ReSharper", "NullableWarningSuppressionIsUsed")]
[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")]
public class LabelByChat {
  public long ChatId { get; set; }
  public int MessageThreadId { get; set; }

  [Required] [MaxLength(66)] public string Address { get; set; } = null!;

  [Required] [MaxLength(100)] public string Label { get; set; } = null!;
}
