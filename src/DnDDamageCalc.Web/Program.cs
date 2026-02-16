using DnDDamageCalc.Web.Auth;
using DnDDamageCalc.Web.Data;
using DnDDamageCalc.Web.Endpoints;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateSlimBuilder(args);

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
.ConfigureHttpClient((sp, client) =>
{
    var repo = ActivatorUtilities.CreateInstance<SupabaseCharacterRepository>(sp, client, supabaseUrl, supabaseServiceKey);
});

builder.Services.AddSingleton<ICharacterRepository>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient();
    return new SupabaseCharacterRepository(httpClient, supabaseUrl, supabaseAnonKey);
});

var app = builder.Build();

app.UseStaticFiles();

SupabaseAuth.Configure(supabaseUrl, supabaseAnonKey);

var dataProtection = app.Services.GetRequiredService<IDataProtectionProvider>();
var secureCookies = !app.Environment.IsDevelopment();
SessionCookie.Configure(dataProtection, secureCookies);

app.UseMiddleware<AuthMiddleware>();

app.MapGet("/", () => Results.Redirect("/index.html"));

app.MapAuthEndpoints();
app.MapCharacterEndpoints();

app.Run();

public partial class Program { }
