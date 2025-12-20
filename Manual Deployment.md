# Manual Deployment Guide for Fakturus.Track

This guide describes the manual steps to deploy the Fakturus.Track Blazor WebAssembly application and Fakturus.Track API to a Hetzner server with Azure Key Vault integration for secrets management.

## Prerequisites

- **Server**: Hetzner server with IP `91.99.65.63`
- **Domain**: `track.fakturus.com` and `api.track.fakturus.com` registered with DNS provider
- **Container Registry**: `registry.fakturus.com` (running)
- **Traefik**: Already running with dashboard
- **Docker**: Installed on the server
- **Docker Compose**: Installed on the server
- **Azure Key Vault**: `https://fakturus.vault.azure.net/` with secrets configured
- **Azure Database for PostgreSQL**: External PostgreSQL service configured

## Server Setup

### 1. Connect to Your Hetzner Server

```bash
ssh root@91.99.65.63
```

### 2. Create Project Directory

```bash
mkdir -p /opt/fakturus-track
cd /opt/fakturus-track
```

### 3. Install Required Dependencies (if not already installed)

```bash
# Update system
apt update && apt upgrade -y

# Install Docker (if not installed)
curl -fsSL https://get.docker.com -o get-docker.sh
sh get-docker.sh

# Install Docker Compose (if not installed)
curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
chmod +x /usr/local/bin/docker-compose
```

## Azure Key Vault Setup

### 1. Create Service Principal for Key Vault Access

From your local machine with Azure CLI installed:

```bash
# Login to Azure
az login

# Create a service principal (replace with your subscription ID)
az ad sp create-for-rbac --name "fakturus-track-keyvault" --role contributor --scopes /subscriptions/{subscription-id}

# Note the output:
# - appId (Client ID)
# - password (Client Secret)
# - tenant (Tenant ID)
```

### 2. Grant Key Vault Access to Service Principal

You can use either Azure RBAC (Role-Based Access Control) or Access Policies. Azure RBAC is recommended for new deployments.

#### Option A: Using Azure RBAC (Recommended)

```bash
# Set variables (replace with actual values from previous step)
CLIENT_ID="<appId-from-previous-step>"
KEY_VAULT_NAME="fakturus"
RESOURCE_GROUP="fakturus"
SUBSCRIPTION_ID="891cf0fe-ec40-44d8-8c2d-6fbc8e4cf61d"

# Get the Key Vault resource ID
KEY_VAULT_ID="/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$KEY_VAULT_NAME"

# Grant Key Vault Secrets User role (allows get and list operations)
az role assignment create \
  --role "Key Vault Secrets User" \
  --assignee $CLIENT_ID \
  --scope $KEY_VAULT_ID

# Verify the role assignment
az role assignment list \
  --assignee $CLIENT_ID \
  --scope $KEY_VAULT_ID
```

#### Option B: Using Access Policies (Legacy)

```bash
# Set variables (replace with actual values from previous step)
CLIENT_ID="<appId-from-previous-step>"
KEY_VAULT_NAME="fakturus"
RESOURCE_GROUP="<your-resource-group>"

# Grant the service principal access to Key Vault using access policies
az keyvault set-policy --name $KEY_VAULT_NAME \
  --spn $CLIENT_ID \
  --secret-permissions get list
```

**Note**: If you're using Azure RBAC, make sure your Key Vault has "Azure role-based access control" enabled in the Access Policy settings. You can check/enable this in the Azure Portal under Key Vault → Access configuration → Permission model.

### 3. Store Secrets in Azure Key Vault

```bash
# Store PostgreSQL connection string
az keyvault secret set --vault-name fakturus \
  --name "TrackPostgresConnectionString" \
  --value "Host=<your-postgres-server>.postgres.database.azure.com;Port=5432;Database=Track;Username=<username>;Password=<password>;Ssl Mode=Require;"

# Alternative: Store as ConnectionStrings--DefaultConnection (if using that format)
az keyvault secret set --vault-name fakturus \
  --name "ConnectionStrings--DefaultConnection" \
  --value "Host=<your-postgres-server>.postgres.database.azure.com;Port=5432;Database=Track;Username=<username>;Password=<password>;Ssl Mode=Require;"
```

**Note**: In Azure Key Vault, use double dashes (`--`) to represent nested configuration sections. For example:
- `ConnectionStrings--DefaultConnection` maps to `ConnectionStrings:DefaultConnection` in appsettings.json
- `TrackPostgresConnectionString` maps directly to `TrackPostgresConnectionString` in configuration

