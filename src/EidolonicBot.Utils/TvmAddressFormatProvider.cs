namespace EidolonicBot;

public class TvmAddressFormatProvider : IFormatProvider, ICustomFormatter {
    public string Format(string? format, object? arg, IFormatProvider? formatProvider) {
        return arg switch {
            null => throw new ArgumentNullException(nameof(arg)),
            string str when Regex.TvmAddressRegex().IsMatch(str) =>
                format switch {
                    "short" => ((FormattableString)$"{str[..6]}..{str[^4..]}").ToString(new TvmAddressFormatProvider()),
                    _ => str
                },
            _ => arg.ToString()!
        };
    }

    public object? GetFormat(Type? formatType) {
        return formatType == typeof(ICustomFormatter) ? this : null;
    }
}