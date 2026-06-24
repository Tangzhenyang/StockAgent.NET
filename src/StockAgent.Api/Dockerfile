FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY StockAgent.sln ./
COPY Directory.Build.props ./
COPY src/StockAgent.Api/StockAgent.Api.csproj src/StockAgent.Api/
COPY tests/StockAgent.Api.Tests/StockAgent.Api.Tests.csproj tests/StockAgent.Api.Tests/

RUN dotnet restore src/StockAgent.Api/StockAgent.Api.csproj

COPY src/StockAgent.Api/ src/StockAgent.Api/

RUN dotnet publish src/StockAgent.Api/StockAgent.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends chromium fonts-noto-cjk \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
ENV DataProtection__KeysPath=/var/stockagent/data-protection-keys
ENV PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH=/usr/bin/chromium

COPY --from=build /app/publish .

RUN mkdir -p /var/stockagent/data-protection-keys /app/wwwroot/pdf

EXPOSE 8080

ENTRYPOINT ["dotnet", "StockAgent.Api.dll"]
