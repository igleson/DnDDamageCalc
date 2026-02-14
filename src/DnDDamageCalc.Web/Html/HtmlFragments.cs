using System.Text;
using DnDDamageCalc.Web.Models;

namespace DnDDamageCalc.Web.Html;

public static class HtmlFragments
{
    private static readonly int[] DieSizes = [4, 6, 8, 10, 12, 20];

    public static string CharacterForm(Character? character = null)
    {
        var c = character ?? new Character();
        var sb = new StringBuilder();

        sb.Append($"""
            <form id="character-form" hx-post="/character/save" hx-target="#form-container" hx-swap="innerHTML">
                <input type="hidden" name="characterId" value="{c.Id}" />
                <input type="hidden" id="level-counter" name="levelCounter" value="{c.Levels.Count}" />
                <input type="hidden" id="attack-counter" name="attackCounter" value="0" />
                <input type="hidden" id="dice-counter" name="diceCounter" value="0" />

                <article>
                    <header><strong>Character Info</strong></header>
                    <label for="characterName">Character Name</label>
                    <input type="text" id="characterName" name="characterName"
                           value="{Encode(c.Name)}" required placeholder="Enter character name" />
                </article>

                <section>
                    <hgroup>
                        <h3>Levels</h3>
                        <p>Add levels and define attacks for each one.</p>
                    </hgroup>

                    <div id="levels-container">
            """);

        for (var i = 0; i < c.Levels.Count; i++)
        {
            sb.Append(LevelFragment(i, c.Levels[i]));
        }

        sb.Append($"""
                    </div>

                    <button type="button"
                            hx-post="/character/level/add"
                            hx-target="#levels-container"
                            hx-swap="beforeend"
                            hx-include="#level-counter"
                            class="secondary outline">
                        + Add Level
                    </button>
                </section>

                <hr />
                <button type="submit">Save Character</button>
            </form>
            """);

        return sb.ToString();
    }

    public static string LevelFragment(int levelIndex, CharacterLevel? level = null)
    {
        var l = level ?? new CharacterLevel();
        var levelNum = l.LevelNumber > 0 ? l.LevelNumber : levelIndex + 1;
        var sb = new StringBuilder();

        sb.Append($"""
            <article id="level-{levelIndex}">
                <header>
                    <div style="display:flex;justify-content:space-between;align-items:center;">
                        <strong>Level {levelNum}</strong>
                        <button type="button"
                                hx-delete="/character/level/remove?index={levelIndex}"
                                hx-target="#level-{levelIndex}"
                                hx-swap="outerHTML"
                                class="outline secondary btn-sm">
                            Remove Level
                        </button>
                    </div>
                </header>

                <input type="hidden" name="level[{levelIndex}].number" value="{levelNum}" />

                <div id="attacks-{levelIndex}">
            """);

        for (var j = 0; j < l.Attacks.Count; j++)
        {
            sb.Append(AttackFragment(levelIndex, j, l.Attacks[j]));
        }

        sb.Append($"""
                </div>

                <button type="button"
                        hx-post="/character/attack/add?levelIndex={levelIndex}"
                        hx-target="#attacks-{levelIndex}"
                        hx-swap="beforeend"
                        hx-include="#attack-counter"
                        class="outline btn-sm">
                    + Add Attack
                </button>
            </article>
            """);

        return sb.ToString();
    }

