using System.Text.Json;

namespace DnDDamageCalc.Web.Auth;

public class AuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string? _testUserId;

    public AuthMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _testUserId = configuration["TestUserId"];
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        // Skip auth for login page, auth endpoints, and static files
        if (path == "/login" || path.StartsWith("/auth/") || path.Contains('.'))
        {
            await _next(context);
            return;
        }

        // Test bypass: if TestUserId is configured, skip real auth
        if (!string.IsNullOrEmpty(_testUserId))
        {
            context.Items["UserId"] = _testUserId;
            context.Items["AccessToken"] = "fake-test-token";
            await _next(context);
            return;
        }

        var cookie = SessionCookie.Read(context.Request);
        if (cookie is null)
        {
            Console.WriteLine($"[AUTH MW] No cookie found for {path} - redirecting to /login");
            RedirectToLogin(context);
            return;
        }

        // Check if token is expired (60s buffer)
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Console.WriteLine($"[AUTH MW] Cookie found for {path} - expires_at={cookie.ExpiresAt}, now={now}, expired={now >= cookie.ExpiresAt - 60}");
        if (now >= cookie.ExpiresAt - 60)
        {
            var refreshed = await SupabaseAuth.RefreshAccessToken(cookie.RefreshToken);
            if (refreshed is null)
            {
                SessionCookie.Clear(context.Response);
                RedirectToLogin(context);
                return;
            }

            SessionCookie.Set(context.Response, refreshed.AccessToken, refreshed.RefreshToken, refreshed.ExpiresIn);
            cookie = new CookiePayload
            {
                AccessToken = refreshed.AccessToken,
                RefreshToken = refreshed.RefreshToken,
                ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(refreshed.ExpiresIn).ToUnixTimeSeconds()
            };
        }

        var userId = JwtHelper.ExtractUserId(cookie.AccessToken);
        if (userId is null)
        {
            Console.WriteLine("[AUTH MW] JWT userId extraction FAILED - redirecting to /login");
            SessionCookie.Clear(context.Response);
            RedirectToLogin(context);
            return;
        }
        Console.WriteLine($"[AUTH MW] Authenticated user {userId} for {path}");

        context.Items["UserId"] = userId;
        context.Items["AccessToken"] = cookie.AccessToken;
        await _next(context);
    }

    private static void RedirectToLogin(HttpContext context)
    {
        if (context.Request.Headers.ContainsKey("HX-Request"))
        {
            context.Response.StatusCode = 401;
            context.Response.Headers["HX-Redirect"] = "/login";
        }
        else
        {
            context.Response.Redirect("/login");
        }
    }
}

public static class JwtHelper
{
    public static string? ExtractUserId(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length != 3) return null;

        try
        {
            var payload = parts[1];
            // Fix base64url padding
            payload = payload.Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var bytes = Convert.FromBase64String(payload);
            using var doc = JsonDocument.Parse(bytes);
            return doc.RootElement.GetProperty("sub").GetString();
        }
        catch
        {
            return null;
        }
    }
}

public static class AuthExtensions
{
    public static string GetUserId(this HttpContext context)
    {
        return context.Items["UserId"] as string ?? throw new InvalidOperationException("UserId not set");
    }

    public static string GetAccessToken(this HttpContext context)
    {
        return context.Items["AccessToken"] as string ?? throw new InvalidOperationException("AccessToken not set");
    }
}
