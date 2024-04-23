# EF Core

## Migrations

### Restore Tools

```shell
cd src
dotnet tool restore
```

### Add migration

```shell
migration="AddThreadIdToSubscription"

cd src
dotnet dotnet-ef migrations add $migration -s EidolonicBot -p EidolonicBot.Database.Sqlite -- --provider Sqlite
dotnet dotnet-ef migrations add $migration -s EidolonicBot -p EidolonicBot.Database.Postgres -- --provider Postgres
```

### Remove last migration

```shell
cd src
dotnet dotnet-ef migrations remove -s EidolonicBot -p EidolonicBot.Database.Sqlite -- --provider Sqlite
dotnet dotnet-ef migrations remove -s EidolonicBot -p EidolonicBot.Database.Postgres -- --provider Postgres
```