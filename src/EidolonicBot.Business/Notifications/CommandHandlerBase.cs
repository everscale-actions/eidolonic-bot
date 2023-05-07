using EidolonicBot.Abstract;
using MassTransit;
using Telegram.Bot.Types;

namespace EidolonicBot.Notifications;

public abstract class CommandHandlerBase : IConsumer<CommandNotification>, IMediatorConsumer {
    public async Task Consume(ConsumeContext<CommandNotification> context) {
        if (!await Check(context.Message.Command, context.Message.Arguments, context.Message.Message, context.CancellationToken)) {
            return;
        }

        await Consume(context.Message.Command, context.Message.Arguments, context.Message.Message, context.CancellationToken);
    }

    protected abstract Task<bool> Check(Command command, string[]? args, Message message, CancellationToken cancellationToken);


    protected abstract Task Consume(Command command, string[]? args, Message message, CancellationToken cancellationToken);
}