namespace EidolonicBot;

public static class HostApplicationBuilderExtensions {
    public static HostApplicationBuilder AddDatabase(this HostApplicationBuilder builder) {
        var provider = builder.Configuration.GetValue("Provider", "Postgres");

        switch (provider) {
            case "Sqlite":
                builder.Services
                    .AddDbContext<AppDbContext, SqliteDbContext>(options =>
                        options.UseSqlite(builder.Configuration.GetConnectionString("Sqlite")));
                break;

            case "Postgres":
                builder.Services
                    .AddDbContext<AppDbContext, PostgresDbContext>(options =>
                        options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));
                break;

            default:
                throw new Exception($"Unsupported provider: {provider}");
        }

        return builder;
    }
}