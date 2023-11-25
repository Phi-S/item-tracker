FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/web/presentation/presentation.csproj", "src/web/presentation/"]
RUN dotnet restore "src/web/presentation/presentation.csproj"
COPY . .
WORKDIR "/src/src/web/presentation"
RUN dotnet build "presentation.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "presentation.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "presentation.dll"]
