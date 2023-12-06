# Console commands:

all commands are executed from the item-tracker repo root folder

## Database migration command

```
dotnet ef migrations add Init -p src/api/infrastructure/infrastructure.csproj -o Database/Migrations
```

## Start postgres docker container

```
docker run -d --name postgres-item-tracker -e POSTGRES_PASSWORD=123 -e POSTGRES_DB=item-tracker -p 5432:5432 --restart unless-stopped postgres:16.1
```

## Start dotnet watch or blazor frontend

```
dotnet watch run --project src\web\presentation\presentation.csproj
```