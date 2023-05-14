namespace EidolonicBot.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class CommandArgAttribute : Attribute {
    public CommandArgAttribute(string name, string description, params string[] dependsOn) {
        Name = name;
        Description = description;
        DependsOn = dependsOn;
    }

    public string Name { get; }
    public string Description { get; }
    public string[] DependsOn { get; }
}
