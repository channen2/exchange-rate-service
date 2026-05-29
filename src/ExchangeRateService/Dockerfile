# Stage 1 - Base
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://0.0.0.0:8080

# Stage 2 - Development (dotnet watch)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dev
WORKDIR /workspace

CMD ["dotnet", "watch", "run", "--project", "ExchangeRateService/ExchangeRateService.csproj", "--urls=http://0.0.0.0:8080"]

# Stage 3 - Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj first (for caching)
COPY ["src/ExchangeRateService/ExchangeRateService.csproj", "src/ExchangeRateService/"]

RUN dotnet restore "src/ExchangeRateService/ExchangeRateService.csproj"

# Copy everything else
COPY . .

WORKDIR /src/src/ExchangeRateService
RUN dotnet build "ExchangeRateService.csproj" -c Release -o /app/build


# Stage 4 - Publish
FROM build AS publish
RUN dotnet publish "ExchangeRateService.csproj" \
  -c Release \
  -o /app/publish \
  /p:UseAppHost=false


# Stage 5 - Production Runtime
FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "ExchangeRateService.dll"]