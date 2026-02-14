using DnDDamageCalc.Web.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace DnDDamageCalc.Tests;

public class FormParserTests
{
    [Fact]
    public void Parse_BasicCharacter_ReturnsName()
    {
        var form = CreateForm(new Dictionary<string, string>
        {
            ["characterId"] = "0",
            ["characterName"] = "Gandalf"
        });

        var character = FormParser.Parse(form);

        Assert.Equal("Gandalf", character.Name);
        Assert.Equal(0, character.Id);
    }

    [Fact]
    public void Parse_WithLevel_ReturnsLevelNumber()
    {
        var form = CreateForm(new Dictionary<string, string>
        {
            ["characterName"] = "Test",
            ["level[0].number"] = "3"
        });

        var character = FormParser.Parse(form);

        Assert.Single(character.Levels);
        Assert.Equal(3, character.Levels[0].LevelNumber);
    }

    [Fact]
    public void Parse_WithAttack_ReturnsAttackDetails()
    {
        var form = CreateForm(new Dictionary<string, string>
        {
            ["characterName"] = "Test",
            ["level[0].number"] = "1",
            ["level[0].attacks[0].name"] = "Longsword",
            ["level[0].attacks[0].hitPercent"] = "65",
            ["level[0].attacks[0].critPercent"] = "5",
            ["level[0].attacks[0].flatModifier"] = "3"
        });

        var character = FormParser.Parse(form);
        var attack = character.Levels[0].Attacks[0];

        Assert.Equal("Longsword", attack.Name);
        Assert.Equal(65, attack.HitPercent);
        Assert.Equal(5, attack.CritPercent);
        Assert.Equal(3, attack.FlatModifier);
    }

    [Fact]
    public void Parse_Checkboxes_ParseCorrectly()
    {
        var form = CreateForm(new Dictionary<string, string>
        {
            ["characterName"] = "Test",
            ["level[0].number"] = "1",
            ["level[0].attacks[0].name"] = "Test",
            ["level[0].attacks[0].hitPercent"] = "50",
            ["level[0].attacks[0].critPercent"] = "5",
            ["level[0].attacks[0].masteryVex"] = "on"
        });

        var character = FormParser.Parse(form);
        var attack = character.Levels[0].Attacks[0];

        Assert.True(attack.MasteryVex);
        Assert.False(attack.MasteryTopple);
    }

    [Fact]
    public void Parse_DiceGroups_ReturnsAll()
    {
        var form = CreateForm(new Dictionary<string, string>
        {
            ["characterName"] = "Test",
            ["level[0].number"] = "1",
            ["level[0].attacks[0].name"] = "Test",
            ["level[0].attacks[0].hitPercent"] = "50",
            ["level[0].attacks[0].critPercent"] = "5",
            ["level[0].attacks[0].dice[0].quantity"] = "2",
            ["level[0].attacks[0].dice[0].dieSize"] = "6",
            ["level[0].attacks[0].dice[1].quantity"] = "1",
            ["level[0].attacks[0].dice[1].dieSize"] = "4"
        });

        var character = FormParser.Parse(form);
        var dice = character.Levels[0].Attacks[0].DiceGroups;

        Assert.Equal(2, dice.Count);
        Assert.Equal(2, dice[0].Quantity);
        Assert.Equal(6, dice[0].DieSize);
        Assert.Equal(1, dice[1].Quantity);
        Assert.Equal(4, dice[1].DieSize);
    }

    [Fact]
    public void Parse_NonContiguousIndices_HandlesGaps()
    {
        var form = CreateForm(new Dictionary<string, string>
        {
            ["characterName"] = "Test",
            ["level[0].number"] = "1",
            ["level[3].number"] = "4",
            ["level[7].number"] = "8"
        });

        var character = FormParser.Parse(form);

        Assert.Equal(3, character.Levels.Count);
        Assert.Equal(1, character.Levels[0].LevelNumber);
        Assert.Equal(4, character.Levels[1].LevelNumber);
        Assert.Equal(8, character.Levels[2].LevelNumber);
    }

    [Fact]
    public void Parse_MultipleLevelsWithAttacks_ReturnsFullStructure()
    {
        var form = CreateForm(new Dictionary<string, string>
        {
            ["characterName"] = "Fighter",
            ["level[0].number"] = "1",
            ["level[0].attacks[0].name"] = "Longsword",
            ["level[0].attacks[0].hitPercent"] = "65",
            ["level[0].attacks[0].critPercent"] = "5",
            ["level[1].number"] = "2",
            ["level[1].attacks[0].name"] = "Greatsword",
            ["level[1].attacks[0].hitPercent"] = "60",
            ["level[1].attacks[0].critPercent"] = "10"
        });

        var character = FormParser.Parse(form);

        Assert.Equal(2, character.Levels.Count);
        Assert.Equal("Longsword", character.Levels[0].Attacks[0].Name);
        Assert.Equal("Greatsword", character.Levels[1].Attacks[0].Name);
    }

    [Fact]
    public void Parse_ExistingCharacterId_PreservesId()
    {
        var form = CreateForm(new Dictionary<string, string>
        {
            ["characterId"] = "42",
            ["characterName"] = "Test"
        });

        var character = FormParser.Parse(form);
        Assert.Equal(42, character.Id);
    }

    [Fact]
    public void Parse_TrimsWhitespace()
    {
        var form = CreateForm(new Dictionary<string, string>
        {
            ["characterName"] = "  Gandalf  ",
            ["level[0].number"] = "1",
            ["level[0].attacks[0].name"] = "  Staff  ",
            ["level[0].attacks[0].hitPercent"] = "50",
            ["level[0].attacks[0].critPercent"] = "5"
        });

        var character = FormParser.Parse(form);

        Assert.Equal("Gandalf", character.Name);
        Assert.Equal("Staff", character.Levels[0].Attacks[0].Name);
    }

    private static IFormCollection CreateForm(Dictionary<string, string> values)
    {
        var dict = new Dictionary<string, StringValues>();
        foreach (var kv in values)
        {
            dict[kv.Key] = new StringValues(kv.Value);
        }
        return new FormCollection(dict);
    }
}
