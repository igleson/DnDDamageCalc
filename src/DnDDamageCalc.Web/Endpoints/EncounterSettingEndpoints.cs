using DnDDamageCalc.Web.Auth;
using DnDDamageCalc.Web.Data;
using DnDDamageCalc.Web.Html;
using DnDDamageCalc.Web.Models;
using DnDDamageCalc.Web.Services;

namespace DnDDamageCalc.Web.Endpoints;

public static class EncounterSettingEndpoints
{
    public static void MapEncounterSettingEndpoints(this WebApplication app)
    {
        app.MapGet("/encounter/list", async (HttpContext ctx, IEncounterSettingRepository repo, ITemplateService templates) =>
        {
            var userId = ctx.GetUserId();
            var accessToken = ctx.GetAccessToken();
            var settings = await repo.ListAllAsync(userId, accessToken);
            var html = HtmlFragments.EncounterList(settings, selectedId: null, templates);
            return Results.Text(html, "text/html");
        });

        app.MapGet("/encounter/form", (ITemplateService templates) =>
            Results.Text(HtmlFragments.EncounterForm(null, templates), "text/html"));

        app.MapGet("/encounter/{id:int}", async (int id, HttpContext ctx, IEncounterSettingRepository repo, ICharacterRepository charRepo, ITemplateService templates) =>
        {
            var userId = ctx.GetUserId();
            var accessToken = ctx.GetAccessToken();
            var encounter = await repo.GetByIdAsync(id, userId, accessToken);
            var isHtmxRequest = ctx.Request.Headers.ContainsKey("HX-Request");

            if (encounter is null)
            {
                if (isHtmxRequest)
                    return Results.Text(HtmlFragments.ValidationError("Encounter setting not found.", templates), "text/html");

                return Results.Redirect("/");
            }

            if (isHtmxRequest)
                return Results.Text(HtmlFragments.EncounterForm(encounter, templates), "text/html");

            var characters = await charRepo.ListAllAsync(userId, accessToken);
            var settings = await repo.ListAllAsync(userId, accessToken);
            var characterListHtml = HtmlFragments.CharacterList(characters, selectedId: null, templates);
            var encounterListHtml = HtmlFragments.EncounterList(settings, selectedId: id, templates);
            var encounterFormHtml = HtmlFragments.EncounterForm(encounter, templates);
            var env = ctx.RequestServices.GetRequiredService<IWebHostEnvironment>();
            var showLogout = !env.IsDevelopment();
            var showHotReload = env.IsDevelopment();
            var fullPageHtml = HtmlFragments.IndexPage(templates, characterListHtml, encounterListHtml, encounterFormHtml, showLogout, showHotReload);
            return Results.Content(fullPageHtml, "text/html");
        });

        app.MapPost("/encounter/save", async (HttpRequest request, HttpContext ctx, IEncounterSettingRepository repo, ITemplateService templates) =>
        {
            var userId = ctx.GetUserId();
            var accessToken = ctx.GetAccessToken();
            var form = await request.ReadFormAsync();
            var setting = FormParser.ParseEncounterSetting(form);

            var errors = Validate(setting);
            if (errors.Count > 0)
            {
                var errorHtml = string.Join("", errors.Select(e =>
                    $"""<p style="color:var(--pico-del-color);">{System.Net.WebUtility.HtmlEncode(e)}</p>"""));
                return Results.Text(errorHtml + HtmlFragments.EncounterForm(setting, templates), "text/html");
            }

            var id = await repo.SaveAsync(setting, userId, accessToken);
            var saved = await repo.GetByIdAsync(id, userId, accessToken) ?? new EncounterSetting { Id = id, Name = setting.Name, Combats = setting.Combats };
            var all = await repo.ListAllAsync(userId, accessToken);
            var confirmation = HtmlFragments.EncounterSaveConfirmation(saved.Name, templates);
            var listOob = $"""<div id="encounter-list" hx-swap-oob="innerHTML">{HtmlFragments.EncounterList(all, selectedId: id, templates)}</div>""";
            return Results.Text(confirmation + HtmlFragments.EncounterForm(saved, templates) + listOob, "text/html");
        });

        app.MapDelete("/encounter/{id:int}", async (int id, HttpContext ctx, IEncounterSettingRepository repo, ITemplateService templates) =>
        {
            var userId = ctx.GetUserId();
            var accessToken = ctx.GetAccessToken();
            await repo.DeleteAsync(id, userId, accessToken);
            var settings = await repo.ListAllAsync(userId, accessToken);
            var html = HtmlFragments.EncounterList(settings, selectedId: null, templates);
            return Results.Text(html, "text/html");
        });
    }

    private static List<string> Validate(EncounterSetting setting)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(setting.Name))
            errors.Add("Encounter setting name is required.");

        if (setting.Combats.Count == 0)
            errors.Add("Add at least one combat.");

        for (var i = 0; i < setting.Combats.Count; i++)
        {
            if (setting.Combats[i].Rounds < 1)
                errors.Add($"Combat {i + 1} rounds must be at least 1.");
        }

        return errors;
    }
}
