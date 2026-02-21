# VirtoCommerce Aspire AppHost

This .NET Aspire AppHost project orchestrates all components of the VirtoCommerce solution for local development.

## Components

The AppHost manages the following services:

### Infrastructure Services
  - **PostgreSQL 16** (7 databases):
  - `platform` - Main platform database
  - `catalog` - Catalog module database
  - `cart` - Shopping cart database
  - `orders` - Orders database
  - `notifications` - Notifications database
  - `hangfire` - Background jobs database
  - `auditlog` - Audit logging database
- **pgAdmin** - PostgreSQL administration and management tool on port 5050
- **Redis** - For caching and backend synchronization
- **RedisInsight** - Redis GUI tool for data visualization and management on port 5540
- **Elasticsearch 8** - Search provider with security enabled
- **Kibana** - Elasticsearch visualization and management UI on port 5601

### Application Services
- **Backend** (VirtoCommerce.Platform.Web) - API and admin portal on HTTPS port 8090
- **Frontend** (Vue3 storefront) - Customer-facing application on port 3000

## Prerequisites

- .NET 10.0 SDK
- Docker Desktop (for running containers)
- Node.js 22.12.0+ with Yarn 4.7.0 (for frontend)
- Frontend dependencies installed: Run `yarn install` in the `frontend` directory before starting the AppHost

## Backend Configuration Options

The AppHost supports two ways to run the VirtoCommerce Platform backend:

### Option 1: Build from Source (Default)

Uses the source code from `../vc-platform` directory. This is the default mode.

**When to use:**
- You're actively developing or modifying the platform code
- You need to debug into platform source code
- You want the latest changes from your local repository

**How to use:**
```powershell
# Default behavior - no configuration needed
cd aspire-host/VirtoCommerce.AppHost
dotnet run
```

### Option 2: Use Pre-built Version

Uses a pre-built version from `../vc-platform-release` directory (published output from GitHub releases).

**When to use:**
- You want to test against a stable, pre-built version
- You don't need to modify platform code
- Faster startup (no compilation needed)
- Testing integration without building platform

**How to use:**
```powershell
cd aspire-host/VirtoCommerce.AppHost
$env:UsePlatformFromSource="false"
dotnet run
```

Or use command line argument:
```powershell
dotnet run -- --no-source
```

### Setting Your Default Preference

You can create a `Directory.Build.props.user` file in the `aspire-host` directory to set your personal default:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project>
  <PropertyGroup>
    <UsePlatformFromSource>false</UsePlatformFromSource>
  </PropertyGroup>
