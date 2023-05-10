CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

var host = Host.CreateApplicationBuilder(args)
    .AddLogging()
    .AddMemoryCache()
    .AddTelegramBot<BotUpdateHandler>()
    .AddEverClient()
    .AddEverWallet()
    .AddDatabase()
    .AddBusiness()
    .Build();

host
    .MigrateDatabase()
    .Run();
