using DnDDamageCalc.Web.Models;
using DnDDamageCalc.Web.Simulation;

namespace DnDDamageCalc.Tests;

public class DamageSimulatorTests
{
    [Fact]
    public void Simulate_AlwaysHits_AverageMatchesExpectedDice()
    {
        // 1d6+3 with 100% hit, 0% crit => average = 3.5 + 3 = 6.5
        var character = MakeCharacter(hitPercent: 100, critPercent: 0,
            flatModifier: 3, diceGroups: [new DiceGroup { Quantity = 1, DieSize = 6 }]);

        var results = DamageSimulator.Simulate(character, iterations: 50_000);

        Assert.Single(results);
        Assert.InRange(results[0].Average, 6.0, 7.0);
    }

    [Fact]
    public void Simulate_AlwaysMisses_DamageIsZero()
    {
        var character = MakeCharacter(hitPercent: 0, critPercent: 0,
            flatModifier: 5, diceGroups: [new DiceGroup { Quantity = 2, DieSize = 8 }]);

        var results = DamageSimulator.Simulate(character, iterations: 10_000);

        Assert.Single(results);
        Assert.Equal(0, results[0].Average);
        Assert.Equal(0, results[0].P50);
    }

    [Fact]
    public void Simulate_AlwaysCrits_DiceDoubledFlatNot()
    {
        // 1d6+3 with 100% hit, 100% crit => always crit => 2d6+3 => avg = 7 + 3 = 10
        var character = MakeCharacter(hitPercent: 100, critPercent: 100,
            flatModifier: 3, diceGroups: [new DiceGroup { Quantity = 1, DieSize = 6 }]);

        var results = DamageSimulator.Simulate(character, iterations: 50_000);

        Assert.Single(results);
        Assert.InRange(results[0].Average, 9.5, 10.5);
    }

    [Fact]
    public void Simulate_FlatModifierOnly_ExactDamage()
    {
        // No dice, just +5, always hits => damage = 5 every time
        var character = MakeCharacter(hitPercent: 100, critPercent: 0,
            flatModifier: 5, diceGroups: []);

        var results = DamageSimulator.Simulate(character, iterations: 1_000);

        Assert.Single(results);
        Assert.Equal(5.0, results[0].Average, precision: 1);
        Assert.Equal(5.0, results[0].P25, precision: 1);
        Assert.Equal(5.0, results[0].P50, precision: 1);
        Assert.Equal(5.0, results[0].P75, precision: 1);
    }

    [Fact]
    public void Simulate_NoLevels_EmptyResults()
    {
        var character = new Character { Name = "Empty" };

        var results = DamageSimulator.Simulate(character);

        Assert.Empty(results);
    }

    [Fact]
    public void Simulate_VexGrantsAdvantageOnMiss()
    {
        // Two attacks: first always misses with Vex, second has 50% hit
        // Without Vex advantage, second attack hits 50% of time
        // With Vex advantage, second attack should hit ~75% of time (1-(1-0.5)^2)
        var attacks = new List<Attack>
        {
            new()
            {
                Name = "Miss with Vex", HitPercent = 0, CritPercent = 0,
                MasteryVex = true, FlatModifier = 0, DiceGroups = []
            },
            new()
            {
                Name = "Follow-up", HitPercent = 50, CritPercent = 0,
                FlatModifier = 10, DiceGroups = []
            }
        };
        var character = new Character
        {
            Name = "Vex Test",
            Levels = [new CharacterLevel { LevelNumber = 1, Attacks = attacks }]
        };

        var results = DamageSimulator.Simulate(character, iterations: 50_000);

        // With advantage: expected = 0.75 * 10 = 7.5
        // Without advantage: expected = 0.5 * 10 = 5.0
        Assert.True(results[0].Average > 6.5, $"Average {results[0].Average} should be > 6.5 (Vex advantage expected ~7.5)");
    }

