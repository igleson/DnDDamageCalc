using System.Globalization;
using System.Text;
using DnDDamageCalc.Web.Models;
using DnDDamageCalc.Web.Services;
using DnDDamageCalc.Web.Simulation;

namespace DnDDamageCalc.Web.Html;

public static class HtmlFragments
{
    private static readonly int[] DieSizes = [4, 6, 8, 10, 12, 20];

    private static string? _emptyCharacterForm;
    private static string? _emptyCharacterLevelForm;
    private static string? _emptyAttackForm = null; // Reset cache to pick up new Graze mastery
    private static string? _emptyDiceGroupForm;

    public static string CharacterForm(Character? character, List<(int Id, string Name)> encounterSettings, int? selectedEncounterId, ITemplateService templates)
    {
        if (character is null && _emptyCharacterForm is not null && encounterSettings.Count == 0) return _emptyCharacterForm;
        var c = character ?? new Character();

        // Generate level HTML fragments
        var levels = new List<object>();
        for (var i = 0; i < c.Levels.Count; i++)
        {
            levels.Add(new { html = LevelFragment(i, c.Levels[i], templates) });
        }

        var model = new
        {
            character_id = c.Id,
            character_name = c.Name ?? "",
            level_count = c.Levels.Count,
            levels = levels,
            has_levels = c.Levels.Count > 0,
            clone_level_button = c.Levels.Count > 0 ? new { html = CloneLevelButton(templates) } : null,
            encounter_settings = encounterSettings.Select(s => new { id = s.Id, name = s.Name }).ToList(),
            selected_encounter_id = selectedEncounterId
        };

        var renderedResult = templates.Render("character-form", model);
        if (_emptyCharacterForm is null && character is null && encounterSettings.Count == 0) _emptyCharacterForm = renderedResult;

        return renderedResult;
    }

    public static string LevelFragment(int levelIndex, CharacterLevel? level, ITemplateService templates)
    {
        if (level is null && _emptyCharacterLevelForm is not null) return _emptyCharacterLevelForm;
        var l = level ?? new CharacterLevel();
        var levelNum = l.LevelNumber > 0 ? l.LevelNumber : levelIndex + 1;
        var levelId = $"level-{levelIndex}";
        var levelBodyId = $"level-body-{levelIndex}";

        // Generate attack HTML fragments
        var attacks = new List<object>();
        for (var j = 0; j < l.Attacks.Count; j++)
        {
            attacks.Add(new { html = AttackFragment(levelIndex, j, l.Attacks[j], templates) });
        }

        var model = new
        {
            level_index = levelIndex,
            level_number = levelNum,
            level_id = levelId,
            level_body_id = levelBodyId,
            attacks = attacks,
            has_attacks = l.Attacks.Count > 0,
            clone_attack_button = l.Attacks.Count > 0 ? new { html = CloneAttackButton(levelIndex, templates) } : null
        };

        var renderedResult = templates.Render("level-fragment", model);

        if (_emptyCharacterLevelForm is null && level is null) _emptyCharacterLevelForm = renderedResult;

        return renderedResult;
    }

    public static string AttackFragment(int levelIndex, int attackIndex, Attack? attack, ITemplateService templates)
    {
        // Temporarily bypass cache to pick up template changes
        // if (attack is null && _emptyAttackForm is not null) return _emptyAttackForm;
        
        var a = attack ?? new Attack();
        var prefix = $"level[{levelIndex}].attacks[{attackIndex}]";
        var attackId = $"attack-{levelIndex}-{attackIndex}";
        var attackBodyId = $"attack-body-{levelIndex}-{attackIndex}";

        // Generate dice group HTML fragments
        var diceGroups = new List<object>();
        for (var k = 0; k < a.DiceGroups.Count; k++)
        {
            diceGroups.Add(new { html = DiceGroupFragment(levelIndex, attackIndex, k, a.DiceGroups[k], templates) });
        }

        var model = new
        {
            level_index = levelIndex,
            attack_index = attackIndex,
            prefix = prefix,
            attack_id = attackId,
            attack_body_id = attackBodyId,
            name = a.Name,
            hit_percent = a.HitPercent,
            crit_percent = a.CritPercent,
            mastery_vex = a.MasteryVex,
            mastery_topple = a.MasteryTopple,
            topple_percent = a.TopplePercent,
            mastery_graze = a.MasteryGraze,
            graze_value = a.GrazeValue,
            flat_modifier = a.FlatModifier,
            dice_groups = diceGroups
        };

        var renderedResult = templates.Render("attack-fragment", model);

        // if (_emptyAttackForm is null && attack is null) _emptyAttackForm = renderedResult;
        
        return renderedResult;
    }

