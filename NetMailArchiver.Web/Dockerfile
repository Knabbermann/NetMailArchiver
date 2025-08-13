FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Projektdateien kopieren (alle Projekte)
COPY ["NetMailArchiver.Web/NetMailArchiver.Web.csproj", "NetMailArchiver.Web/"]
COPY ["NetMailArchiver.Controllers/NetMailArchiver.Services.csproj", "NetMailArchiver.Controllers/"]
COPY ["NetMailArchiver.DataAccess/NetMailArchiver.DataAccess.csproj", "NetMailArchiver.DataAccess/"]
RUN dotnet restore "NetMailArchiver.Web/NetMailArchiver.Web.csproj"

# Restliche Dateien kopieren
COPY . .

WORKDIR "/src/NetMailArchiver.Web"
RUN dotnet build "NetMailArchiver.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "NetMailArchiver.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NetMailArchiver.Web.dll"]