## Build and Push Images

### 1. Build and Push API Image

From your local development machine:

```bash
# Navigate to the solution root
cd /c/Projects/Fakturus/fakturus.track

# Build the Docker image
docker build -f Fakturus.Track.Backend/Dockerfile -t registry.fakturus.com/fakturus-track-api:latest .

# Push to your registry
docker push registry.fakturus.com/fakturus-track-api:latest
```

### 2. Build and Push Blazor WebAssembly Image

```bash
# Build the Blazor WebAssembly image
docker build -f Fakturus.Track.Frontend/Dockerfile -t registry.fakturus.com/fakturus-track-ui:latest .

# Push to your registry
docker push registry.fakturus.com/fakturus-track-ui:latest
```

## Server Deployment

### 1. Create Environment File

On your Hetzner server, create `/opt/fakturus-track/.env`:

```env
# Azure Key Vault Authentication
AZURE_CLIENT_ID=<your-service-principal-client-id>
AZURE_CLIENT_SECRET=<your-service-principal-client-secret>
AZURE_TENANT_ID=<your-azure-tenant-id>
```

**Important**: Keep this file secure and never commit it to version control. Set appropriate permissions:

```bash
chmod 600 /opt/fakturus-track/.env
```

### 2. Create Docker Compose File

On your Hetzner server, create `/opt/fakturus-track/docker-compose.yml`:

```yaml
version: '3.8'

networks:
  proxy:
    external: true
  fakturus-internal:
    driver: bridge

services:
  fakturus-track-api:
    image: registry.fakturus.com/fakturus-track-api:latest
    container_name: fakturus-track-api
    restart: unless-stopped
    ports:
      - 8082:80
    networks:
      - proxy
      - fakturus-internal
    env_file:
      - .env
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:80
      - KeyVault__VaultUrl=https://fakturus.vault.azure.net/
      - AzureAdB2C__Instance=https://fakturus.b2clogin.com/
      - AzureAdB2C__Domain=fakturus.onmicrosoft.com
      - AzureAdB2C__TenantId=17c44991-367b-4d16-b818-1c268d2faed5
      - AzureAdB2C__ClientId=74fd0ed2-8865-4bad-b002-7d867ad8791a
      - AzureAdB2C__Audience=74fd0ed2-8865-4bad-b002-7d867ad8791a
      - AzureAdB2C__SignUpSignInPolicyId=B2C_1_fakt_sign_in
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.fakturus-track-api.rule=Host(`api.track.fakturus.com`)"
      - "traefik.http.routers.fakturus-track-api.entrypoints=https"
      - "traefik.http.routers.fakturus-track-api.tls=true"
      - "traefik.http.routers.fakturus-track-api.tls.certresolver=letsencrypt"
      - "traefik.http.services.fakturus-track-api.loadbalancer.server.port=80"
      - "traefik.docker.network=proxy"
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost/v1/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

  fakturus-track-ui:
    image: registry.fakturus.com/fakturus-track-ui:latest
    container_name: fakturus-track-ui
    restart: unless-stopped
    ports:
      - 8092:80
    networks:
      - proxy
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.fakturus-track-ui.rule=Host(`track.fakturus.com`)"
      - "traefik.http.routers.fakturus-track-ui.entrypoints=https"
      - "traefik.http.routers.fakturus-track-ui.tls=true"
      - "traefik.http.routers.fakturus-track-ui.tls.certresolver=letsencrypt"
      - "traefik.http.services.fakturus-track-ui.loadbalancer.server.port=80"
      - "traefik.docker.network=proxy"
```

### 3. DNS Configuration

Ensure your DNS records are properly configured:

```
A Record: track.fakturus.com -> 91.99.65.63
A Record: api.track.fakturus.com -> 91.99.65.63
```

### 4. Deploy the Application

```bash
# Navigate to the project directory
cd /opt/fakturus-track

# Pull the latest images
docker-compose pull

# Start the services
docker-compose up -d

# Check the status
docker-compose ps
```

### 5. Verify Deployment

```bash
# Check container logs
docker-compose logs -f fakturus-track-api
docker-compose logs -f fakturus-track-ui

# Check if containers are running
docker ps | grep fakturus-track

# Test API endpoint
curl -k https://api.track.fakturus.com/v1/health

# Test UI
curl -k https://track.fakturus.com
```

## Database Setup

### 1. Run Database Migrations

**Important**: Ensure PostgreSQL is accessible and connection string is configured in Key Vault.

