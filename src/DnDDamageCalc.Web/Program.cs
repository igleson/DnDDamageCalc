using DnDDamageCalc.Web.Auth;
using DnDDamageCalc.Web.Data;
using DnDDamageCalc.Web.Endpoints;
using DnDDamageCalc.Web.Html;
using DnDDamageCalc.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateSlimBuilder(args);

// Enable console logging for debugging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

builder.Services.AddDataProtection().SetApplicationName("DnDDamageCalc");
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueCountLimit = 10_000;
});

// Register template service
builder.Services.AddSingleton<ITemplateService, TemplateService>();

// Register file watcher service for hot reloading in development
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IFileWatcherService, FileWatcherService>();
}

// Register repository based on environment
if (builder.Environment.IsDevelopment())
{
    // Development: SQLite with no authentication
    var sqliteConnectionString = builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=dev-characters.db";
    Database.Configure(sqliteConnectionString);
    Database.Initialize();
    
    builder.Services.AddSingleton<ICharacterRepository, SqliteCharacterRepository>();
    builder.Services.AddSingleton<IEncounterSettingRepository, SqliteEncounterSettingRepository>();
}
else
{
    // Production: Supabase with authentication
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

    builder.Services.AddSingleton<IEncounterSettingRepository>(sp =>
    {
        var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient();
        return new SupabaseEncounterSettingRepository(httpClient, supabaseUrl, supabaseAnonKey);
    });
}

var app = builder.Build();

var logger = app.Logger;
logger.LogInformation("=== APPLICATION STARTING ===");
logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);

if (!app.Environment.IsDevelopment())
{
    var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL") ?? "";
    var supabaseAnonKey = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY") ?? "";
    
    logger.LogInformation("Supabase URL: {Url}", supabaseUrl);
    SupabaseAuth.Configure(supabaseUrl, supabaseAnonKey);
    
    var dataProtection = app.Services.GetRequiredService<IDataProtectionProvider>();
    SessionCookie.Configure(dataProtection, secureCookies: false);
}
else
{
    logger.LogInformation("Development mode: Authentication disabled, using SQLite");
    var dataProtection = app.Services.GetRequiredService<IDataProtectionProvider>();
    SessionCookie.Configure(dataProtection, secureCookies: true);
}

// Middleware order matters: static files -> auth middleware -> endpoint routing
app.UseStaticFiles();
app.UseMiddleware<AuthMiddleware>();

app.MapGet("/", async (HttpContext ctx, ITemplateService templates, ICharacterRepository repo, IEncounterSettingRepository encounterRepo) => 
{
    var showLogout = !app.Environment.IsDevelopment();
    
    // Get user ID and access token for data fetching
    var userId = ctx.GetUserId();
    var accessToken = ctx.GetAccessToken();
    
    // Fetch character list
    var characters = await repo.ListAllAsync(userId, accessToken);
    var settings = await encounterRepo.ListAllAsync(userId, accessToken);
    var characterListHtml = HtmlFragments.CharacterList(characters, selectedId: null, templates);
    var encounterListHtml = HtmlFragments.EncounterList(settings, selectedId: null, templates);
    
    // Generate empty character form
    var characterFormHtml = HtmlFragments.CharacterForm(null, settings, selectedEncounterId: null, templates);
    
    var indexPageHtml = HtmlFragments.IndexPage(templates, characterListHtml, encounterListHtml, characterFormHtml, showLogout, app.Environment.IsDevelopment());
    return Results.Content(indexPageHtml, "text/html");
});

app.MapGet("/index.html", () => Results.Redirect("/"));
app.MapGet("/health", () => Task.FromResult(Results.Ok()));

if (!app.Environment.IsDevelopment())
{
    app.MapAuthEndpoints();
}

app.MapCharacterEndpoints();
app.MapEncounterSettingEndpoints();

// Hot reload endpoints for development
if (app.Environment.IsDevelopment())
{
    app.MapGet("/dev/hot-reload-check", (IFileWatcherService fileWatcher) =>
        Results.Ok(new { lastModified = fileWatcher.GetLastModifiedTicks() }));

    app.MapPost("/dev/clear-template-cache", (ITemplateService templateService) =>
    {
        templateService.ClearCache();
        return Results.Ok(new { message = "Template cache cleared" });
    });
}

app.Run();

public partial class Program;
