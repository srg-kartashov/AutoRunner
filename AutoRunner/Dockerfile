# Базовый образ для рантайма
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Образ для сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Копируем весь исходный код
COPY . .


# Восстанавливаем зависимости
RUN dotnet restore "AutoRunner/AutoRunner.csproj"

# Собираем проект
RUN dotnet build "AutoRunner/AutoRunner.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Публикуем проект
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "AutoRunner/AutoRunner.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false /p:PublishTrimmed=false

# Устанавливаем Playwright CLI и браузеры
RUN dotnet tool install --global Microsoft.Playwright.CLI \
    && export PATH="$PATH:/root/.dotnet/tools" \
    && playwright install chromium --with-deps

# Финальный образ
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "AutoRunner.dll"]