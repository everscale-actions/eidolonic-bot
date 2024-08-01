namespace EidolonicBot;

public class ShortStringFormatProvider : IFormatProvider, ICustomFormatter
{
    public static readonly IFormatProvider Instance = new ShortStringFormatProvider();

    public string Format(string? format, object? arg, IFormatProvider? formatProvider)
    {
        return arg switch
        {
            null => throw new ArgumentNullException(nameof(arg)),
            string str =>
                format?.Split("..") switch
                {
                    [var startStr, var endStr]
                        when int.TryParse(startStr, out var start)
                             && int.TryParse(endStr, out var end)
                             && str.Length > start + end + 1
                        => ((FormattableString)$"{str[..start]}..{str[^end..]}").ToString(Instance),
                    _ => str
                },
            _ => arg.ToString()!
        };
    }

    public object? GetFormat(Type? formatType)
    {
        return formatType == typeof(ICustomFormatter) ? this : null;
    }
}
