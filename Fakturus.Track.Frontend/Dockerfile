FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Install Node.js and npm for Tailwind CSS build
RUN apt-get update && \
    apt-get install -y curl && \
    curl -fsSL https://deb.nodesource.com/setup_20.x | bash - && \
    apt-get install -y nodejs && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Copy csproj and restore as distinct layers
COPY ["Fakturus.Track.Frontend/Fakturus.Track.Frontend.csproj", "Fakturus.Track.Frontend/"]

RUN dotnet restore "Fakturus.Track.Frontend/Fakturus.Track.Frontend.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/Fakturus.Track.Frontend"
RUN dotnet build "Fakturus.Track.Frontend.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Fakturus.Track.Frontend/Fakturus.Track.Frontend.csproj" -c Release -o /app/publish

# Use nginx to serve the Blazor WebAssembly application
FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html

# Copy the published files to nginx
COPY --from=publish /app/publish/wwwroot /usr/share/nginx/html
COPY Fakturus.Track.Frontend/nginx.conf /etc/nginx/nginx.conf

EXPOSE 80
EXPOSE 443

ENTRYPOINT ["nginx", "-g", "daemon off;"]

