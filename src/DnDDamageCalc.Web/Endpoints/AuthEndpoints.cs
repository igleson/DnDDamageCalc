using DnDDamageCalc.Web.Auth;
using DnDDamageCalc.Web.Html;

namespace DnDDamageCalc.Web.Endpoints;

public static class AuthEndpoints
{
    private const string PkceVerifierCookie = ".DnDPkce";

    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapGet("/login", () => Results.Text(HtmlFragments.LoginPage(), "text/html"));

        app.MapGet("/auth/login", (HttpRequest request, HttpResponse response) =>
        {
            var scheme = request.Scheme;
            var host = request.Host;
            var redirectUri = $"{scheme}://{host}/auth/callback";

            var (verifier, challenge) = SupabaseAuth.GeneratePkce();

            response.Cookies.Append(PkceVerifierCookie, verifier, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromMinutes(10),
                Path = "/auth/callback"
            });

            var loginUrl = SupabaseAuth.GetGoogleLoginUrl(redirectUri, challenge);
            response.Redirect(loginUrl);
        });

        app.MapGet("/auth/callback", async (HttpRequest request, HttpResponse response) =>
        {
            var code = request.Query["code"].ToString();
            if (string.IsNullOrEmpty(code))
            {
                response.Redirect("/login");
                return;
            }

            var verifier = request.Cookies[PkceVerifierCookie] ?? "";
            if (string.IsNullOrEmpty(verifier))
            {
                response.Redirect("/login");
                return;
            }

            response.Cookies.Delete(PkceVerifierCookie, new CookieOptions { Path = "/auth/callback" });

            Console.WriteLine($"[AUTH] Exchanging code (length={code.Length}) with verifier (length={verifier.Length})");
            var tokens = await SupabaseAuth.ExchangeCodeForTokens(code, verifier);
            if (tokens is null)
            {
                Console.WriteLine("[AUTH] Token exchange FAILED - redirecting to /login");
                response.Redirect("/login");
                return;
            }

            Console.WriteLine($"[AUTH] Token exchange OK - access_token length={tokens.AccessToken.Length}, expires_in={tokens.ExpiresIn}");
            SessionCookie.Set(response, tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresIn);
            response.Redirect("/");
        });

        app.MapPost("/auth/logout", (HttpContext context) =>
        {
            var cookie = SessionCookie.Read(context.Request);
            if (cookie is not null)
            {
                // Fire and forget - don't block logout on Supabase API
                _ = SupabaseAuth.Logout(cookie.AccessToken);
            }
            SessionCookie.Clear(context.Response);
            context.Response.Redirect("/login");
        });
    }
}
