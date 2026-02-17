# DnD Damage Calculator

A D&D Damage Calculator built with ASP.NET Minimal APIs, HTMX, and PicoCSS. Compiled with Native AOT for fast startup and low memory usage. Stores character data in Supabase PostgreSQL with Google OAuth authentication.

## Tech Stack

- **.NET 10** — Minimal APIs with `CreateSlimBuilder`
- **Native AOT** — Ahead-of-time compilation for native binaries
- **HTMX 2.0** — Dynamic HTML updates without JavaScript frameworks
- **PicoCSS v2** — Minimal CSS framework for clean styling
- **Supabase** — PostgreSQL database with authentication
- **Google OAuth** — Secure user authentication via Supabase Auth
- **xUnit** — Integration tests with `WebApplicationFactory`

## Features

- **Character Management**: Create, edit, and delete D&D characters with multiple levels
- **Encounter Settings**: Create per-user combat timelines (multiple combats, rounds, short-rest flags)
- **Damage Simulation**: Monte Carlo simulation (10,000 iterations) for damage statistics
- **Weapon Masteries**: Support for Vex and Topple masteries
- **Percentile Stats**: Average, P25, P50, P75, P90, P95 damage per level
- **User Isolation**: Row Level Security ensures users only see their own characters
- **HTMX Interface**: Fast, interactive UI without JavaScript frameworks
- **Native AOT**: Compiles to native binary for ~10x faster startup

## Setup

### Development Mode (SQLite, No Authentication)

For local development without Supabase:

```bash
# Set environment (PowerShell)
$env:ASPNETCORE_ENVIRONMENT="Development"

# Or (bash/zsh)
export ASPNETCORE_ENVIRONMENT=Development

# Run the app
dotnet run --project src/DnDDamageCalc.Web
```

Open http://localhost:5082 — characters save to `dev-characters.db` (gitignored).

**Features in dev mode:**
- ✅ SQLite database (no network calls)
- ✅ No authentication required (fixed "dev-user" ID)
- ✅ Full app functionality (create/edit/delete/simulate)
- ✅ Persistent storage across runs

### Production Mode (Supabase + Google OAuth)

### 1. Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Supabase Account](https://supabase.com) (free tier available)
- Google OAuth credentials (for authentication)

### 2. Database Setup

1. Create a new Supabase project at [supabase.com](https://supabase.com)
2. Run the SQL migration script:
   - Open Supabase dashboard → **SQL Editor**
   - Copy contents of `migrations/001_initial_schema.sql`
   - Execute the script

3. Enable Google authentication:
   - Go to **Authentication** → **Providers**
   - Enable **Google** provider
   - Follow Supabase instructions to configure OAuth

### 3. Environment Variables

Create a `.env` file or set environment variables:

```bash
SUPABASE_URL=https://your-project-id.supabase.co
SUPABASE_ANON_KEY=your-anon-key-here
SUPABASE_SERVICE_KEY=your-service-role-key-here
```

Find these values in: **Supabase Dashboard → Settings → API**

⚠️ **Never commit your `.env` file** (already in `.gitignore`)

### 4. Run the Application

```bash
# Clone
git clone https://github.com/igleson/DnDDamageCalc.git
cd DnDDamageCalc

# Set environment variables (see .env.example)
cp .env.example .env
# Edit .env with your Supabase credentials

# Set production environment
export ASPNETCORE_ENVIRONMENT=Production  # or omit (defaults to Production)

# Run with hot reload
dotnet watch --project src/DnDDamageCalc.Web

# Or run normally
dotnet run --project src/DnDDamageCalc.Web
```

Open http://localhost:5082 and sign in with Google.

## Deployment

Deploy to Fly.io with Native AOT compilation:

```bash
# Quick start (5 minutes)
flyctl secrets set SUPABASE_URL=https://your-project.supabase.co
flyctl secrets set SUPABASE_ANON_KEY=your-anon-key
flyctl secrets set SUPABASE_SERVICE_KEY=your-service-key
flyctl launch --no-deploy
flyctl deploy
```

**📖 Full deployment guide**: See [DEPLOYMENT.md](DEPLOYMENT.md) for complete instructions  
**⚡ Quick reference**: See [DEPLOY-QUICK.md](DEPLOY-QUICK.md) for common commands

## Development

### Hot Reload Development

For the best development experience with automatic reloading:

```bash
# Windows
.\run-dev.ps1

# macOS/Linux  
./run-dev.sh
```

This enables:
- **✅ .NET Hot Reload** - Code changes reload automatically
- **✅ Template Hot Reload** - Scriban template changes reload the page
- **✅ CSS Hot Reload** - Style changes reload the page  
- **✅ Browser Auto-Refresh** - No manual refresh needed

### Manual Development

```bash
# Run with hot reload manually
dotnet watch run --project src/DnDDamageCalc.Web

# Run tests (uses SQLite, not Supabase)
dotnet test

# Build
dotnet build

# Publish Native AOT binary
dotnet publish src/DnDDamageCalc.Web -c Release
```

## Project Structure

```
DnDDamageCalc/
├── migrations/
│   └── 001_initial_schema.sql      # Supabase database schema
├── src/DnDDamageCalc.Web/
│   ├── Auth/                       # Supabase auth services
│   ├── Data/                       # Repository layer
│   ├── Endpoints/                  # Minimal API routes
│   ├── Html/                       # HTMX fragments
│   ├── Models/                     # Domain models
│   ├── Simulation/                 # Damage calculation engine
│   ├── Program.cs                  # Application entry point
│   └── wwwroot/index.html          # PicoCSS + HTMX UI
└── tests/DnDDamageCalc.Tests/
    ├── CharacterEndpointTests.cs   # Integration tests
    ├── CharacterRepositoryTests.cs # Repository tests (SQLite)
    ├── DamageSimulatorTests.cs     # Simulation logic tests
    └── FormParserTests.cs          # Form parsing tests
```

## Architecture

- **No JavaScript frameworks** — HTMX handles all interactivity
- **No EF Core** — Direct SQL/HTTP for AOT compatibility
- **No Blazor** — Server-rendered HTML fragments
- **Dependency Injection** — `ICharacterRepository` abstraction
- **Row Level Security** — PostgreSQL RLS policies isolate user data
- **HTTP-only cookies** — Secure JWT storage
- **JSONB storage** — PostgreSQL-native format for nested data

## Testing

Tests use SQLite for speed and isolation (no Supabase calls):

```bash
dotnet test  # 59 tests, all fast and isolated
```

## Migration Guide

If migrating from an existing SQLite setup, see [MIGRATION.md](MIGRATION.md) for detailed instructions.

## Documentation

- [MIGRATION.md](MIGRATION.md) — Supabase migration guide
- [CLAUDE.md](CLAUDE.md) — Detailed architecture documentation

## Commands Reference

```bash
dotnet build                     # Build the solution
dotnet test                      # Run all 59 tests
dotnet run --project src/DnDDamageCalc.Web  # Run the app (http://localhost:5082)
dotnet publish -c Release        # AOT-compiled release build
```

## License

MIT License — See [LICENSE](LICENSE) for details

## Contributing

Pull requests welcome! Please ensure:
- All tests pass (`dotnet test`)
- Code builds with AOT (`dotnet publish -c Release`)
- HTMX patterns followed (no custom JavaScript)
- Row Level Security considerations for any database changes
