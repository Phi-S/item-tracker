# Database migration command
(item-tracker root folder)

dotnet ef migrations add InitialCreate -p src/api/infrastructure/infrastructure.csproj -o Database/Migrations
