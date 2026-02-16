# Supabase Migration Guide

This guide explains how to migrate your DnD Damage Calculator from local SQLite to Supabase PostgreSQL.

## Prerequisites

1. **Supabase Project**: Create a free project at [supabase.com](https://supabase.com)
2. **Git Repository**: Ensure you have the latest code pulled

## Step 1: Run the SQL Migration

1. Go to your Supabase dashboard
2. Navigate to **SQL Editor**
3. Open the file `migrations/001_initial_schema.sql` from this repository
4. Copy and paste the entire contents into the SQL editor
5. Click **Run** to execute the migration

This will create:
- `characters` table with JSONB data column
- Row Level Security (RLS) policies for user isolation
- Indexes for optimal query performance
- Automatic `updated_at` timestamp trigger

## Step 2: Configure Environment Variables

The application requires three environment variables to connect to Supabase:

```bash
SUPABASE_URL=https://your-project-id.supabase.co
SUPABASE_ANON_KEY=your-anon-key-here
SUPABASE_SERVICE_KEY=your-service-role-key-here
```

### Finding Your Credentials

1. Go to your Supabase project dashboard
2. Click **Settings** → **API**
3. Copy the following:
   - **Project URL** → `SUPABASE_URL`
   - **Project API keys** → **anon public** → `SUPABASE_ANON_KEY`
   - **Project API keys** → **service_role secret** → `SUPABASE_SERVICE_KEY`

⚠️ **WARNING**: The `SUPABASE_SERVICE_KEY` bypasses Row Level Security. Never expose it to clients or commit it to version control.

### Setting Environment Variables

#### Windows (PowerShell)
```powershell
$env:SUPABASE_URL="https://your-project-id.supabase.co"
$env:SUPABASE_ANON_KEY="your-anon-key"
$env:SUPABASE_SERVICE_KEY="your-service-key"
```

#### Windows (Command Prompt)
```cmd
set SUPABASE_URL=https://your-project-id.supabase.co
set SUPABASE_ANON_KEY=your-anon-key
set SUPABASE_SERVICE_KEY=your-service-key
```

#### Linux/macOS
```bash
export SUPABASE_URL=https://your-project-id.supabase.co
export SUPABASE_ANON_KEY=your-anon-key
export SUPABASE_SERVICE_KEY=your-service-key
```

#### Using .env file (Recommended for local development)
```bash
# Copy the example file
cp .env.example .env

# Edit .env and fill in your actual values
# Never commit .env to version control (already in .gitignore)
```

## Step 3: Configure Google OAuth (For Authentication)

1. In Supabase dashboard, go to **Authentication** → **Providers**
2. Enable **Google** provider
3. Follow Supabase's instructions to:
   - Create a Google OAuth app
   - Add authorized redirect URIs
   - Configure client ID and secret in Supabase

## Step 4: Run the Application

```bash
dotnet run --project src/DnDDamageCalc.Web
```

The application will:
1. Connect to Supabase PostgreSQL
2. Redirect unauthenticated users to the login page
3. Authenticate users via Google OAuth
4. Store character data in your Supabase database

## Step 5: Verify

1. Open http://localhost:5082
2. Sign in with Google
3. Create a test character
4. Verify the data appears in Supabase:
   - Go to **Table Editor** → **characters**
   - You should see your character with JSONB data

## Data Migration (Optional)

If you have existing SQLite data to migrate:

### Export from SQLite
```sql
-- Connect to your local characters.db
SELECT Id, Name, Data FROM Characters;
```

### Manual Import
Unfortunately, the binary protobuf blob format is not directly compatible with JSONB. You'll need to:
1. Load characters in the old app
2. Manually recreate them in the new app

For bulk migration, consider writing a custom script that:
1. Reads SQLite protobuf blobs
2. Deserializes to C# objects
3. Serializes to JSON
4. Inserts into Supabase

## Architecture Changes

### Before (SQLite)
- Local `characters.db` file
- Protobuf binary blob storage
- No authentication required
- Static `CharacterRepository` class

### After (Supabase)
- PostgreSQL database in Supabase cloud
- JSONB native storage
- Google OAuth authentication required
- Dependency-injected `ICharacterRepository` interface
- Row Level Security for data isolation

### Storage Format Change

**Old (Protobuf blob)**:
```
Characters
├── Id (INTEGER)
├── Name (TEXT)
└── Data (BLOB - protobuf-serialized List<CharacterLevel>)
```

**New (JSONB)**:
```json
{
  "id": 1,
  "user_id": "uuid-from-google-oauth",
  "name": "Gandalf",
  "data": [
    {
      "levelNumber": 1,
      "attacks": [
        {
          "name": "Staff Strike",
          "hitPercent": 65,
          "critPercent": 5,
          "flatModifier": 3,
          "diceGroups": [{"quantity": 1, "dieSize": 8}]
        }
      ]
    }
  ]
}
```

## Troubleshooting

### Error: "SUPABASE_URL and SUPABASE_SERVICE_KEY environment variables must be set"
- Ensure environment variables are set before running the app
- Check for typos in variable names
- Verify the values are not empty strings

### Error: 401 Unauthorized
- RLS policies are working correctly
- You need to sign in with Google OAuth
- Check that Google provider is enabled in Supabase

### Error: Connection timeout
- Verify `SUPABASE_URL` is correct
- Check your internet connection
- Ensure Supabase project is active (not paused)

### Characters not appearing
- Verify you're signed in
- Check the `characters` table in Supabase Table Editor
- Ensure RLS policies allow your user ID

### Row Level Security Issues
- RLS policies match `user_id` to `auth.uid()`
- The `SUPABASE_SERVICE_KEY` bypasses RLS for server operations
- User sees only their own data automatically

## Testing

The test suite continues to use SQLite for fast, isolated tests:

```bash
dotnet test
```

Tests use:
- `SqliteCharacterRepository` (not Supabase)
- `CustomWebApplicationFactory` that injects SQLite repo
- Temporary in-memory database per test run
- No actual Supabase API calls

This keeps tests:
- Fast (no network calls)
- Isolated (no shared state)
- Reliable (no external dependencies)

## Security Notes

1. **Never commit** `SUPABASE_SERVICE_KEY` to version control
2. **Use HTTPS only** in production
3. **RLS policies** automatically isolate user data
4. **HTTP-only cookies** protect JWT tokens from XSS
5. **Google OAuth** provides secure authentication
6. **.env file** is in `.gitignore`

## Rollback (if needed)

To revert to SQLite locally for testing:

1. Checkout the previous commit before migration
2. Or manually set `TestUserId` environment variable to bypass auth
3. Tests always use SQLite regardless of production config

## Production Deployment

For production (e.g., Azure, AWS, Heroku):

1. Set environment variables in your hosting platform
2. Ensure `ASPNETCORE_ENVIRONMENT=Production`
3. Use secure cookies (automatic in production)
4. Consider setting up a custom domain for auth redirects
5. Monitor Supabase dashboard for usage and performance

## Support

- Supabase Docs: https://supabase.com/docs
- Supabase Discord: https://discord.supabase.com
- Project Issues: [GitHub Issues](https://github.com/your-repo/issues)
