using DnDDamageCalc.Web.Auth;
using DnDDamageCalc.Web.Data;
using DnDDamageCalc.Web.Endpoints;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateSlimBuilder(args);

// Enable console logging for debugging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

builder.Services.AddDataProtection().SetApplicationName("DnDDamageCalc");

var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL") ?? "";
var supabaseAnonKey = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY") ?? "";
var supabaseServiceKey = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_KEY") ?? "";

if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseServiceKey))
{
    throw new InvalidOperationException("SUPABASE_URL and SUPABASE_SERVICE_KEY environment variables must be set");
}

builder.Services.AddHttpClient<ICharacterRepository, SupabaseCharacterRepository>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigureHttpClient((sp, client) => { ActivatorUtilities.CreateInstance<SupabaseCharacterRepository>(sp, client, supabaseUrl, supabaseServiceKey); });

builder.Services.AddSingleton<ICharacterRepository>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient();
    return new SupabaseCharacterRepository(httpClient, supabaseUrl, supabaseAnonKey);
});

var app = builder.Build();

var logger = app.Logger;
logger.LogInformation("=== APPLICATION STARTING ===");
logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
logger.LogInformation("Supabase URL: {Url}", supabaseUrl);

SupabaseAuth.Configure(supabaseUrl, supabaseAnonKey);

var dataProtection = app.Services.GetRequiredService<IDataProtectionProvider>();
var secureCookies = !app.Environment.IsDevelopment();
SessionCookie.Configure(dataProtection, secureCookies);

// Middleware order matters: static files -> auth middleware -> endpoint routing
app.UseStaticFiles();
app.UseMiddleware<AuthMiddleware>();

app.MapGet("/", () => Results.Redirect("/index.html"));
app.MapGet("/health", () => Task.FromResult(Results.Ok()));
app.MapAuthEndpoints();
app.MapCharacterEndpoints();

app.Run();

public partial class Program;
