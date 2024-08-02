namespace EidolonicBot.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class CommandArgAttribute(
  string name,
  string description,
  params string[] dependsOn
) : Attribute {
  public string Name { get; } = name;
  public string Description { get; } = description;
  public string[] DependsOn { get; } = dependsOn;
}