    [Fact]
    public void Simulate_VexConsumedAfterOneUse()
    {
        // Three attacks: first misses (Vex), second should have advantage, third should NOT
        var attacks = new List<Attack>
        {
            new()
            {
                Name = "Miss with Vex", HitPercent = 0, CritPercent = 0,
                MasteryVex = true, FlatModifier = 0, DiceGroups = []
            },
            new()
            {
                Name = "Second", HitPercent = 50, CritPercent = 0,
                FlatModifier = 10, DiceGroups = []
            },
            new()
            {
                Name = "Third", HitPercent = 50, CritPercent = 0,
                FlatModifier = 10, DiceGroups = []
            }
        };
        var character = new Character
        {
            Name = "Vex Consumed Test",
            Levels = [new CharacterLevel { LevelNumber = 1, Attacks = attacks }]
        };

        var results = DamageSimulator.Simulate(character, iterations: 50_000);

        // Expected: 0 + 0.75*10 + 0.5*10 = 12.5
        Assert.InRange(results[0].Average, 12.0, 13.0);
    }

    [Fact]
    public void Simulate_ToppleMakesTargetProne_SubsequentAttacksGetAdvantage()
    {
        // First attack always hits with Topple (100% topple chance),
        // second attack has 50% hit → should get advantage from prone → ~75%
        var attacks = new List<Attack>
        {
            new()
            {
                Name = "Topple Hit", HitPercent = 100, CritPercent = 0,
                MasteryTopple = true, TopplePercent = 100,
                FlatModifier = 0, DiceGroups = []
            },
            new()
            {
                Name = "Follow-up", HitPercent = 50, CritPercent = 0,
                FlatModifier = 10, DiceGroups = []
            }
        };
        var character = new Character
        {
            Name = "Topple Test",
            Levels = [new CharacterLevel { LevelNumber = 1, Attacks = attacks }]
        };

        var results = DamageSimulator.Simulate(character, iterations: 50_000);

        // With advantage from prone: expected = 0.75 * 10 = 7.5
        Assert.True(results[0].Average > 6.5, $"Average {results[0].Average} should be > 6.5 (prone advantage expected ~7.5)");
    }

    [Fact]
    public void Simulate_TopplePersistsAcrossAttacks()
    {
        // First hits with topple, second and third both get advantage (prone persists)
        var attacks = new List<Attack>
        {
            new()
            {
                Name = "Topple Hit", HitPercent = 100, CritPercent = 0,
                MasteryTopple = true, TopplePercent = 100,
                FlatModifier = 0, DiceGroups = []
            },
            new()
            {
                Name = "Second", HitPercent = 50, CritPercent = 0,
                FlatModifier = 10, DiceGroups = []
            },
            new()
            {
                Name = "Third", HitPercent = 50, CritPercent = 0,
                FlatModifier = 10, DiceGroups = []
            }
        };
        var character = new Character
        {
            Name = "Topple Persist Test",
            Levels = [new CharacterLevel { LevelNumber = 1, Attacks = attacks }]
        };

        var results = DamageSimulator.Simulate(character, iterations: 50_000);

        // Both follow-ups get advantage: 0 + 0.75*10 + 0.75*10 = 15.0
        Assert.Equal(15.0, results[0].Average, precision: 0);
    }

    [Fact]
    public void Simulate_PercentilesAreOrdered()
    {
        var character = MakeCharacter(hitPercent: 65, critPercent: 5,
            flatModifier: 3, diceGroups: [new DiceGroup { Quantity = 2, DieSize = 6 }]);

        var results = DamageSimulator.Simulate(character, iterations: 10_000);

        var s = results[0];
        Assert.True(s.P25 <= s.P50);
        Assert.True(s.P50 <= s.P75);
        Assert.True(s.P75 <= s.P90);
        Assert.True(s.P90 <= s.P95);
    }

    private static Character MakeCharacter(int hitPercent, int critPercent,
        int flatModifier, List<DiceGroup> diceGroups)
    {
        return new Character
        {
            Name = "Test",
            Levels =
            [
                new CharacterLevel
                {
                    LevelNumber = 1,
                    Attacks =
                    [
                        new Attack
                        {
                            Name = "Test Attack",
                            HitPercent = hitPercent,
                            CritPercent = critPercent,
                            FlatModifier = flatModifier,
                            DiceGroups = diceGroups
                        }
                    ]
                }
            ]
        };
    }
}