    public static string AttackFragment(int levelIndex, int attackIndex, Attack? attack = null)
    {
        var a = attack ?? new Attack();
        var prefix = $"level[{levelIndex}].attacks[{attackIndex}]";
        var sb = new StringBuilder();

        sb.Append($"""
            <fieldset id="attack-{levelIndex}-{attackIndex}">
                <legend>{(string.IsNullOrEmpty(a.Name) ? "New Attack" : Encode(a.Name))}</legend>

                <label for="{prefix}.name">Attack Name</label>
                <input type="text" name="{prefix}.name" value="{Encode(a.Name)}" required placeholder="e.g. Longsword" />

                <div class="grid">
                    <div>
                        <label for="{prefix}.hitPercent">Hit %</label>
                        <input type="number" name="{prefix}.hitPercent" value="{a.HitPercent}" min="0" max="100"
                               hx-post="/character/validate-percentages"
                               hx-trigger="change"
                               hx-target="#pct-error-{levelIndex}-{attackIndex}"
                               hx-swap="innerHTML"
                               hx-include="[name='{prefix}.hitPercent'],[name='{prefix}.critPercent']" />
                    </div>
                    <div>
                        <label for="{prefix}.critPercent">Crit %</label>
                        <input type="number" name="{prefix}.critPercent" value="{a.CritPercent}" min="0" max="100"
                               hx-post="/character/validate-percentages"
                               hx-trigger="change"
                               hx-target="#pct-error-{levelIndex}-{attackIndex}"
                               hx-swap="innerHTML"
                               hx-include="[name='{prefix}.hitPercent'],[name='{prefix}.critPercent']" />
                    </div>
                </div>
                <small id="pct-error-{levelIndex}-{attackIndex}" style="color:var(--pico-del-color);"></small>

                <div class="grid">
                    <div>
                        <label><input type="checkbox" name="{prefix}.masteryVex" {(a.MasteryVex ? "checked" : "")} /> Vex</label>
                    </div>
                    <div>
                        <label><input type="checkbox" name="{prefix}.masteryTopple" {(a.MasteryTopple ? "checked" : "")} /> Topple</label>
                    </div>
                </div>

                <h6>Damage</h6>
                <div id="dice-{levelIndex}-{attackIndex}">
            """);

        for (var k = 0; k < a.DiceGroups.Count; k++)
        {
            sb.Append(DiceGroupFragment(levelIndex, attackIndex, k, a.DiceGroups[k]));
        }

        sb.Append($"""
                </div>

                <button type="button"
                        hx-post="/character/dice/add?levelIndex={levelIndex}&attackIndex={attackIndex}"
                        hx-target="#dice-{levelIndex}-{attackIndex}"
                        hx-swap="beforeend"
                        hx-include="#dice-counter"
                        class="outline btn-sm">
                    + Add Dice
                </button>

                <div style="margin-top:1rem;">
                    <label for="{prefix}.flatModifier">Damage Modifier (+/-)</label>
                    <input type="number" name="{prefix}.flatModifier" value="{a.FlatModifier}" placeholder="e.g. 5 for +5" />
                </div>

                <div class="attack-actions">
                    <button type="button"
                            hx-delete="/character/attack/remove?levelIndex={levelIndex}&attackIndex={attackIndex}"
                            hx-target="#attack-{levelIndex}-{attackIndex}"
                            hx-swap="outerHTML"
                            class="outline secondary btn-sm">
                        Remove Attack
                    </button>
                </div>
            </fieldset>
            """);

        return sb.ToString();
    }

    public static string DiceGroupFragment(int levelIndex, int attackIndex, int diceIndex, DiceGroup? dice = null)
    {
        var d = dice ?? new DiceGroup { Quantity = 1, DieSize = 6 };
        var prefix = $"level[{levelIndex}].attacks[{attackIndex}].dice[{diceIndex}]";

        var sizeOptions = new StringBuilder();
        foreach (var size in DieSizes)
        {
            var selected = size == d.DieSize ? " selected" : "";
            sizeOptions.Append($"""<option value="{size}"{selected}>d{size}</option>""");
        }

        return $"""
            <div id="dice-{levelIndex}-{attackIndex}-{diceIndex}" class="grid" style="align-items:end;">
                <div>
                    <label for="{prefix}.quantity">Qty</label>
                    <input type="number" name="{prefix}.quantity" value="{d.Quantity}" min="1" max="99" required />
                </div>
                <div>
                    <label for="{prefix}.dieSize">Die</label>
                    <select name="{prefix}.dieSize">{sizeOptions}</select>
                </div>
                <div>
                    <button type="button"
                            hx-delete="/character/dice/remove?levelIndex={levelIndex}&attackIndex={attackIndex}&diceIndex={diceIndex}"
                            hx-target="#dice-{levelIndex}-{attackIndex}-{diceIndex}"
                            hx-swap="outerHTML"
                            class="outline secondary btn-sm"
                            style="margin-bottom:0;">
                        X
                    </button>
                </div>
            </div>
            """;
    }

    public static string CharacterList(List<(int Id, string Name)> characters)
    {
        if (characters.Count == 0)
            return "<p><em>No saved characters.</em></p>";

        var sb = new StringBuilder();
        foreach (var (id, name) in characters)
        {
            sb.Append($"""
                <div class="char-item">
                    <a href="#"
                       hx-get="/character/{id}"
                       hx-target="#form-container"
                       hx-swap="innerHTML">
                        {Encode(name)}
                    </a>
                    <button type="button"
                            hx-delete="/character/{id}"
                            hx-target="#character-list"
                            hx-swap="innerHTML"
                            hx-confirm="Delete {Encode(name)}?"
                            class="outline secondary btn-sm">
                        &times;
                    </button>
                </div>
                """);
        }
        return sb.ToString();
    }

    public static string ValidationError(string message) =>
        $"""<span style="color:var(--pico-del-color);">{Encode(message)}</span>""";

    public static string SaveConfirmation(int id, string name) =>
        $"""<article style="padding:0.75rem;margin-bottom:1rem;border-left:4px solid var(--pico-ins-color);">Character "{Encode(name)}" saved successfully.</article>""";

    private static string Encode(string value) =>
        System.Net.WebUtility.HtmlEncode(value);
}
