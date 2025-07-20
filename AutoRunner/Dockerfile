FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

USER root

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ./Clients ./Clients
COPY AutoRunner ./AutoRunner
WORKDIR /src/AutoRunner

# Восстанавливаем зависимости
RUN dotnet restore "AutoRunner/AutoRunner.csproj"

# Собираем проект
RUN dotnet build "AutoRunner/AutoRunner.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release

# Публикуем (релизная сборка без app host)
RUN dotnet publish "AutoRunner/AutoRunner.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false /p:PublishTrimmed=false

FROM base AS final
WORKDIR /app

# Копируем опубликованное приложение
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "AutoRunner.dll"]