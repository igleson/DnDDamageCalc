using DnDDamageCalc.Web.Auth;
using DnDDamageCalc.Web.Data;
using DnDDamageCalc.Web.Html;
using DnDDamageCalc.Web.Models;
using DnDDamageCalc.Web.Services;
using DnDDamageCalc.Web.Simulation;

namespace DnDDamageCalc.Web.Endpoints;

public static class CharacterEndpoints
{
    public static void MapCharacterEndpoints(this WebApplication app)
    {
        app.MapGet("/character/form", (ITemplateService templates) =>
            Results.Text(HtmlFragments.CharacterForm(null, templates), "text/html"));

        app.MapGet("/character/list", async (HttpContext ctx, ICharacterRepository repo, ITemplateService templates) =>
        {
            var userId = ctx.GetUserId();
            var accessToken = ctx.GetAccessToken();
            var characters = await repo.ListAllAsync(userId, accessToken);
            return Results.Text(HtmlFragments.CharacterList(characters, selectedId: null, templates), "text/html");
        });

        app.MapGet("/character/{id:int}", async (int id, HttpContext ctx, ICharacterRepository repo, ITemplateService templates) =>
        {
            var userId = ctx.GetUserId();
            var accessToken = ctx.GetAccessToken();
            var character = await repo.GetByIdAsync(id, userId, accessToken);
            
            // Check if this is an HTMX request (partial update) or direct browser visit (full page)
            var isHtmxRequest = ctx.Request.Headers.ContainsKey("HX-Request");
            
            if (character is null)
            {
                if (isHtmxRequest)
                {
                    // HTMX request: return error message in form container
                    return Results.Text(
                        HtmlFragments.ValidationError($"Character with ID {id} not found.", templates), 
                        "text/html"
                    );
                }
                else
                {
                    // Direct browser visit: redirect to home page
                    return Results.Redirect("/");
                }
            }
            
            if (isHtmxRequest)
            {
                // HTMX request: return just the character form HTML
                return Results.Text(HtmlFragments.CharacterForm(character, templates), "text/html");
            }
            else
            {
                // Direct browser visit: return full page with character loaded
                var characters = await repo.ListAllAsync(userId, accessToken);
                var characterListHtml = HtmlFragments.CharacterList(characters, selectedId: id, templates);
                var characterFormHtml = HtmlFragments.CharacterForm(character, templates);
                var env = ctx.RequestServices.GetRequiredService<IWebHostEnvironment>();
                var showLogout = !env.IsDevelopment();
                var showHotReload = env.IsDevelopment();
                
                var fullPageHtml = HtmlFragments.IndexPage(templates, characterListHtml, characterFormHtml, showLogout, showHotReload);
                return Results.Content(fullPageHtml, "text/html");
            }
        });

        app.MapPost("/character/dice/add", (HttpRequest request, int levelIndex, int attackIndex, ITemplateService templates) =>
        {
            var form = request.Form;
            int.TryParse(form["diceCounter"], out var counter);
            var html = HtmlFragments.DiceGroupFragment(levelIndex, attackIndex, counter, null, templates);
            var newCounter = counter + 1;
            html += $"""<input type="hidden" id="dice-counter" name="diceCounter" value="{newCounter}" hx-swap-oob="true" />""";
            return Results.Text(html, "text/html");
        });





        app.MapDelete("/character/dice/remove", (int levelIndex, int attackIndex, int diceIndex) =>
            Results.Text("", "text/html"));

        app.MapPost("/character/save", async (HttpRequest request, HttpContext ctx, ICharacterRepository repo, ITemplateService templates) =>
        {
            var userId = ctx.GetUserId();
            var accessToken = ctx.GetAccessToken();
            var form = await request.ReadFormAsync();
            var character = FormParser.Parse(form);

            var errors = Validate(character);
            if (errors.Count > 0)
            {
                var errorHtml = string.Join("", errors.Select(e =>
                    $"""<p style="color:var(--pico-del-color);">{System.Net.WebUtility.HtmlEncode(e)}</p>"""));
                return Results.Text(errorHtml + HtmlFragments.CharacterForm(character, templates), "text/html");
            }

            var id = await repo.SaveAsync(character, userId, accessToken);
            character.Id = id;
            var confirmation = HtmlFragments.SaveConfirmation(id, character.Name, templates);
            var characters = await repo.ListAllAsync(userId, accessToken);
            var sidebarOob = $"""<div id="character-list" hx-swap-oob="innerHTML">{HtmlFragments.CharacterList(characters, selectedId: id, templates)}</div>""";
            return Results.Text(confirmation + HtmlFragments.CharacterForm(character, templates) + sidebarOob, "text/html");
        });

        app.MapDelete("/character/{id:int}", async (int id, HttpContext ctx, ICharacterRepository repo, ITemplateService templates) =>
        {
            var userId = ctx.GetUserId();
            var accessToken = ctx.GetAccessToken();
            await repo.DeleteAsync(id, userId, accessToken);
            var characters = await repo.ListAllAsync(userId, accessToken);
            return Results.Text(HtmlFragments.CharacterList(characters, selectedId: null, templates), "text/html");
        });

        app.MapPost("/character/validate-percentages", async (HttpRequest request, ITemplateService templates) =>
        {
            var form = await request.ReadFormAsync();
            int.TryParse(form.Keys.FirstOrDefault(k => k.EndsWith(".hitPercent")) is string hitKey ? form[hitKey].ToString() : "0", out var hit);
            int.TryParse(form.Keys.FirstOrDefault(k => k.EndsWith(".critPercent")) is string critKey ? form[critKey].ToString() : "0", out var crit);

            if (hit + crit > 100)
                return Results.Text(HtmlFragments.ValidationError("Hit% + Crit% cannot exceed 100.", templates), "text/html");

            return Results.Text("", "text/html");
        });

        app.MapPost("/character/calculate", async (HttpRequest request, ITemplateService templates) =>
        {
            var form = await request.ReadFormAsync();
            var character = FormParser.Parse(form);

            var errors = ValidateForCalculation(character);
            if (errors.Count > 0)
            {
                var errorHtml = string.Join("", errors.Select(e =>
                    $"""<p style="color:var(--pico-del-color);">{System.Net.WebUtility.HtmlEncode(e)}</p>"""));
                return Results.Text(errorHtml, "text/html");
            }

            var stats = DamageSimulator.Simulate(character);
            return Results.Text(HtmlFragments.DamageResultsGraph(stats), "text/html");
        });
    }

