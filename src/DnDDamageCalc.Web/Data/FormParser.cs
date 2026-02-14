using System.Text.RegularExpressions;
using DnDDamageCalc.Web.Models;
using Microsoft.AspNetCore.Http;

namespace DnDDamageCalc.Web.Data;

public static partial class FormParser
{
    public static Character Parse(IFormCollection form)
    {
        var character = new Character
        {
            Id = ParseInt(form, "characterId"),
            Name = form["characterName"].ToString().Trim()
        };

        var levelIndices = ExtractIndices(form, LevelPattern());
        foreach (var li in levelIndices)
        {
            var level = new CharacterLevel
            {
                LevelNumber = ParseInt(form, $"level[{li}].number")
            };

            var attackIndices = ExtractIndices(form, AttackPattern(li));
            foreach (var ai in attackIndices)
            {
                var prefix = $"level[{li}].attacks[{ai}]";
                var attack = new Attack
                {
                    Name = form[$"{prefix}.name"].ToString().Trim(),
                    HitPercent = ParseInt(form, $"{prefix}.hitPercent"),
                    CritPercent = ParseInt(form, $"{prefix}.critPercent"),
                    FlatModifier = ParseInt(form, $"{prefix}.flatModifier"),
                    MasteryVex = form[$"{prefix}.masteryVex"] == "on",
                    MasteryTopple = form[$"{prefix}.masteryTopple"] == "on",
                    TopplePercent = ParseInt(form, $"{prefix}.topplePercent")
                };

                var diceIndices = ExtractIndices(form, DicePattern(li, ai));
                foreach (var di in diceIndices)
                {
                    var dPrefix = $"{prefix}.dice[{di}]";
                    attack.DiceGroups.Add(new DiceGroup
                    {
                        Quantity = ParseInt(form, $"{dPrefix}.quantity", 1),
                        DieSize = ParseInt(form, $"{dPrefix}.dieSize", 6)
                    });
                }

                level.Attacks.Add(attack);
            }

            character.Levels.Add(level);
        }

        return character;
    }

    private static int ParseInt(IFormCollection form, string key, int defaultValue = 0)
    {
        var value = form[key].ToString();
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    private static SortedSet<int> ExtractIndices(IFormCollection form, Regex pattern)
    {
        var indices = new SortedSet<int>();
        foreach (var key in form.Keys)
        {
            var match = pattern.Match(key);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var index))
            {
                indices.Add(index);
            }
        }
        return indices;
    }

    [GeneratedRegex(@"^level\[(\d+)\]\.")]
    private static partial Regex LevelPattern();

    private static Regex AttackPattern(int levelIndex) =>
        new($@"^level\[{levelIndex}\]\.attacks\[(\d+)\]\.");

    private static Regex DicePattern(int levelIndex, int attackIndex) =>
        new($@"^level\[{levelIndex}\]\.attacks\[{attackIndex}\]\.dice\[(\d+)\]\.");
}
