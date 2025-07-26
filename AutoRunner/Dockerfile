FROM mcr.microsoft.com/playwright/dotnet:v1.53.0-noble

WORKDIR /app

COPY . .

RUN dotnet restore "AutoRunner/AutoRunner.csproj"
RUN dotnet build "AutoRunner/AutoRunner.csproj" -c Release
RUN dotnet publish "AutoRunner/AutoRunner.csproj" -c Release -o /app/publish /p:UseAppHost=false /p:PublishTrimmed=false

WORKDIR /app/publish

ENTRYPOINT ["dotnet", "AutoRunner.dll"]