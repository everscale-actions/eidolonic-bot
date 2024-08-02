using Telegram.Bot.Extensions.Markup;

namespace EidolonicBot.Utils;

public static class UserExtensions {
  public static string ToMentionMarkdownV2(this User user) {
    return Tools.MentionMarkdown(
      user.Id, user.Username is not null ? $"@{user.Username}" : $"{user.FirstName} {user.LastName}".Trim(),
      ParseMode.MarkdownV2);
  }
}
