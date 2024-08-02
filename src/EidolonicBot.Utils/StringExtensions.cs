using Telegram.Bot.Extensions.Markup;
using Telegram.Bot.Types.Enums;

namespace EidolonicBot;

public static class StringExtensions {
  public static string ToEscapedMarkdownV2(this string str) {
    return Tools.EscapeMarkdown(str, ParseMode.MarkdownV2);
  }
}