    public static string DiceGroupFragment(int levelIndex, int attackIndex, int diceIndex, DiceGroup? dice, ITemplateService templates)
    {
        if (dice is null && _emptyDiceGroupForm is not null) return _emptyDiceGroupForm;
        var d = dice ?? new DiceGroup { Quantity = 1, DieSize = 6 };
        var prefix = $"level[{levelIndex}].attacks[{attackIndex}].dice[{diceIndex}]";

        var model = new
        {
            level_index = levelIndex,
            attack_index = attackIndex,
            dice_index = diceIndex,
            prefix = prefix,
            quantity = d.Quantity,
            die_size = d.DieSize,
            die_sizes = DieSizes
        };
        var renderedResult = templates.Render("dice-group-fragment", model);

        if (_emptyDiceGroupForm is null && dice is null) _emptyDiceGroupForm = renderedResult;
        
        return renderedResult;
    }

    public static string CharacterList(List<(int Id, string Name)> characters, int? selectedId, ITemplateService templates)
    {
        return templates.Render("character-list", new
        {
            characters = characters.Select(c => new { id = c.Id, name = c.Name }).ToList(),
            selected_id = selectedId
        });
    }

    public static string CloneLevelButton(ITemplateService templates) =>
        templates.Render("clone-level-button");

    public static string CloneAttackButton(int levelIndex, ITemplateService templates) =>
        templates.Render("clone-attack-button", new { level_index = levelIndex });

    public static string EncounterList(List<(int Id, string Name)> settings, int? selectedId, ITemplateService templates)
    {
        return templates.Render("encounter-list", new
        {
            settings = settings.Select(s => new { id = s.Id, name = s.Name }).ToList(),
            selected_id = selectedId
        });
    }

    public static string EncounterForm(EncounterSetting? encounter, ITemplateService templates)
    {
        var e = encounter ?? new EncounterSetting
        {
            Combats = [new CombatDefinition { Rounds = 1, ShortRestAfter = false }]
        };

        if (e.Combats.Count == 0)
            e.Combats.Add(new CombatDefinition { Rounds = 1, ShortRestAfter = false });

        return templates.Render("encounter-form", new
        {
            encounter_id = e.Id,
            encounter_name = e.Name,
            combats = e.Combats.Select((c, i) => new
            {
                index = i,
                rounds = c.Rounds <= 0 ? 1 : c.Rounds,
                short_rest_after = c.ShortRestAfter
            }).ToList()
        });
    }

    public static string ValidationError(string message, ITemplateService templates) =>
        templates.Render("validation-error", new { message });

    public static string SaveConfirmation(int id, string name, ITemplateService templates) =>
        templates.Render("save-confirmation", new { name });

    public static string EncounterSaveConfirmation(string name, ITemplateService templates) =>
        templates.Render("encounter-save-confirmation", new { name });

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

    public static string LoginPage(ITemplateService templates) =>
        templates.Render("login-page");

    public static string IndexPage(ITemplateService templates, string characterListHtml, string encounterListHtml, string characterFormHtml, bool showLogout = true, bool showHotReload = false) =>
        templates.Render("index-page", new
        {
            show_logout = showLogout,
            show_hot_reload = showHotReload,
            character_list = new { html = characterListHtml },
            encounter_list = new { html = encounterListHtml },
            character_form = new { html = characterFormHtml }
        });

    private static string F(double value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string F1(double value) =>
        value.ToString("0.0", CultureInfo.InvariantCulture);
}
