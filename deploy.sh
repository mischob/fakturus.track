#!/bin/bash
set -e

echo "Starting Fakturus.Track deployment..."

# Set APP_VERSION from git commit hash
export APP_VERSION=$(git rev-parse --short HEAD 2>/dev/null || echo "1.0.0")
echo "Deploying version: $APP_VERSION"

# Pull latest images
docker-compose -f docker-compose.prod.yml pull

# Stop services
docker-compose -f docker-compose.prod.yml down

# Start services
docker-compose -f docker-compose.prod.yml up -d

# Wait for services to be ready
sleep 30

# Check health
if curl -f https://api.track.fakturus.com/v1/health > /dev/null 2>&1; then
    echo "API is healthy"
else
    echo "API health check failed"
    exit 1
fi

if curl -f https://track.fakturus.com > /dev/null 2>&1; then
    echo "UI is accessible"
else
    echo "UI accessibility check failed"
    exit 1
fi

echo "Deployment completed successfully!"

