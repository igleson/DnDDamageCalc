# Fly.io Deployment Quick Reference

## Prerequisites Checklist
- [ ] Fly.io account created
- [ ] `flyctl` CLI installed and authenticated
- [ ] Supabase project URL and keys ready
- [ ] Google OAuth configured in Supabase

## First-Time Deployment (5 minutes)

```bash
# 1. Set secrets
flyctl secrets set SUPABASE_URL=https://your-project.supabase.co
flyctl secrets set SUPABASE_ANON_KEY=your-anon-key
flyctl secrets set SUPABASE_SERVICE_KEY=your-service-key

# 2. Launch (creates app, no deploy yet)
flyctl launch --no-deploy

# 3. Deploy
flyctl deploy

# 4. Test
flyctl open
curl https://dnddamagecalc.fly.dev/health
```

## Update Google OAuth Redirect URIs
Add to Google Cloud Console → OAuth 2.0 Client:
```
https://dnddamagecalc.fly.dev/auth/callback
```

Add to Supabase → Authentication → URL Configuration:
```
https://dnddamagecalc.fly.dev/auth/callback
```

## Redeployment
```bash
flyctl deploy
```

## Common Commands

| Task | Command |
|------|---------|
| View logs | `flyctl logs` |
| Check status | `flyctl status` |
| Open app | `flyctl open` |
| SSH into container | `flyctl ssh console` |
| View secrets | `flyctl secrets list` |
| Set secret | `flyctl secrets set KEY=value` |
| Scale memory | `flyctl scale memory 512` |
| Rollback | `flyctl releases rollback` |
| Dashboard | `flyctl dashboard` |

## Health Check
```bash
curl https://dnddamagecalc.fly.dev/health
```

Expected response:
```json
{
  "status": "healthy",
  "timestamp": "2026-02-16T18:30:00Z",
  "checks": {
    "supabase": "ok"
  }
}
```

## Troubleshooting

### App won't start
```bash
flyctl logs
flyctl ssh console
```

### Secrets missing
```bash
flyctl secrets list
flyctl secrets set SUPABASE_URL=https://...
```

### OAuth not working
Check redirect URIs in Google Cloud Console and Supabase dashboard.

## Files Created
- `Dockerfile` - Native AOT multi-stage build
- `fly.toml` - Fly.io app configuration
- `.dockerignore` - Build exclusions
- `DEPLOYMENT.md` - Full deployment guide
- `.env.production` - Template for secrets (not committed)

## Resources
- Full guide: `DEPLOYMENT.md`
- Fly.io docs: https://fly.io/docs
- App dashboard: `flyctl dashboard`
