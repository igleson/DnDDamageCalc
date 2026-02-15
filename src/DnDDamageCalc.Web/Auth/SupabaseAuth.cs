using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DnDDamageCalc.Web.Auth;

public static class SupabaseAuth
{
    private static string _url = "";
    private static string _anonKey = "";
    private static readonly HttpClient Http = new();

    public static void Configure(string url, string anonKey)
    {
        _url = url.TrimEnd('/');
        _anonKey = anonKey;
    }

    public static (string verifier, string challenge) GeneratePkce()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        var verifier = Base64UrlEncode(bytes);
        var challengeBytes = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        var challenge = Base64UrlEncode(challengeBytes);
        return (verifier, challenge);
    }

    public static string GetGoogleLoginUrl(string redirectUri, string codeChallenge)
    {
        return $"{_url}/auth/v1/authorize?provider=google&redirect_to={Uri.EscapeDataString(redirectUri)}&flow_type=pkce&code_challenge={Uri.EscapeDataString(codeChallenge)}&code_challenge_method=S256";
    }

    public static async Task<AuthTokens?> ExchangeCodeForTokens(string code, string codeVerifier)
    {
        var body = $"{{\"auth_code\":\"{code}\",\"code_verifier\":\"{codeVerifier}\"}}";
        Console.WriteLine($"[AUTH] POST {_url}/auth/v1/token?grant_type=pkce");
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_url}/auth/v1/token?grant_type=pkce")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("apikey", _anonKey);

        var response = await Http.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[AUTH] Token exchange error: {(int)response.StatusCode} {response.StatusCode} - {json}");
            return null;
        }

        return JsonSerializer.Deserialize(json, AuthJsonContext.Default.AuthTokens);
    }

    public static async Task<AuthTokens?> RefreshAccessToken(string refreshToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_url}/auth/v1/token?grant_type=refresh_token")
        {
            Content = new StringContent($"{{\"refresh_token\":\"{refreshToken}\"}}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("apikey", _anonKey);

        var response = await Http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize(json, AuthJsonContext.Default.AuthTokens);
    }

    public static async Task Logout(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_url}/auth/v1/logout");
        request.Headers.Add("apikey", _anonKey);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        await Http.SendAsync(request);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}

public class AuthTokens
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = "";

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = "";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("user")]
    public AuthUser? User { get; set; }
}

public class AuthUser
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";
}

[JsonSerializable(typeof(AuthTokens))]
internal partial class AuthJsonContext : JsonSerializerContext;
