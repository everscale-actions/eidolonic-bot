namespace EidolonicBot;

public static class HostApplicationBuilderExtensions {
    public static HostApplicationBuilder AddGraphQLClient(this HostApplicationBuilder builder) {
        builder.Services.AddHttpClient("GraphQLClient")
            .ConfigureHttpClient((sp, client) =>
                client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<EverClientOptions>>().Value.Network.Endpoints[0]))
            .AddTypedClient<GraphQLClient>(client => new GraphQLClient(client));

        return builder;
    }
}
