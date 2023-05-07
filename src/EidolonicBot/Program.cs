using EidolonicBot;
using EidolonicBot.Services;

var host = Host.CreateApplicationBuilder(args)
    .AddLogging()
    .AddMemoryCache()
    .AddTelegramBot<BotUpdateHandler>()
    .AddEverClient()
    .AddEverWallet()
    .AddBusiness()
    .Build();

host.Run();