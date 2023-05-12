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
    .Build();

host
    .MigrateDatabase()
    .Run();
