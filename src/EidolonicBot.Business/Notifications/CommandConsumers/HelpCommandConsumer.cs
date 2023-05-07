using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace EidolonicBot.Notifications.CommandConsumers;

public class HelpCommandConsumer : CommandHandlerBase {
    private readonly ITelegramBotClient _botClient;

    public HelpCommandConsumer(ITelegramBotClient botClient) {
        _botClient = botClient;
    }

    protected override Task<bool> Check(Command command, string[]? args, Message message, CancellationToken cancellationToken) {
        return Task.FromResult(command == Command.Help);
    }

    protected override async Task Consume(Command command, string[]? args, Message message, CancellationToken cancellationToken) {
        var sb = new StringBuilder("Usage:\n");
        foreach (var (_, commandDescription) in
                 CommandHelpers.CommandAttributeByCommand.Where(c => c.Value is not null))
            sb.Append($"{commandDescription!.Text}\t- {commandDescription.Description}\n");
        var text = sb.ToString().TrimEnd('\n');

        await _botClient.SendTextMessageAsync(
            message.Chat.Id,
            text,
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken);
    }
}