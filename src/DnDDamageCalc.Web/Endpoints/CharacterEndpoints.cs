using DnDDamageCalc.Web.Auth;
using DnDDamageCalc.Web.Data;
using DnDDamageCalc.Web.Html;
using DnDDamageCalc.Web.Simulation;

namespace DnDDamageCalc.Web.Endpoints;

public static class CharacterEndpoints
{
    public static void MapCharacterEndpoints(this WebApplication app)
    {
        app.MapGet("/character/form", () =>
            Results.Text(HtmlFragments.CharacterForm(), "text/html"));

        app.MapGet("/character/list", async (HttpContext ctx, ICharacterRepository repo) =>
        {
            var userId = ctx.GetUserId();
            var accessToken = ctx.GetAccessToken();
            var characters = await repo.ListAllAsync(userId, accessToken);
            return Results.Text(HtmlFragments.CharacterList(characters, selectedId: null), "text/html");
        });

        app.MapGet("/character/{id:int}", async (int id, HttpContext ctx, ICharacterRepository repo) =>
        {
            var userId = ctx.GetUserId();
            var accessToken = ctx.GetAccessToken();
            var character = await repo.GetByIdAsync(id, userId, accessToken);
            if (character is null) return Results.NotFound();
            return Results.Text(HtmlFragments.CharacterForm(character), "text/html");
        });

        app.MapPost("/character/level/add", (HttpRequest request) =>
        {
            var form = request.Form;
            int.TryParse(form["levelCounter"], out var counter);
            var levelNumber = counter + 1;
            if (levelNumber > 20)
                return Results.Text(HtmlFragments.ValidationError("Maximum 20 levels."), "text/html");

            var html = HtmlFragments.LevelFragment(counter, new Models.CharacterLevel { LevelNumber = levelNumber });
            html += $"""<input type="hidden" id="level-counter" name="levelCounter" value="{levelNumber}" hx-swap-oob="true" />""";
            html += $"""<span id="clone-level-btn" hx-swap-oob="innerHTML">{HtmlFragments.CloneLevelButton()}</span>""";
            return Results.Text(html, "text/html");
        });

        app.MapPost("/character/attack/add", (HttpRequest request, int levelIndex) =>
        {
            var form = request.Form;
            int.TryParse(form["attackCounter"], out var counter);
            var html = HtmlFragments.AttackFragment(levelIndex, counter);
            var newCounter = counter + 1;
            html += $"""<input type="hidden" id="attack-counter" name="attackCounter" value="{newCounter}" hx-swap-oob="true" />""";
            return Results.Text(html, "text/html");
        });

        app.MapPost("/character/dice/add", (HttpRequest request, int levelIndex, int attackIndex) =>
        {
            var form = request.Form;
            int.TryParse(form["diceCounter"], out var counter);
            var html = HtmlFragments.DiceGroupFragment(levelIndex, attackIndex, counter);
            var newCounter = counter + 1;
            html += $"""<input type="hidden" id="dice-counter" name="diceCounter" value="{newCounter}" hx-swap-oob="true" />""";
            return Results.Text(html, "text/html");
        });

        app.MapDelete("/character/level/remove", (int index) =>
            Results.Text("", "text/html"));

        app.MapDelete("/character/attack/remove", (int levelIndex, int attackIndex) =>
            Results.Text("", "text/html"));

        app.MapDelete("/character/dice/remove", (int levelIndex, int attackIndex, int diceIndex) =>
            Results.Text("", "text/html"));

        app.MapPost("/character/save", async (HttpRequest request, HttpContext ctx, ICharacterRepository repo) =>
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
                return Results.Text(errorHtml + HtmlFragments.CharacterForm(character), "text/html");
            }

            var id = await repo.SaveAsync(character, userId, accessToken);
            character.Id = id;
            var confirmation = HtmlFragments.SaveConfirmation(id, character.Name);
            var characters = await repo.ListAllAsync(userId, accessToken);
            var sidebarOob = $"""<div id="character-list" hx-swap-oob="innerHTML">{HtmlFragments.CharacterList(characters, selectedId: id)}</div>""";
            return Results.Text(confirmation + HtmlFragments.CharacterForm(character) + sidebarOob, "text/html");
        });

        app.MapDelete("/character/{id:int}", async (int id, HttpContext ctx, ICharacterRepository repo) =>
        {
            var userId = ctx.GetUserId();
            var accessToken = ctx.GetAccessToken();
            await repo.DeleteAsync(id, userId, accessToken);
            var characters = await repo.ListAllAsync(userId, accessToken);
            return Results.Text(HtmlFragments.CharacterList(characters, selectedId: null), "text/html");
        });

        app.MapPost("/character/validate-percentages", async (HttpRequest request) =>
        {
            var form = await request.ReadFormAsync();
            int.TryParse(form.Keys.FirstOrDefault(k => k.EndsWith(".hitPercent")) is string hitKey ? form[hitKey].ToString() : "0", out var hit);
            int.TryParse(form.Keys.FirstOrDefault(k => k.EndsWith(".critPercent")) is string critKey ? form[critKey].ToString() : "0", out var crit);

            if (hit + crit > 100)
                return Results.Text(HtmlFragments.ValidationError("Hit% + Crit% cannot exceed 100."), "text/html");

            return Results.Text("", "text/html");
        });

        app.MapPost("/character/calculate", async (HttpRequest request) =>
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
            return Results.Text(HtmlFragments.DamageResultsTable(stats), "text/html");
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
