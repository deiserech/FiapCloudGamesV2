FROM mcr.microsoft.com/dotnet/aspnet:8.0-noble AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0-noble AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/FiapCloudGames.Api/FiapCloudGames.Api.csproj", "src/FiapCloudGames.Api/"]
COPY ["src/FiapCloudGames.Application/FiapCloudGames.Application.csproj", "src/FiapCloudGames.Application/"]
COPY ["src/FiapCloudGames.Domain/FiapCloudGames.Domain.csproj", "src/FiapCloudGames.Domain/"]
COPY ["src/FiapCloudGames.Infrastructure/FiapCloudGames.Infrastructure.csproj", "src/FiapCloudGames.Infrastructure/"]
RUN dotnet restore "./src/FiapCloudGames.Api/FiapCloudGames.Api.csproj"
COPY . .
WORKDIR "/src/src/FiapCloudGames.Api"
RUN dotnet build "./FiapCloudGames.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./FiapCloudGames.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FiapCloudGames.Api.dll"]

EXPOSE 5000