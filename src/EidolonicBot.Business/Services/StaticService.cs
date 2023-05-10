using Telegram.Bot;

namespace EidolonicBot.Services;

public class StaticService : IStaticService {
    private readonly ITelegramBotClient _botClient;
    private string? _botUsername;

    public StaticService(ITelegramBotClient botClient) {
        _botClient = botClient;
    }

    public async Task<string?> GetBotUsername(CancellationToken cancellationToken) {
        return _botUsername ??= (await _botClient.GetMeAsync(cancellationToken)).Username;
    }
}
