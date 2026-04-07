FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["Fakturus.Track.WebApp/Fakturus.Track.WebApp.csproj", "Fakturus.Track.WebApp/"]
RUN dotnet restore "Fakturus.Track.WebApp/Fakturus.Track.WebApp.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/Fakturus.Track.WebApp"
RUN dotnet build "Fakturus.Track.WebApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Fakturus.Track.WebApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app

RUN apt-get update && \
    apt-get install -y curl && \
    rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Fakturus.Track.WebApp.dll"]
