# ----------------------
# Basis-Image für Laufzeit
# ----------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# ----------------------
# Build-Stage
# ----------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Nur Projektdatei kopieren und restore, damit Caches genutzt werden können
COPY ["NetMailArchiver.Web.csproj", "./"]
RUN dotnet restore "NetMailArchiver.Web.csproj"

# Restliche Dateien kopieren
COPY . .

# Build
RUN dotnet build "NetMailArchiver.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

# ----------------------
# Publish-Stage
# ----------------------
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "NetMailArchiver.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ----------------------
# Final-Image
# ----------------------
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NetMailArchiver.Web.dll"]
