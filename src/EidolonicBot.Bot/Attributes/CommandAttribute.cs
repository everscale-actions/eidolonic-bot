namespace EidolonicBot.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public class CommandAttribute(
  string text
) : Attribute {
  public string Text { get; } = text;
  public string? Description { get; init; }
  public bool IsWalletNeeded { get; init; }
  public bool IsBotInitCommand { get; init; }
}