The application automatically applies migrations on startup using `Database.Migrate()`. However, you can also run migrations manually:

```bash
# Execute migrations inside the API container
docker-compose exec fakturus-track-api dotnet ef database update

# Or if you need to run migrations manually
docker-compose exec fakturus-track-api /bin/bash
# Inside container:
# dotnet ef database update
```

**Note**: 
- Migrations are automatically applied when the container starts (configured in `Program.cs`)
- If migrations fail, the container will fail to start (fail-fast behavior)
- Ensure the `dotnet ef` tools are available in the container. The Dockerfile includes `Microsoft.EntityFrameworkCore.Tools` package.
- For Azure PostgreSQL, ensure the server firewall allows connections from the Hetzner server IP (91.99.65.63).
- If you need to run migrations manually, ensure the connection string in Key Vault is accessible from your local machine or use Azure Cloud Shell.

## Update Deployment

### 1. Update Application (Recommended: Use Deploy Script)

**Option A: Using the Deploy Script (Recommended)**

```bash
cd /opt/fakturus-track
./deploy.sh
```

This ensures atomic deployment with rolling updates.

**Option B: Manual Update (Not Recommended)**

⚠️ **Warning**: Manual updates can cause cache/version mismatch issues. Use the deploy script instead.

```bash
# Pull latest images
docker-compose pull

# WRONG: This can cause "mixed version" issues
# docker-compose down && docker-compose up -d

# CORRECT: Rolling update
docker-compose up -d --no-deps --force-recreate fakturus-track-api
sleep 15
docker-compose up -d --no-deps --force-recreate fakturus-track-ui

# Remove old images
docker image prune -f
```

**Why Rolling Updates Matter**:
- Blazor WASM uses content-hashed filenames (e.g., `System.Private.CoreLib.abc123.wasm`)
- If clients load during a non-atomic deployment, they may get:
  - Old `blazor.boot.json` pointing to old file names
  - New files on the server (old names return 404)
- Result: 404 errors on framework files, app won't load
- Solution: Update containers atomically, one at a time

### 2. Update Secrets in Key Vault

If you need to update secrets:

```bash
# Update PostgreSQL connection string
az keyvault secret set --vault-name fakturus \
  --name "TrackPostgresConnectionString" \
  --value "<new-connection-string>"

# Restart the API container to reload secrets
docker-compose restart fakturus-track-api
```

## Monitoring and Maintenance

### 1. View Logs

```bash
# View all logs
docker-compose logs -f

# View specific service logs
docker-compose logs -f fakturus-track-api
docker-compose logs -f fakturus-track-ui
```

### 2. Health Checks

The API includes a health check endpoint at `/v1/health`. Monitor this endpoint:

```bash
# Check API health
curl https://api.track.fakturus.com/v1/health

# Set up monitoring (example with cron)
# Add to crontab: */5 * * * * curl -f https://api.track.fakturus.com/v1/health || echo "API health check failed"
```

### 3. SSL Certificate Management

Traefik should automatically handle SSL certificates via Let's Encrypt. Monitor certificate status:

```bash
# Check Traefik logs for certificate issues
docker logs traefik | grep -i cert

# Verify SSL certificate
openssl s_client -connect track.fakturus.com:443 -servername track.fakturus.com
```

## Troubleshooting

### Common Issues

1. **Container won't start**:
   ```bash
   docker-compose logs [service-name]
   ```

2. **404 errors on `*.wasm` or `*.dll` files after deployment**:
   - **Symptom**: Browser console shows 404 errors for framework files like `System.Private.CoreLib.*.wasm`
   - **Cause**: Cache/version mismatch - browser cached old `blazor.boot.json` that references old file names
   - **Immediate Fix**: 
     ```bash
     # Clear browser cache and hard reload
     # Chrome/Edge: Ctrl+Shift+R (Windows) / Cmd+Shift+R (Mac)
     # Or: DevTools → Application → Clear storage → Clear site data
     ```
   - **Prevention**:
     - Always use the `deploy.sh` script for atomic deployment
     - Verify nginx cache headers are correct (see `CACHE_STRATEGY.md`)
     - Check that boot files have `Cache-Control: no-cache, no-store`
   - **Verification**:
     ```bash
     # Boot files should NOT be cached
     curl -I https://track.fakturus.com/_framework/blazor.boot.json | grep Cache-Control
     # Should return: Cache-Control: no-cache, no-store, must-revalidate
     
     # Framework files should be cached long-term
     curl -I https://track.fakturus.com/_framework/System.Private.CoreLib.*.wasm | grep Cache-Control
     # Should return: Cache-Control: public, max-age=31536000, immutable
     ```

