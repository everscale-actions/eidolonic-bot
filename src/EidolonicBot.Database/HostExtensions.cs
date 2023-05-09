namespace EidolonicBot;

public static class HostExtensions {
    public static IHost MigrateDatabase(this IHost host) {
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
        return host;
    }
}