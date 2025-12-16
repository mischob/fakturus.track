FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["Fakturus.Track.Backend/Fakturus.Track.Backend.csproj", "Fakturus.Track.Backend/"]
RUN dotnet restore "Fakturus.Track.Backend/Fakturus.Track.Backend.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/Fakturus.Track.Backend"
RUN dotnet build "Fakturus.Track.Backend.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Fakturus.Track.Backend.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app

# Install PostgreSQL client libraries required for Npgsql
RUN apt-get update && \
    apt-get install -y libgssapi-krb5-2 libkrb5-3 libssl3 curl && \
    rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Fakturus.Track.Backend.dll"]