3. **Changes not visible after deployment**:
   - **Symptom**: Deployed new version but users still see old version
   - **Cause**: `index.html` or boot files are cached
   - **Fix**: 
     - Verify nginx configuration has correct cache headers
     - Users must hard reload (Ctrl+Shift+R)
     - Check nginx logs to ensure new files are being served
   - **Prevention**: Ensure `index.html` has `Cache-Control: no-cache, no-store`

4. **Key Vault authentication fails**:
   - Verify service principal credentials in `.env` file
   - **Check RBAC role assignments** (if using Azure RBAC):
     ```bash
     CLIENT_ID="<your-client-id>"
     KEY_VAULT_ID="/subscriptions/891cf0fe-ec40-44d8-8c2d-6fbc8e4cf61d/resourceGroups/fakturus/providers/Microsoft.KeyVault/vaults/fakturus"
     az role assignment list --assignee $CLIENT_ID --scope $KEY_VAULT_ID
     ```
   - **Grant Key Vault Secrets User role** if missing:
     ```bash
     az role assignment create \
       --role "Key Vault Secrets User" \
       --assignee $CLIENT_ID \
       --scope $KEY_VAULT_ID
     ```
   - **Check Access Policies** (if using legacy access policies):
     ```bash
     az keyvault show --name fakturus --query properties.accessPolicies
     ```
   - Verify Key Vault permission model:
     ```bash
     az keyvault show --name fakturus --query properties.enableRbacAuthorization
     ```
     - If `true`, use Azure RBAC (Option A above)
     - If `false`, use Access Policies (Option B above)
   - Verify secrets exist: `az keyvault secret list --vault-name fakturus`
   - Wait a few minutes after granting permissions for propagation

5. **Database connection issues**:
   - Verify connection string in Key Vault: `az keyvault secret show --vault-name fakturus --name "TrackPostgresConnectionString"`
   - Check Azure PostgreSQL firewall rules allow Hetzner server IP (91.99.65.63)
   - Test connection from server: `psql -h <postgres-server> -U <username> -d Track`
   - Verify SSL is enabled in connection string: `Ssl Mode=Require;`

6. **Missing PostgreSQL client libraries**:
   - **Error**: `Cannot load library libgssapi_krb5.so.2`
   - **Solution**: Rebuild the Docker image with the updated Dockerfile that includes required libraries
   - The Dockerfile has been updated to install `libgssapi-krb5-2`, `libkrb5-3`, and `libssl3`

7. **API not accessible**:
   - Verify Traefik routing rules
   - Check if API container is healthy: `docker-compose ps`
   - Test internal connectivity: `docker-compose exec fakturus-track-ui curl http://fakturus-track-api`

8. **SSL certificate issues**:
   - Check Traefik configuration
   - Verify DNS propagation: `nslookup track.fakturus.com`
   - Check Let's Encrypt rate limits

9. **CORS errors**:
   - Verify CORS configuration in `Program.cs` includes `https://track.fakturus.com`
   - Check browser console for specific CORS error messages
   - Verify API is accessible from the frontend domain

## Security Considerations

1. **Firewall Configuration**:
   ```bash
   # Allow only necessary ports
   ufw allow 22    # SSH
   ufw allow 80    # HTTP (Traefik)
   ufw allow 443   # HTTPS (Traefik)
   ufw enable
   ```

2. **Container Registry Authentication**:
   ```bash
   # If your registry requires authentication
   docker login registry.fakturus.com
   ```

3. **Environment File Security**:
   - Keep `.env` file secure with `chmod 600`
   - Never commit `.env` to version control
   - Rotate service principal credentials regularly

4. **Key Vault Security**:
   - Use least privilege principle for Key Vault access
   - Regularly rotate secrets
   - Monitor Key Vault access logs

5. **Regular Updates**:
   - Keep Docker and Docker Compose updated
   - Regularly update base images
   - Monitor security advisories for .NET and nginx

## Automation Scripts

### Deploy Script (Atomic Deployment)

Create `/opt/fakturus-track/deploy.sh`:

