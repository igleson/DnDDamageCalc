using System.Globalization;
using System.Text;
using DnDDamageCalc.Web.Models;
using DnDDamageCalc.Web.Simulation;

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

                    <div style="display:flex;gap:1rem;">
                        <button type="button"
                                hx-post="/character/level/add"
                                hx-target="#levels-container"
                                hx-swap="beforeend"
                                hx-include="#level-counter"
                                class="secondary outline">
                            + Add Level
                        </button>
                        <span id="clone-level-btn">
                            {(c.Levels.Count > 0 ? CloneLevelButton() : "")}
                        </span>
                    </div>
                </section>

                <hr />
                <button type="submit">Save Character</button>
            </form>

            <button type="button"
                    hx-post="/character/calculate"
                    hx-include="#character-form"
                    hx-target="#damage-results"
                    hx-swap="innerHTML"
                    class="contrast"
                    style="margin-top:1rem;width:100%;">
                Calculate Damage
            </button>
            <div id="damage-results"></div>
            """);

        return sb.ToString();
    }

    public static string LevelFragment(int levelIndex, CharacterLevel? level = null)
    {
        var l = level ?? new CharacterLevel();
        var levelNum = l.LevelNumber > 0 ? l.LevelNumber : levelIndex + 1;
        var levelId = $"level-{levelIndex}";
        var levelBodyId = $"level-body-{levelIndex}";
        var sb = new StringBuilder();

        sb.Append($"""
            <article id="{levelId}">
                <header>
                    <div style="display:flex;justify-content:space-between;align-items:center;">
                        <strong>Level {levelNum}</strong>
                        <div style="display:flex;gap:0.5rem;">
                            <button type="button"
                                    data-collapse-target="{levelBodyId}"
                                    onclick="toggleSectionById(this)"
                                    class="outline secondary btn-sm">&#x25bc;</button>
                            <button type="button"
                                    hx-delete="/character/level/remove?index={levelIndex}"
                                    hx-target="#{levelId}"
                                    hx-swap="outerHTML"
                                    class="outline secondary btn-sm">
                                Remove Level
                            </button>
                        </div>
                    </div>
                </header>

                <input type="hidden" name="level[{levelIndex}].number" value="{levelNum}" />

                <div id="{levelBodyId}">
                    <div id="attacks-{levelIndex}">
            """);

        for (var j = 0; j < l.Attacks.Count; j++)
        {
            sb.Append(AttackFragment(levelIndex, j, l.Attacks[j]));
        }

        sb.Append($"""
                    </div>

                    <div style="display:flex;gap:1rem;">
                        <button type="button"
                                hx-post="/character/attack/add?levelIndex={levelIndex}"
                                hx-target="#attacks-{levelIndex}"
                                hx-swap="beforeend"
                                hx-include="#attack-counter"
                                class="outline btn-sm">
                            + Add Attack
                        </button>
                        {(l.Attacks.Count > 0 ? CloneAttackButton(levelIndex) : "")}
                    </div>
                </div>
            </article>
            """);

        return sb.ToString();
    }

    public static string AttackFragment(int levelIndex, int attackIndex, Attack? attack = null)
    {
        var a = attack ?? new Attack();
        var prefix = $"level[{levelIndex}].attacks[{attackIndex}]";
        var attackId = $"attack-{levelIndex}-{attackIndex}";
        var attackBodyId = $"attack-body-{levelIndex}-{attackIndex}";
        var sb = new StringBuilder();

        sb.Append($"""
            <fieldset id="{attackId}">
                <legend style="display:flex;align-items:center;gap:0.5rem;">
                    <button type="button"
                            data-collapse-target="{attackBodyId}"
                            onclick="toggleSectionById(this)"
                            class="outline secondary btn-sm">&#x25bc;</button>
                    {(string.IsNullOrEmpty(a.Name) ? "New Attack" : Encode(a.Name))}
                </legend>

                <div id="{attackBodyId}">
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
                            <label><input type="checkbox" name="{prefix}.masteryTopple" {(a.MasteryTopple ? "checked" : "")}
                                   onchange="this.closest('fieldset').querySelector('.topple-pct').style.display=this.checked?'block':'none'" /> Topple</label>
                        </div>
                    </div>
                    <div class="topple-pct" style="display:{(a.MasteryTopple ? "block" : "none")};">
                        <label for="{prefix}.topplePercent">Topple Save Fail %</label>
                        <input type="number" name="{prefix}.topplePercent" value="{a.TopplePercent}" min="0" max="100" placeholder="e.g. 40" />
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
                                hx-target="#{attackId}"
                                hx-swap="outerHTML"
                                class="outline secondary btn-sm">
                            Remove Attack
                        </button>
                    </div>
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

    public static string CharacterList(List<(int Id, string Name)> characters, int? selectedId = null)
    {
        if (characters.Count == 0)
            return "<p><em>No saved characters.</em></p>";

        var sb = new StringBuilder();
        foreach (var (id, name) in characters)
        {
            var selectedClass = id == selectedId ? " selected" : "";
            sb.Append($"""
                <div class="char-item{selectedClass}">
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

    public static string CloneLevelButton() =>
        """
        <button type="button"
                onclick="cloneLevel()"
                class="secondary outline">
            Clone Last Level
        </button>
        """;

    public static string CloneAttackButton(int levelIndex) =>
        $"""
        <button type="button"
                onclick="cloneAttack({levelIndex})"
                class="outline btn-sm">
            Clone Last Attack
        </button>
        """;

    public static string ValidationError(string message) =>
        $"""<span style="color:var(--pico-del-color);">{Encode(message)}</span>""";

    public static string SaveConfirmation(int id, string name) =>
        $"""<article style="padding:0.75rem;margin-bottom:1rem;border-left:4px solid var(--pico-ins-color);">Character "{Encode(name)}" saved successfully.</article>""";

    public static string DamageResultsGraph(List<LevelStats> stats)
    {
        if (stats.Count == 0)
            return "<p><em>No levels to calculate.</em></p>";

        var ordered = stats.OrderBy(s => s.LevelNumber).ToList();
        var series = new (string Key, string Label, string Color, Func<LevelStats, double> Selector)[]
        {
            ("average", "Average", "#4cc9f0", s => s.Average),
            ("p25", "P25", "#4895ef", s => s.P25),
            ("p50", "P50", "#4361ee", s => s.P50),
            ("p75", "P75", "#3f37c9", s => s.P75),
            ("p90", "P90", "#7209b7", s => s.P90),
            ("p95", "P95", "#b5179e", s => s.P95)
        };

        const double width = 860;
        const double height = 420;
        const double marginLeft = 64;
        const double marginRight = 22;
        const double marginTop = 22;
        const double marginBottom = 50;
        var plotWidth = width - marginLeft - marginRight;
        var plotHeight = height - marginTop - marginBottom;

        var minLevel = ordered.Min(s => s.LevelNumber);
        var maxLevel = ordered.Max(s => s.LevelNumber);
        var allValues = ordered.SelectMany(s => series.Select(metric => metric.Selector(s))).ToList();
        var minDamage = Math.Min(0, allValues.Min());
        var maxDamage = Math.Max(0, allValues.Max());
        if (Math.Abs(maxDamage - minDamage) < 0.0001)
            maxDamage = minDamage + 1;

        double MapX(int level) =>
            marginLeft + (maxLevel == minLevel ? plotWidth / 2.0 : ((level - minLevel) / (double)(maxLevel - minLevel)) * plotWidth);

        double MapY(double value) =>
            marginTop + ((maxDamage - value) / (maxDamage - minDamage)) * plotHeight;

        const int yTickCount = 5;
        var sb = new StringBuilder();
        sb.Append("""
            <article style="margin-top:1rem;">
                <header><strong>Damage Statistics</strong></header>
                <div data-damage-graph style="position:relative;">
                    <div style="display:flex;flex-wrap:wrap;gap:0.75rem;margin-bottom:0.75rem;">
            """);

        foreach (var metric in series)
        {
            sb.Append($"""
                        <label style="display:flex;align-items:center;gap:0.4rem;margin:0;">
                            <input type="checkbox"
                                   data-series-toggle="{metric.Key}"
                                   onchange="toggleDamageSeries(this)"
                                   checked />
                            <span style="display:inline-block;width:0.85rem;height:0.85rem;border-radius:50%;background:{metric.Color};"></span>
                            {metric.Label}
                        </label>
                """);
        }

        sb.Append($"""
                    </div>
                    <div data-damage-tooltip
                         style="display:none;position:absolute;z-index:20;pointer-events:none;padding:0.35rem 0.55rem;background:var(--pico-card-background-color);border:1px solid var(--pico-muted-border-color);border-radius:var(--pico-border-radius);font-size:0.78rem;line-height:1.2;box-shadow:0 4px 12px rgba(0,0,0,0.35);"></div>
                    <svg id="damage-results-graph"
                         viewBox="0 0 {F(width)} {F(height)}"
                         width="100%"
                         role="img"
                         aria-label="Damage by level graph">
                        <rect x="0" y="0" width="{F(width)}" height="{F(height)}" fill="transparent"></rect>
            """);

        for (var i = 0; i <= yTickCount; i++)
        {
            var fraction = i / (double)yTickCount;
            var y = marginTop + (fraction * plotHeight);
            var value = maxDamage - (fraction * (maxDamage - minDamage));
            sb.Append($"""
                        <line x1="{F(marginLeft)}" y1="{F(y)}" x2="{F(width - marginRight)}" y2="{F(y)}"
                              stroke="var(--pico-muted-border-color)" stroke-width="1"></line>
                        <text x="{F(marginLeft - 8)}" y="{F(y + 4)}" text-anchor="end" fill="var(--pico-muted-color)" font-size="10">
                            {F1(value)}
                        </text>
                """);
        }

        foreach (var level in ordered.Select(s => s.LevelNumber))
        {
            var x = MapX(level);
            sb.Append($"""
                        <line x1="{F(x)}" y1="{F(marginTop)}" x2="{F(x)}" y2="{F(height - marginBottom)}"
                              stroke="var(--pico-muted-border-color)" stroke-width="1" stroke-dasharray="2,4"></line>
                        <text x="{F(x)}" y="{F(height - marginBottom + 18)}" text-anchor="middle" fill="var(--pico-muted-color)" font-size="10">
                            {level}
                        </text>
                """);
        }

        sb.Append($"""
                        <line x1="{F(marginLeft)}" y1="{F(height - marginBottom)}" x2="{F(width - marginRight)}" y2="{F(height - marginBottom)}"
                              stroke="var(--pico-color)" stroke-width="1.5"></line>
                        <line x1="{F(marginLeft)}" y1="{F(marginTop)}" x2="{F(marginLeft)}" y2="{F(height - marginBottom)}"
                              stroke="var(--pico-color)" stroke-width="1.5"></line>
                        <text x="{F(marginLeft + (plotWidth / 2.0))}" y="{F(height - 10)}" text-anchor="middle" fill="var(--pico-color)" font-size="12">
                            Level
                        </text>
                        <text x="18" y="{F(marginTop + (plotHeight / 2.0))}" text-anchor="middle" fill="var(--pico-color)" font-size="12"
                              transform="rotate(-90 18 {F(marginTop + (plotHeight / 2.0))})">
                            Damage
                        </text>
            """);

        foreach (var metric in series)
        {
            var points = string.Join(" ", ordered.Select(s => $"{F(MapX(s.LevelNumber))},{F(MapY(metric.Selector(s)))}"));
            sb.Append($"""
                        <g data-series="{metric.Key}">
                            <polyline points="{points}" fill="none" stroke="{metric.Color}" stroke-width="2.25"></polyline>
                """);
            foreach (var stat in ordered)
            {
                var x = MapX(stat.LevelNumber);
                var value = metric.Selector(stat);
                var y = MapY(value);
                sb.Append($"""
                            <circle cx="{F(x)}"
                                    cy="{F(y)}"
                                    r="3.25"
                                    fill="{metric.Color}"
                                    style="cursor:pointer;"
                                    data-series="{metric.Key}"
                                    data-stat-label="{metric.Label}"
                                    data-level="{stat.LevelNumber}"
                                    data-damage="{F1(value)}"
                                    onmouseenter="showDamageTooltip(event)"
                                    onmousemove="moveDamageTooltip(event)"
                                    onmouseleave="hideDamageTooltip(this)"></circle>
                    """);
            }
            sb.Append("""
                        </g>
                """);
        }

        sb.Append("""
                    </svg>
                </div>
            </article>
            """);

        return sb.ToString();
    }

    public static string LoginPage() =>
        """
        <!DOCTYPE html>
        <html lang="en" data-theme="dark">
        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1" />
            <title>D&amp;D Damage Calculator - Login</title>
            <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/@picocss/pico@2/css/pico.min.css" />
            <style>
                :root { font-size: 12px; }
                body { display: flex; justify-content: center; align-items: center; min-height: 100vh; margin: 0; }
            </style>
        </head>
        <body>
            <article style="max-width:400px;width:100%;text-align:center;">
                <header><strong>D&amp;D Damage Calculator</strong></header>
                <p>Sign in to manage your characters and calculate damage statistics.</p>
                <a href="/auth/login" role="button">Sign in with Google</a>
            </article>
        </body>
        </html>
        """;

    private static string Encode(string value) =>
        System.Net.WebUtility.HtmlEncode(value);

    private static string F(double value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string F1(double value) =>
        value.ToString("0.0", CultureInfo.InvariantCulture);
}
