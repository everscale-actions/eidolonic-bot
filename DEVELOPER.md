# EF Core

## Docker container to dump DB from rider

```shell
docker run --rm -it --name psql -v $(echo -n ~$USER)/_backups/eidolonic-db/:$(echo -n ~$USER)/_backups/eidolonic-db/ postgres bash
```

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