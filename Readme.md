# Database migration command
(item-tracker root folder)

dotnet ef migrations add InitialCreate -p src/api/infrastructure/infrastructure.csproj -o Database/Migrations

docker run -d --name postgres-item-tracker -e POSTGRES_PASSWORD=123 -e POSTGRES_DB=item-tracker -p 5432:5432 --restart unless-stopped postgres:16.1

dotnet watch run --project src\web\presentation\presentation.csproj

