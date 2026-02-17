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
            ["level[0].attacks[0].actionType"] = "bonus_action",
            ["level[0].attacks[0].reactionChancePercent"] = "35",
            ["level[0].attacks[0].hitPercent"] = "65",
            ["level[0].attacks[0].critPercent"] = "5",
            ["level[0].attacks[0].flatModifier"] = "3"
        });

        var character = FormParser.Parse(form);
        var attack = character.Levels[0].Attacks[0];

        Assert.Equal("Longsword", attack.Name);
        Assert.Equal("bonus_action", attack.ActionType);
        Assert.Equal(35, attack.ReactionChancePercent);
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
        Assert.False(attack.RequiresSetup);
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
            ["level[0].attacks[0].actionType"] = "action",
            ["level[0].attacks[0].hitPercent"] = "65",
            ["level[0].attacks[0].critPercent"] = "5",
            ["level[1].number"] = "2",
            ["level[1].attacks[0].name"] = "Greatsword",
            ["level[1].attacks[0].actionType"] = "reaction",
            ["level[1].attacks[0].hitPercent"] = "60",
            ["level[1].attacks[0].critPercent"] = "10"
        });

        var character = FormParser.Parse(form);

        Assert.Equal(2, character.Levels.Count);
        Assert.Equal("Longsword", character.Levels[0].Attacks[0].Name);
        Assert.Equal("Greatsword", character.Levels[1].Attacks[0].Name);
    }

    [Fact]
    public void Parse_AttackOrder_UsesOrderField()
    {
        var form = CreateForm(new Dictionary<string, string>
        {
            ["characterName"] = "Fighter",
            ["level[0].number"] = "1",
            ["level[0].attacks[10].name"] = "Second",
            ["level[0].attacks[10].actionType"] = "action",
            ["level[0].attacks[10].hitPercent"] = "60",
            ["level[0].attacks[10].critPercent"] = "5",
            ["level[0].attacks[10].order"] = "1",
            ["level[0].attacks[4].name"] = "First",
            ["level[0].attacks[4].actionType"] = "action",
            ["level[0].attacks[4].hitPercent"] = "70",
            ["level[0].attacks[4].critPercent"] = "5",
            ["level[0].attacks[4].order"] = "0"
        });

        var character = FormParser.Parse(form);

        Assert.Equal(2, character.Levels[0].Attacks.Count);
        Assert.Equal("First", character.Levels[0].Attacks[0].Name);
        Assert.Equal("Second", character.Levels[0].Attacks[1].Name);
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
    public void Parse_TopplePercent_ParsedCorrectly()
    {
        var form = CreateForm(new Dictionary<string, string>
        {
            ["characterName"] = "Test",
            ["level[0].number"] = "1",
            ["level[0].attacks[0].name"] = "Hammer",
            ["level[0].attacks[0].hitPercent"] = "65",
            ["level[0].attacks[0].critPercent"] = "5",
            ["level[0].attacks[0].masteryTopple"] = "on",
            ["level[0].attacks[0].topplePercent"] = "40"
        });

        var character = FormParser.Parse(form);
        var attack = character.Levels[0].Attacks[0];

        Assert.True(attack.MasteryTopple);
        Assert.Equal(40, attack.TopplePercent);
    }

    [Fact]
    public void Parse_TopplePercent_DefaultsToZero()
    {
        var form = CreateForm(new Dictionary<string, string>
        {
            ["characterName"] = "Test",
            ["level[0].number"] = "1",
            ["level[0].attacks[0].name"] = "Sword",
            ["level[0].attacks[0].hitPercent"] = "65",
            ["level[0].attacks[0].critPercent"] = "5"
        });

        var character = FormParser.Parse(form);
        var attack = character.Levels[0].Attacks[0];

        Assert.Equal(0, attack.TopplePercent);
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

    [Fact]
    public void Parse_RequiresSetup_ParsedCorrectly()
    {
        var form = CreateForm(new Dictionary<string, string>
        {
            ["characterName"] = "Test",
            ["level[0].number"] = "1",
            ["level[0].attacks[0].name"] = "Heavy Strike",
            ["level[0].attacks[0].hitPercent"] = "60",
            ["level[0].attacks[0].critPercent"] = "5",
            ["level[0].attacks[0].requiresSetup"] = "on"
        });

        var character = FormParser.Parse(form);
        var attack = character.Levels[0].Attacks[0];

        Assert.True(attack.RequiresSetup);
    }

    [Fact]
    public void Parse_HasActionSurge_ParsedCorrectly()
    {
        var form = CreateForm(new Dictionary<string, string>
        {
            ["characterName"] = "Test",
            ["level[0].number"] = "1",
            ["level[0].resources.hasActionSurge"] = "true"
        });

        var character = FormParser.Parse(form);

        Assert.True(character.Levels[0].Resources.HasActionSurge);
    }

    [Fact]
    public void Parse_HasActionSurge_CommaSeparatedValue_ParsedCorrectly()
    {
        var form = new FormCollection(new Dictionary<string, StringValues>
        {
            ["characterName"] = new("Test"),
            ["level[0].number"] = new("1"),
            ["level[0].resources.hasActionSurge"] = new("true,false")
        });

        var character = FormParser.Parse(form);

        Assert.True(character.Levels[0].Resources.HasActionSurge);
    }

    [Fact]
    public void Parse_ShieldMasterResources_ParsedCorrectly()
    {
        var form = CreateForm(new Dictionary<string, string>
        {
            ["characterName"] = "Test",
            ["level[0].number"] = "1",
            ["level[0].resources.hasShieldMaster"] = "true",
            ["level[0].resources.shieldMasterTopplePercent"] = "45"
        });

        var character = FormParser.Parse(form);

        Assert.True(character.Levels[0].Resources.HasShieldMaster);
        Assert.Equal(45, character.Levels[0].Resources.ShieldMasterTopplePercent);
    }

    [Fact]
    public void Parse_HeroicInspiration_ParsedCorrectly()
    {
        var form = CreateForm(new Dictionary<string, string>
        {
            ["characterName"] = "Test",
            ["level[0].number"] = "1",
            ["level[0].resources.hasHeroicInspiration"] = "true"
        });

        var character = FormParser.Parse(form);

        Assert.True(character.Levels[0].Resources.HasHeroicInspiration);
    }

    [Fact]
    public void ParseEncounterSetting_ReturnsCombats()
    {
        var form = CreateForm(new Dictionary<string, string>
        {
            ["encounterId"] = "4",
            ["encounterName"] = "  Dungeon Crawl  ",
            ["combat[0].rounds"] = "3",
            ["combat[1].rounds"] = "2",
            ["combat[1].shortRestAfter"] = "on"
        });

        var encounter = FormParser.ParseEncounterSetting(form);

        Assert.Equal(4, encounter.Id);
        Assert.Equal("Dungeon Crawl", encounter.Name);
        Assert.Equal(2, encounter.Combats.Count);
        Assert.Equal(3, encounter.Combats[0].Rounds);
        Assert.True(encounter.Combats[1].ShortRestAfter);
    }

    [Fact]
    public void ParseEncounterSettingId_ReturnsId()
    {
        var form = CreateForm(new Dictionary<string, string>
        {
            ["encounterSettingId"] = "7"
        });

        var id = FormParser.ParseEncounterSettingId(form);

        Assert.Equal(7, id);
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
