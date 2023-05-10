using System.Text;
using EidolonicBot.Notifications.CommandConsumers.Base;
using Microsoft.Extensions.Caching.Memory;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace EidolonicBot.Notifications.CommandConsumers;

public class HelpCommandConsumer : CommandConsumerBase {
    public HelpCommandConsumer(ITelegramBotClient botClient, IMemoryCache memoryCache) : base(Command.Help, botClient, memoryCache) { }

    protected override Task<string?> Consume(string[] args, Message message, long chatId, bool isAdmin, CancellationToken cancellationToken) {
        var sb = new StringBuilder("Usage:\n");

        foreach (var (_, commandDescription) in
                 CommandHelpers.CommandAttributeByCommand.Where(c => c.Value is not null)) {
            sb.Append($"{commandDescription!.Text}\t- {commandDescription.Description}\n");
        }

        return Task.FromResult(sb.ToString().TrimEnd('\n'))!;
    }
}
