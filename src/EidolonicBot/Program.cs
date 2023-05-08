using System.Globalization;
using EidolonicBot;
using EidolonicBot.Services;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

var host = Host.CreateApplicationBuilder(args)
    .AddLogging()
    .AddMemoryCache()
    .AddTelegramBot<BotUpdateHandler>()
    .AddEverClient()
    .AddEverWallet()
    .AddBusiness()
    .Build();

host.Run();