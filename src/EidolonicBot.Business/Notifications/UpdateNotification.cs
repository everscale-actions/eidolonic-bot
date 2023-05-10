using Telegram.Bot.Types;

namespace EidolonicBot.Notifications;

public record UpdateNotification {
    public Update Update { get; init; } = null!;
}
