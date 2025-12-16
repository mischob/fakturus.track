# Fakturus Track - Time Tracker

A Blazor WebAssembly application for tracking daily work time, built with .NET 10.

## Features

- Start and stop work time tracking
- Edit start and stop times manually
- Multiple work sessions per day
- Offline-first storage using localStorage
- Background sync with backend API
- Mobile-optimized UI with Tailwind CSS

## Project Structure

- `Fakturus.Track.Backend` - ASP.NET Core API with FastEndpoints
- `Fakturus.Track.Frontend` - Blazor WebAssembly application

## Prerequisites

- .NET 10 SDK
- Node.js and npm (for Tailwind CSS)
- PostgreSQL (via Docker)

## Setup

### 1. Database Setup

Start PostgreSQL using Docker Compose:

```bash
docker-compose up -d
```

### 2. Backend Setup

1. Navigate to `Fakturus.Track.Backend`
2. Update `appsettings.Development.json` with your database connection string if needed
3. Run migrations (if using EF Core migrations):
   ```bash
   dotnet ef database update
   ```
   Or the database will be created automatically on first run

### 3. Frontend Setup

1. Navigate to `Fakturus.Track.Frontend`
2. Install npm dependencies:
   ```bash
   npm install
   ```
3. Build Tailwind CSS:
   ```bash
   npm run buildcss
   ```
   Or use watch mode during development:
   ```bash
   npm run dev
   ```

### 4. Configuration

Update `Fakturus.Track.Frontend/wwwroot/appsettings.json` with your API base URL:

```json
{
  "ApiSettings": {
    "BaseUrl": "https://localhost:7001"
  }
}
```

## Running the Application

### Backend

```bash
cd Fakturus.Track.Backend
dotnet run
```

The API will be available at `https://localhost:7001`

### Frontend

```bash
cd Fakturus.Track.Frontend
dotnet run
```

The application will be available at `https://localhost:7003` (or the port specified in launchSettings.json)

## API Endpoints

- `GET /v1/work-sessions` - Get all work sessions for the authenticated user
- `GET /v1/work-sessions/{id}` - Get a specific work session
- `POST /v1/work-sessions` - Create a new work session
- `PUT /v1/work-sessions/{id}` - Update a work session
- `DELETE /v1/work-sessions/{id}` - Delete a work session
- `POST /v1/work-sessions/sync` - Bulk sync work sessions from client

## Authentication

The application uses Azure AD B2C for authentication. Update the configuration in:
- Backend: `appsettings.json` / `appsettings.Development.json`
- Frontend: `wwwroot/appsettings.json`

## Development Notes

- All times are stored in UTC in the database
- Times are converted to local timezone for display
- Work sessions are stored locally first, then synced to the backend
- Background sync runs every 5 minutes when the app is active
- Manual sync is available via the Sync button

