using DnDDamageCalc.Web.Auth;
using DnDDamageCalc.Web.Data;
using DnDDamageCalc.Web.Endpoints;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddDataProtection().SetApplicationName("DnDDamageCalc");

var app = builder.Build();

app.UseStaticFiles();

var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL") ?? "";
var supabaseAnonKey = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY") ?? "";
SupabaseAuth.Configure(supabaseUrl, supabaseAnonKey);

var dataProtection = app.Services.GetRequiredService<IDataProtectionProvider>();
var secureCookies = !app.Environment.IsDevelopment();
SessionCookie.Configure(dataProtection, secureCookies);

app.UseMiddleware<AuthMiddleware>();

Database.Initialize();

app.MapGet("/", () => Results.Redirect("/index.html"));

app.MapAuthEndpoints();
app.MapCharacterEndpoints();

app.Run();

public partial class Program { }
