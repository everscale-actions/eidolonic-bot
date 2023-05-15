CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

var host = Host.CreateApplicationBuilder(args)
    .AddLogging()
    .AddMemoryCache()
    .AddTelegramBot()
    .AddEverClient()
    .AddEverWallet()
    .AddDatabase()
    .AddMassTransit()
    .AddSubscriptions()
    .AddLinkFormatter()
    .Build();

host
    .MigrateDatabase()
    .Run();