</Project>
```

**Note:** Add `Directory.Build.props.user` to `.gitignore` to keep personal preferences local.

## Running the Application

1. **Configure secrets** (first time only):
   ```powershell
   cd aspire-host/VirtoCommerce.AppHost
   dotnet user-secrets init
   dotnet user-secrets set "Parameters:PostgresPassword" "your-secure-password"
   dotnet user-secrets set "Parameters:RedisPassword" "your-redis-password"
   dotnet user-secrets set "Parameters:ElasticPassword" "your-elastic-password"
   ```
   
   Note: Kibana uses the same `ElasticPassword` (elastic superuser) for development simplicity.

2. Run the AppHost:
   ```powershell
   # Using source (default)
   dotnet run
   
   # Or using pre-built
   $env:UsePlatformFromSource="false"
   dotnet run
   ```

3. The Aspire Dashboard will open automatically in your browser, showing all services and their status.

4. Access the applications:
   - Aspire Dashboard: https://localhost:17275
   - Backend API: https://localhost:8090
   - Frontend: https://localhost:3000
   - pgAdmin: http://localhost:5050 (PostgreSQL admin tool - auto-configured to connect to postgres)
   - RedisInsight: http://localhost:5540 (Redis GUI tool)
   - Kibana: http://localhost:5601 (Elasticsearch username: `elastic`, password from secrets)

## Configuration

### Secrets Management

**Important:** Passwords are NOT stored in `appsettings.json` to keep them out of the repository. You must configure secrets using one of the methods below.

#### Option 1: User Secrets (Recommended for Local Development)

User Secrets are stored locally and never committed to the repository.

**Initial setup (one-time):**
```powershell
cd aspire-host/VirtoCommerce.AppHost
dotnet user-secrets init
```

**Set secrets:**
```powershell
dotnet user-secrets set "Parameters:PostgresPassword" "your-secure-password"
dotnet user-secrets set "Parameters:RedisPassword" "your-redis-password"
dotnet user-secrets set "Parameters:ElasticPassword" "your-elastic-password"
```

**List secrets (to verify):**
```powershell
dotnet user-secrets list
```

#### Option 2: Environment Variables

Set environment variables before running:

```powershell
$env:Parameters__PostgresPassword="your-secure-password"
$env:Parameters__RedisPassword="your-redis-password"
$env:Parameters__ElasticPassword="your-elastic-password"
dotnet run
```

**Note:** Environment variables take precedence over User Secrets, so they're useful for CI/CD pipelines or temporary overrides.

### Parameters

The AppHost uses Aspire parameters for configuration. Non-sensitive parameters can be set in `appsettings.json`.

#### Platform Path
When using the pre-built backend option, the path to the release directory is configurable via the `PlatformPath` parameter in `appsettings.json` (default: `../vc-platform-release`).

You can override it via environment variable:
```powershell
$env:Parameters__PlatformPath="C:\path\to\vc-platform-release"
dotnet run -- --no-source
```

Or modify `appsettings.json`:
```json
{
  "Parameters": {
    "PlatformPath": {
      "value": "../vc-platform-release"
    }
  }
}
```

### Environment Variables
All connection strings and service endpoints are automatically injected by Aspire:
- Database connections use PostgreSQL provider
- Redis connection for caching and distributed locking
- Elasticsearch for search functionality
- Backend URL is automatically configured for the frontend

## Development Workflow

1. **Start Services**: Run `dotnet run` in the VirtoCommerce.AppHost directory
2. **View Logs**: Use the Aspire Dashboard to monitor logs from all services
3. **Stop Services**: Press Ctrl+C in the terminal running the AppHost

## Troubleshooting

### Containers not starting
- Ensure Docker Desktop is running
- Check Docker Desktop has enough resources allocated
- Verify ports 5432 (PostgreSQL), 5050 (pgAdmin), 6379 (Redis), 5540 (RedisInsight), 9200 (Elasticsearch), 5601 (Kibana), 8090 (Backend), and 3000 (Frontend) are available

### Missing secrets error
- Ensure you've configured secrets using User Secrets or environment variables
- Run `dotnet user-secrets list` to verify secrets are set
- Check that `Parameters:PostgresPassword`, `Parameters:RedisPassword`, and `Parameters:ElasticPassword` are configured
- Note: Kibana uses the same `ElasticPassword` (no separate password needed)

### Database connection issues
- Aspire automatically creates the databases on first run
- Connection strings are injected via environment variables
- Check the Aspire Dashboard for connection string values

### Frontend not connecting to backend
- Verify the backend is running (check Aspire Dashboard)
- The frontend automatically proxies API calls via Vite configuration
- Backend URL is set via `APP_BACKEND_URL` environment variable

### Backend build errors (source mode)
- Ensure `vc-platform` directory exists at the solution root
- Verify all platform dependencies are restored: `cd ../vc-platform && dotnet restore`
- Check that the platform solution builds successfully

### Backend not found (pre-built mode)
- Ensure the release directory exists (default: `../vc-platform-release` relative to AppHost)
- Verify it contains `VirtoCommerce.Platform.Web.dll` and `VirtoCommerce.Platform.Web.runtimeconfig.json`
- The pre-built version should be a complete published output from GitHub releases
- Check the `PlatformPath` parameter in `appsettings.json` if using a custom location
- The path is resolved relative to the AppHost directory, or use an absolute path

## Project Structure

```
aspire-host/
└── VirtoCommerce.AppHost/ # Main orchestration project
    ├── Program.cs         # Service configuration
    └── appsettings.json   # AppHost settings
```

Note: Aspire service defaults (OpenTelemetry, health checks, service discovery) are provided by the VirtoCommerce.AspireIntegration module in the `modules` directory.

## Notes

- All services are configured with data volumes for persistence
- OpenTelemetry is automatically configured for distributed tracing
- Health checks are enabled for all services
- The Aspire Dashboard provides real-time monitoring and logs

