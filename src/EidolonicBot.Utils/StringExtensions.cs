using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Telegram.Bot.Types.Enums;

namespace EidolonicBot;

public static class StringExtensions {
  private static ImmutableDictionary<(ParseMode parseMode, MessageEntityType? entityType), string> Escaped => new Dictionary<(ParseMode parseMode, MessageEntityType? entityType), string> {
    { (ParseMode.Markdown, null), Regex.Escape("""_*`[""") },
    { (ParseMode.MarkdownV2, MessageEntityType.Pre), Regex.Escape("""\`""") },
    { (ParseMode.MarkdownV2, MessageEntityType.Code), Regex.Escape("""\`""") },
    { (ParseMode.MarkdownV2, MessageEntityType.TextLink), Regex.Escape("""\)""") }, {
      (ParseMode.MarkdownV2, null),
      Regex.Escape(str: """\_*()~`>#+-=|{}.![]""")
        .Replace("]", "\\]", StringComparison.Ordinal)
        .Replace("-", "\\-", StringComparison.Ordinal)
    },
  }.ToImmutableDictionary();

  public static string EscapeMarkdown(
    string text,
    ParseMode parseMode = ParseMode.Markdown,
    MessageEntityType? entityType = default) {
    var escaped = (parseMode, entityType) switch {
      (ParseMode.Markdown, _) => Escaped[(ParseMode.Markdown, null)],
      (ParseMode.MarkdownV2, MessageEntityType.Pre) => Escaped[(ParseMode.MarkdownV2, MessageEntityType.Pre)],
      (ParseMode.MarkdownV2, MessageEntityType.Code) => Escaped[(ParseMode.MarkdownV2, MessageEntityType.Code)],
      (ParseMode.MarkdownV2, MessageEntityType.TextLink) => Escaped[(ParseMode.MarkdownV2, MessageEntityType.TextLink)],
      (ParseMode.MarkdownV2, _) => Escaped[(ParseMode.MarkdownV2, null)],
      _ => throw new ArgumentException("Only ParseMode.Markdown and ParseMode.MarkdownV2 allowed.", nameof(parseMode)),
    };

    return Regex.Replace(
      input: text,
      pattern: $"([{escaped}])",
      replacement: """\$1""",
      RegexOptions.CultureInvariant,
      matchTimeout: TimeSpan.FromSeconds(1));
  }

  public static string ToEscapedMarkdownV2(this string str) {
    return EscapeMarkdown(str, ParseMode.MarkdownV2);
  }
}
