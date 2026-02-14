var builder = WebApplication.CreateSlimBuilder(args);

var app = builder.Build();

app.UseStaticFiles();

app.MapGet("/", () => Results.Redirect("/index.html"));

app.MapPost("/counter/increment", (int? count) =>
{
    var newCount = (count ?? 0) + 1;
    return Results.Text(
        $"""<div id="counter"><span>Count: {newCount}</span><button hx-post="/counter/increment?count={newCount}" hx-target="#counter" hx-swap="outerHTML">Increment</button></div>""",
        "text/html");
});

app.Run();

public partial class Program { }
