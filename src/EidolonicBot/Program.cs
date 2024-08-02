CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

var host = Host.CreateApplicationBuilder(args)
  .AddLogging()
  .AddMemoryCache()
  .AddDatabase()
  .AddMassTransit()
  .AddEverClient()
  .AddEverWallet()
  .AddTelegramBot()
  .AddLinkFormatter()
  .AddSubscriptions()
  .Build();

host
  .MigrateDatabase()
  .Run();
