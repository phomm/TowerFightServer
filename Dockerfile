#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 80
EXPOSE 443
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["TowerFight.API/TowerFight.API.csproj", "TowerFight.API/"]
COPY ["TowerFight.BuinessLogic/TowerFight.BusinessLogic.csproj", "TowerFight.BuinessLogic/"]
RUN dotnet restore "./TowerFight.API/./TowerFight.API.csproj"
COPY . .
WORKDIR "/src/TowerFight.API"
RUN dotnet build "./TowerFight.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TowerFight.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TowerFight.API.dll"]