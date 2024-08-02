namespace EidolonicBot;

public static class HostApplicationBuilderExtensions {
  public static HostApplicationBuilder AddSubscriptions(this HostApplicationBuilder builder) {
    builder.Services.AddSingleton<SubscriptionService>()
      .AddSingleton<ISubscriptionService>(sp => sp.GetRequiredService<SubscriptionService>())
      .AddHostedService(sp => sp.GetRequiredService<SubscriptionService>());

    return builder;
  }
}
