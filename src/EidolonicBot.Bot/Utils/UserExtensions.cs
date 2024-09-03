namespace EidolonicBot.Utils;

public static class UserExtensions {
  private static string MentionMarkdown(ChatId userId, string name, ParseMode parseMode = ParseMode.Markdown) {
    var tgLink = $"tg://user?id={userId}";
    return parseMode == ParseMode.Markdown
      ? $"[{name}]({tgLink})"
      : $"[{StringExtensions.EscapeMarkdown(name, parseMode)}]({tgLink})";
  }

  public static string ToMentionMarkdownV2(this User user) {
    return MentionMarkdown(
      user.Id, user.Username is not null ? $"@{user.Username}" : $"{user.FirstName} {user.LastName}".Trim(),
      ParseMode.MarkdownV2);
  }
}
