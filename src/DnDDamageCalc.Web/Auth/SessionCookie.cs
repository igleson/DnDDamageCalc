using Microsoft.AspNetCore.DataProtection;

namespace DnDDamageCalc.Web.Auth;

public static class SessionCookie
{
    private const string CookieName = ".DnDAuth";
    private static IDataProtector _protector = null!;
    private static bool _secureCookies;

    public static void Configure(IDataProtectionProvider provider, bool secureCookies)
    {
        _protector = provider.CreateProtector("DnDDamageCalc.Auth");
        _secureCookies = secureCookies;
    }

    public static void Set(HttpResponse response, string accessToken, string refreshToken, int expiresIn)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn).ToUnixTimeSeconds();
        var payload = $"{accessToken}|{refreshToken}|{expiresAt}";
        var encrypted = _protector.Protect(payload);

        response.Cookies.Append(CookieName, encrypted, new CookieOptions
        {
            HttpOnly = true,
            Secure = _secureCookies,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromDays(7),
            Path = "/"
        });
    }

    public static CookiePayload? Read(HttpRequest request)
    {
        if (!request.Cookies.TryGetValue(CookieName, out var encrypted) || string.IsNullOrEmpty(encrypted))
            return null;

        try
        {
            var payload = _protector.Unprotect(encrypted);
            var parts = payload.Split('|', 3);
            if (parts.Length != 3) return null;

            return new CookiePayload
            {
                AccessToken = parts[0],
                RefreshToken = parts[1],
                ExpiresAt = long.Parse(parts[2])
            };
        }
        catch
        {
            return null;
        }
    }

    public static void Clear(HttpResponse response)
    {
        response.Cookies.Delete(CookieName, new CookieOptions
        {
            Path = "/"
        });
    }
}

public class CookiePayload
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public long ExpiresAt { get; set; }
}
