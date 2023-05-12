# EF Core

## Migrations

### Add migration

```shell
migration="AddThreadIdToSubscription"

cd src
dotnet ef migrations add $migration -s EidolonicBot -p EidolonicBot.Database.Sqlite -- --provider Sqlite
dotnet ef migrations add $migration -s EidolonicBot -p EidolonicBot.Database.Postgres -- --provider Postgres
```

### Remove last migration

```shell
cd src
dotnet ef migrations remove -s EidolonicBot -p EidolonicBot.Database.Sqlite -- --provider Sqlite
dotnet ef migrations remove -s EidolonicBot -p EidolonicBot.Database.Postgres -- --provider Postgres
```