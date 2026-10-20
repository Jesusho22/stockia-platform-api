# StockIA Platform API — multi-stage build (context: repository root).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY DataBit.StockIA.Platform/DataBit.StockIA.Platform.csproj DataBit.StockIA.Platform/
RUN dotnet restore DataBit.StockIA.Platform/DataBit.StockIA.Platform.csproj
COPY DataBit.StockIA.Platform/ DataBit.StockIA.Platform/
COPY ml/models/ ml/models/
RUN dotnet publish DataBit.StockIA.Platform/DataBit.StockIA.Platform.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
# tzdata: America/Lima for "today" (expiration dates, sales of the day, forecasts).
RUN apt-get update && apt-get install -y --no-install-recommends tzdata && rm -rf /var/lib/apt/lists/*
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    TZ=America/Lima
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "DataBit.StockIA.Platform.dll"]
