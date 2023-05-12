namespace EidolonicBot.Events;

public record BotCommandReceived(Command Command, string[] Arguments, Message Message);
