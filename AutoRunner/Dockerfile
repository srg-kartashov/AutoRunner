FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Install Chrome and chromedriver
RUN apt-get update && apt-get install -y \
    wget \
    curl \
    unzip \
    gnupg \
    libglib2.0-0 \
    libnss3 \
    libgconf-2-4 \
    libfontconfig1 \
    libxss1 \
    libasound2 \
    fonts-liberation \
    libappindicator3-1 \
    xdg-utils \
    chromium-driver \
    && rm -rf /var/lib/apt/lists/*

RUN wget -q -O - https://dl.google.com/linux/linux_signing_key.pub | apt-key add - \
 && echo "deb [arch=amd64] http://dl.google.com/linux/chrome/deb/ stable main" > /etc/apt/sources.list.d/google-chrome.list \
 && apt-get update \
 && apt-get install -y google-chrome-stable \
 && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Копируем только проектный файл (из текущей папки контекста)
COPY ["AutoRunner.csproj", "./"]

# Восстанавливаем зависимости
RUN dotnet restore "./AutoRunner.csproj"

# Копируем весь исходный код (из текущей папки)
COPY . .

# Собираем проект
RUN dotnet build "./AutoRunner.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release

# Публикуем (релизная сборка без app host)
RUN dotnet publish "./AutoRunner.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false /p:PublishTrimmed=false

FROM base AS final
WORKDIR /app

# Копируем опубликованное приложение
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "AutoRunner.dll"]