FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

# Сборка и публикация .NET-приложения
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Копируем исходники
COPY . .

# Восстанавливаем зависимости
RUN dotnet restore "AutoRunner/AutoRunner.csproj"

# Собираем проект
RUN dotnet build "AutoRunner/AutoRunner.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Публикуем проект
FROM build AS publish
RUN dotnet publish "AutoRunner/AutoRunner.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false /p:PublishTrimmed=false

# Финальный образ — с готовым Chrome от Selenium
FROM selenium/standalone-chrome
USER root
WORKDIR /app

# Копируем опубликованное приложение
COPY --from=publish /app/publish .

# Меняем владельца (если нужно запускать от seluser)
RUN chown -R seluser:seluser /app

# Переключаемся на безопасного пользователя Selenium
USER seluser

# Запуск приложения
ENTRYPOINT ["dotnet", "AutoRunner.dll"]