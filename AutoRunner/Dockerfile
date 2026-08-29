FROM mcr.microsoft.com/playwright/dotnet:v1.61.0-noble

RUN curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
RUN bash /tmp/dotnet-install.sh --channel 10.0 --install-dir /usr/share/dotnet
RUN rm /tmp/dotnet-install.sh

WORKDIR /app

COPY . .

RUN dotnet restore "AutoRunner/AutoRunner.csproj"
RUN dotnet build "AutoRunner/AutoRunner.csproj" -c Release
RUN dotnet publish "AutoRunner/AutoRunner.csproj" -c Release -o /app/publish /p:UseAppHost=false /p:PublishTrimmed=false

WORKDIR /app/publish

ENTRYPOINT ["dotnet", "AutoRunner.dll"]
