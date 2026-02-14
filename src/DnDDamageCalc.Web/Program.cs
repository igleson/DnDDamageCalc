using DnDDamageCalc.Web.Data;
using DnDDamageCalc.Web.Endpoints;

var builder = WebApplication.CreateSlimBuilder(args);

var app = builder.Build();

app.UseStaticFiles();

Database.Initialize();

app.MapGet("/", () => Results.Redirect("/index.html"));

app.MapCharacterEndpoints();

app.Run();

public partial class Program { }
