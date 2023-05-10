using Telegram.Bot.Types;

namespace EidolonicBot.Utils;

public static class UserExtensions {
    public static string ToMentionString(this User user) {
        return user.Username is null
            ? $"[{user.FirstName}](tg://user?id={user.Id})"
            : $"@{user.Username}";
    }
}
