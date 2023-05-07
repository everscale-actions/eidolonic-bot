using Telegram.Bot.Types;

namespace EidolonicBot.Notifications;

// ReSharper disable once ClassNeverInstantiated.Global
public record CommandNotification {
    public Command Command { get; init; }
    public string[] Arguments { get; init; } = Array.Empty<string>();
    public Message Message { get; init; } = null!;
}