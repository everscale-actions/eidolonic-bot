namespace EidolonicBot.Utils;

public static class CommandHelpers {
    public static readonly IReadOnlyDictionary<Command, CommandAttribute?> CommandAttributeByCommand =
        Enum.GetValues<Command>().ToDictionary(c => c, c => c.GetAttributeOfType<CommandAttribute>());

    public static readonly IReadOnlyDictionary<string, Command> CommandByText =
        CommandAttributeByCommand
            .Where(d => d.Value?.Text is not null)
            .ToDictionary(d => d.Value!.Text, d => d.Key);
}
