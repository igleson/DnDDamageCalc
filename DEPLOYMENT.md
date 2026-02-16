# Deploying to Fly.io

This guide covers deploying the DnD Damage Calculator to Fly.io with Native AOT compilation.

## Prerequisites

1. **Fly.io Account**: Sign up at [fly.io](https://fly.io)
2. **flyctl CLI**: Install the Fly.io command-line tool
3. **Supabase Project**: Running Supabase instance with the schema from `migrations/001_initial_schema.sql`
4. **Google OAuth**: Configured in Supabase Auth (see main README.md)

## Installing flyctl

### Windows (PowerShell)
```powershell
powershell -Command "iwr https://fly.io/install.ps1 -useb | iex"
```

### macOS/Linux
```bash
curl -L https://fly.io/install.sh | sh
```

### Verify Installation
```bash
flyctl version
flyctl auth login
```

## Initial Deployment

### Step 1: Configure Secrets

Set your Supabase credentials as Fly.io secrets (never commit these):

```bash
flyctl secrets set SUPABASE_URL=https://your-project-id.supabase.co
flyctl secrets set SUPABASE_ANON_KEY=your-anon-key-here
flyctl secrets set SUPABASE_SERVICE_KEY=your-service-role-key-here
```

**Find these values in:**
- Supabase Dashboard → Settings → API
  - **Project URL** → `SUPABASE_URL`
  - **anon public** key → `SUPABASE_ANON_KEY`
  - **service_role secret** key → `SUPABASE_SERVICE_KEY`

⚠️ **Security Warning**: `SUPABASE_SERVICE_KEY` bypasses Row Level Security. Keep it secret!

### Step 2: Launch the Application

```bash
# Navigate to project root
cd C:\Users\VTEX\repositories\DnDDamageCalc

# Launch (creates app and deploys)
flyctl launch --no-deploy

# Review the generated fly.toml (should already be configured)
# Make any adjustments if needed

# Deploy
flyctl deploy
```

During `flyctl launch`:
- Accept the app name `dnddamagecalc` (or choose your own)
- Confirm region: `iad` (US East)
- **Decline** PostgreSQL database (we use Supabase)
- **Decline** Redis (not needed)

### Step 3: Configure Google OAuth

After deployment, your app will be available at `https://dnddamagecalc.fly.dev`.

**Update Google Cloud Console:**
1. Go to [Google Cloud Console](https://console.cloud.google.com)
2. Navigate to **APIs & Services** → **Credentials**
3. Select your OAuth 2.0 Client ID
4. Add to **Authorized redirect URIs**:
   ```
   https://dnddamagecalc.fly.dev/auth/callback
   https://your-project-id.supabase.co/auth/v1/callback
   ```

**Update Supabase Configuration:**
1. Go to Supabase Dashboard → **Authentication** → **URL Configuration**
2. Add to **Redirect URLs**:
   ```
   https://dnddamagecalc.fly.dev/auth/callback
   ```

### Step 4: Verify Deployment

```bash
# Check application status
flyctl status

# View recent logs
flyctl logs

# Test health endpoint
curl https://dnddamagecalc.fly.dev/health

# Open in browser
flyctl open
```

Expected health check response:
```json
{
  "status": "healthy",
  "timestamp": "2026-02-16T18:30:00Z",
  "checks": {
    "supabase": "ok"
  }
}
```

### Step 5: Test Authentication Flow

1. Open `https://dnddamagecalc.fly.dev`
2. Click "Sign in with Google"
3. Complete OAuth flow
4. Verify you can create/edit/delete characters

## Redeployment

After making code changes:

```bash
# Build and deploy
flyctl deploy

# Monitor deployment
flyctl logs

# Check status
flyctl status
```

## Rollback

If a deployment fails or causes issues:

```bash
# List release history
flyctl releases

# Rollback to previous version
flyctl releases rollback <version>
```

## Monitoring & Debugging

### View Logs
```bash
# Real-time logs
flyctl logs

# Last 100 lines
flyctl logs --count 100

# Filter by severity
flyctl logs --level error
```

### Application Metrics
```bash
# View metrics dashboard
flyctl dashboard metrics
```

### SSH into Container
```bash
# Open shell in running container
flyctl ssh console

# Run health check manually
curl http://localhost:8080/health
```

### Scaling
```bash
# Scale to 2 instances
flyctl scale count 2

# Scale up memory (requires paid plan)
flyctl scale memory 512

# Scale down to 0 (free tier auto-stops anyway)
flyctl scale count 0
```

## Local Docker Testing

Test the Docker build locally before deploying:

```bash
# Build the image
docker build -t dnddamagecalc .

# Run locally
docker run -p 8080:8080 \
  -e SUPABASE_URL=https://your-project-id.supabase.co \
  -e SUPABASE_ANON_KEY=your-anon-key \
  -e SUPABASE_SERVICE_KEY=your-service-key \
  dnddamagecalc

# Test health endpoint
curl http://localhost:8080/health

# Test in browser
start http://localhost:8080
```

## Troubleshooting

### Deployment Fails: "Error: SUPABASE_URL must be set"

**Solution**: Set secrets before deploying
```bash
flyctl secrets set SUPABASE_URL=https://your-project.supabase.co
flyctl secrets set SUPABASE_ANON_KEY=your-key
flyctl secrets set SUPABASE_SERVICE_KEY=your-key
```

### Health Check Fails: "unhealthy" status

**Causes:**
1. Supabase credentials are incorrect
2. Supabase RLS policies blocking access
3. Network connectivity issues

**Debug:**
```bash
flyctl logs
flyctl ssh console
curl http://localhost:8080/health
```

### OAuth Redirect Errors

**Solution**: Verify redirect URIs in:
1. Google Cloud Console → OAuth 2.0 Client
2. Supabase Dashboard → Authentication → URL Configuration

Both must include `https://dnddamagecalc.fly.dev/auth/callback`

### Application Not Starting (OOMKilled)

**Cause**: 256MB RAM may be insufficient during startup

**Solutions:**
1. Check logs: `flyctl logs`
2. Temporarily scale up: `flyctl scale memory 512`
3. Native AOT binary should be ~20-30MB, so 256MB should suffice

### Slow Cold Starts

**Expected behavior** on free tier:
- App stops after inactivity
- First request wakes it up (~2-5 seconds)
- Native AOT helps: ~100ms startup vs ~2s for regular .NET

**Solution for faster startup:**
- Upgrade to `min_machines_running = 1` (requires paid plan)

## Environment Variables Reference

| Variable | Source | Description |
|----------|--------|-------------|
| `SUPABASE_URL` | Fly.io Secret | Supabase project URL |
| `SUPABASE_ANON_KEY` | Fly.io Secret | Public anon key for client-side auth |
| `SUPABASE_SERVICE_KEY` | Fly.io Secret | Server-side key (bypasses RLS) |
| `ASPNETCORE_ENVIRONMENT` | fly.toml | Set to "Production" |
| `ASPNETCORE_URLS` | Dockerfile | Set to `http://+:8080` |

## Deployment Checklist

- [ ] flyctl installed and authenticated
- [ ] Secrets configured (`SUPABASE_URL`, `SUPABASE_ANON_KEY`, `SUPABASE_SERVICE_KEY`)
- [ ] Google OAuth redirect URIs updated
- [ ] Supabase redirect URLs configured
- [ ] App deployed: `flyctl deploy`
- [ ] Health check passing: `curl https://dnddamagecalc.fly.dev/health`
- [ ] Authentication flow tested (Google sign-in works)
- [ ] Character CRUD operations working
- [ ] Damage calculation working

## Cost Considerations

**Free Tier Includes:**
- 3 shared-cpu-1x VMs with 256MB RAM each
- 160GB outbound data transfer
- Auto-stop/auto-start (no idle charges)

**Current Configuration:**
- 1 VM, 256MB RAM, shared CPU
- Auto-stops when idle → **$0/month**
- Only pays for active usage on free tier allowance

**To avoid charges:**
- Stay within free tier limits
- Monitor usage: `flyctl dashboard`
- Set up billing alerts in Fly.io dashboard

## Support & Resources

- **Fly.io Documentation**: https://fly.io/docs
- **Fly.io Community**: https://community.fly.io
- **App Logs**: `flyctl logs`
- **Status Page**: https://status.fly.io
- **Supabase Docs**: https://supabase.com/docs
