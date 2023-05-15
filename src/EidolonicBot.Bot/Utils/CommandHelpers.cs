namespace EidolonicBot.Utils;

public static class CommandHelpers {
    public static readonly IReadOnlyDictionary<Command, CommandAttribute?> CommandAttributeByCommand =
        Enum.GetValues<Command>().ToDictionary(c => c, c => c.GetAttributeOfType<CommandAttribute>());

    public static readonly IReadOnlyDictionary<string, Command> CommandByText =
        CommandAttributeByCommand
            .Where(d => d.Value?.Text is not null)
            .ToDictionary(d => d.Value!.Text, d => d.Key);

    public static readonly IReadOnlyDictionary<Command, string?> HelpByCommand =
        Enum.GetValues<Command>().ToDictionary(c => c, c => {
            if (c.GetAttributeOfType<CommandAttribute>() is not { Description: { } description, Text: { } text }) {
                return null;
            }

            var argAttrs = c.GetAttributesOfType<CommandArgAttribute>();
            if (argAttrs.Length == 0) {
                return description;
            }

            var usages = argAttrs
                .Where(arg => arg.DependsOn.Length == 0)
                .Select(arg => $"{text} {GetSubArgs(argAttrs, arg.Name)}");

            var args = argAttrs
                .Select(a => $"   {a.Name} - {a.Description}");
            var help = description + '\n' +
                       "Usages:\n" +
                       string.Join('\n', usages) + "\n\n" +
                       string.Join('\n', args);
            return help;
        });

    // ReSharper disable once ParameterTypeCanBeEnumerable.Local
    private static string GetSubArgs(CommandArgAttribute[] args, string str) {
        var arg = args.SingleOrDefault(a => a.DependsOn.Contains(str))?.Name;
        return arg is null ? str : GetSubArgs(args, $"{str} {arg}");
    }
}