```bash
#!/bin/bash
set -e

echo "Starting Fakturus.Track deployment..."

# Set APP_VERSION from git commit hash
export APP_VERSION=$(git rev-parse --short HEAD 2>/dev/null || echo "1.0.0")
echo "Deploying version: $APP_VERSION"

# Pull latest images
echo "Pulling latest Docker images..."
docker-compose pull

# IMPORTANT: Use rolling update strategy to avoid downtime and cache issues
# Stop and start services one by one to ensure atomic deployment
echo "Performing rolling update..."

# Update API first (backend can handle brief downtime)
docker-compose up -d --no-deps --force-recreate fakturus-track-api

# Wait for API to be healthy
echo "Waiting for API to be ready..."
sleep 15

# Update UI (this is critical - must be atomic)
docker-compose up -d --no-deps --force-recreate fakturus-track-ui

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
```

Make it executable:
```bash
chmod +x /opt/fakturus-track/deploy.sh
```

**Important Notes on Atomic Deployment**:

1. **Rolling Updates**: The script uses `--no-deps --force-recreate` to update services one at a time, ensuring:
   - No "mixed version" state where some files are old and some are new
   - Containers are recreated atomically
   - Old containers stay running until new ones are ready

2. **Why Not `docker-compose down && up`**: 
   - `down` stops all services simultaneously
   - During the gap, clients might load partial old/new file combinations
   - This causes 404 errors on hashed framework files (`.wasm`, `.dll`)

3. **Service Order**:
   - API first: Backend changes are usually backward-compatible
   - UI second: Frontend must match the framework files exactly

4. **Cache Invalidation**: 
   - The nginx configuration ensures boot files (`blazor.boot.json`) are never cached
   - Framework files with content hashes can be cached indefinitely
   - See `CACHE_STRATEGY.md` for detailed caching strategy

## Notes

- Replace `registry.fakturus.com` with your actual registry URL if different
- Adjust volume paths and environment variables according to your specific needs
- PostgreSQL runs on Azure Database for PostgreSQL (external service)
- Secrets are managed through Azure Key Vault
- Monitor disk space usage, especially for logs
- Set up log rotation to prevent disk space issues
- Consider implementing automated backups for database
- For any issues during deployment, check the container logs and Traefik dashboard for routing information

## Blazor WASM Cache Strategy

**CRITICAL**: Blazor WebAssembly apps require special cache handling to avoid version mismatch issues.

### Key Points:

1. **Boot files must NEVER be cached**:
   - `index.html`
   - `/_framework/blazor.boot.json`
   - `/_framework/blazor.webassembly.js`
   - These files tell the browser which versioned files to load

2. **Framework files can be cached forever**:
   - `/_framework/*.wasm`, `*.dll`, `*.dat`
   - These have content hashes in their names (e.g., `System.Private.CoreLib.abc123.wasm`)
   - When content changes, filename changes → automatic cache busting

3. **Atomic deployment is essential**:
   - Always use rolling updates (`--no-deps --force-recreate`)
   - Never use `docker-compose down && up` (causes mixed version state)
   - Update services one at a time

4. **SPA fallback must not catch framework files**:
   - `/_framework/` paths must return 404 if file is missing
   - Never rewrite framework paths to `index.html`

For detailed information, see `CACHE_STRATEGY.md` in the project root.

## Azure AD B2C Configuration

### Blazor WebAssembly Configuration

Ensure your `wwwroot/appsettings.json` in the Blazor app contains:

```json
{
  "AzureAdB2C": {
    "Authority": "https://fakturus.b2clogin.com/fakturus.onmicrosoft.com/B2C_1_fakt_sign_in",
    "ClientId": "3fb35bc6-8825-495e-b0a2-18e00352f968",
    "ValidateAuthority": false,
    "ApiScope": "https://fakturus.onmicrosoft.com/74fd0ed2-8865-4bad-b002-7d867ad8791a/access"
  },
  "ApiSettings": {
    "BaseUrl": "https://api.track.fakturus.com"
  }
}
```

### Azure B2C App Registration

Ensure your Azure B2C application registration includes:

- **Redirect URIs**: 
  - `https://track.fakturus.com/authentication/login-callback`
  - `https://track.fakturus.com/authentication/logout-callback`
- **Implicit grant**: Enable ID tokens and Access tokens
- **API permissions**: Configured for your API scope (`https://fakturus.onmicrosoft.com/74fd0ed2-8865-4bad-b002-7d867ad8791a/access`)

### CORS Configuration

Your API should be configured to allow CORS from your Blazor app. This is handled in `Program.cs`:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (builder.Environment.IsDevelopment())
            policy.WithOrigins("https://localhost:7086", "http://localhost:5138", "https://localhost:7003", "http://localhost:7003")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        else
            policy.WithOrigins("https://track.fakturus.com")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
    });
});
```