    private static List<string> Validate(Models.Character character)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(character.Name))
            errors.Add("Character name is required.");

        foreach (var level in character.Levels)
        {
            if (level.LevelNumber < 1 || level.LevelNumber > 20)
                errors.Add($"Level number must be between 1 and 20 (got {level.LevelNumber}).");

            foreach (var attack in level.Attacks)
            {
                if (string.IsNullOrWhiteSpace(attack.Name))
                    errors.Add($"Attack name is required at level {level.LevelNumber}.");

                if (attack.HitPercent + attack.CritPercent > 100)
                    errors.Add($"Hit% + Crit% exceeds 100 for attack \"{attack.Name}\" at level {level.LevelNumber}.");

                if (attack.HitPercent < 0 || attack.HitPercent > 100)
                    errors.Add($"Hit% must be 0-100 for attack \"{attack.Name}\" at level {level.LevelNumber}.");

                if (attack.CritPercent < 0 || attack.CritPercent > 100)
                    errors.Add($"Crit% must be 0-100 for attack \"{attack.Name}\" at level {level.LevelNumber}.");

                if (attack.MasteryTopple && (attack.TopplePercent < 0 || attack.TopplePercent > 100))
                    errors.Add($"Topple% must be 0-100 for attack \"{attack.Name}\" at level {level.LevelNumber}.");
            }
        }

        return errors;
    }

    private static List<string> ValidateForCalculation(Models.Character character)
    {
        var errors = new List<string>();

        if (character.Levels.Count == 0)
            errors.Add("Add at least one level to calculate damage.");

        foreach (var level in character.Levels)
        {
            if (level.Attacks.Count == 0)
                errors.Add($"Level {level.LevelNumber} needs at least one attack.");

            foreach (var attack in level.Attacks)
            {
                if (attack.HitPercent + attack.CritPercent > 100)
                    errors.Add($"Hit% + Crit% exceeds 100 for attack \"{attack.Name}\" at level {level.LevelNumber}.");

                if (attack.HitPercent < 0 || attack.HitPercent > 100)
                    errors.Add($"Hit% must be 0-100 for attack \"{attack.Name}\" at level {level.LevelNumber}.");

                if (attack.CritPercent < 0 || attack.CritPercent > 100)
                    errors.Add($"Crit% must be 0-100 for attack \"{attack.Name}\" at level {level.LevelNumber}.");

                if (attack.MasteryTopple && (attack.TopplePercent < 0 || attack.TopplePercent > 100))
                    errors.Add($"Topple% must be 0-100 for attack \"{attack.Name}\" at level {level.LevelNumber}.");
            }
        }

        return errors;
    }
}
