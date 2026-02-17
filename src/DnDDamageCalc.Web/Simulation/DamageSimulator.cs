using DnDDamageCalc.Web.Models;

namespace DnDDamageCalc.Web.Simulation;

public class LevelStats
{
    public int LevelNumber { get; set; }
    public double Average { get; set; }
    public double P25 { get; set; }
    public double P50 { get; set; }
    public double P75 { get; set; }
    public double P90 { get; set; }
    public double P95 { get; set; }
}

public static class DamageSimulator
{
    public static List<LevelStats> Simulate(Character character, int iterations = 10_000)
    {
        var defaultSetting = new EncounterSetting
        {
            Name = "Default",
            Combats = [new CombatDefinition { Rounds = 1, ShortRestAfter = false }]
        };
        return Simulate(character, defaultSetting, iterations);
    }

    public static List<LevelStats> Simulate(Character character, EncounterSetting setting, int iterations = 10_000)
    {
        var results = new List<LevelStats>();
        var totalRounds = setting.Combats.Sum(c => Math.Max(1, c.Rounds));

        foreach (var level in character.Levels)
        {
            var damages = new double[Math.Max(1, iterations * totalRounds)];
            var index = 0;
            for (var i = 0; i < iterations; i++)
            {
                var hasActionSurge = level.Resources?.HasActionSurge == true;
                var nextAttackHasAdvantage = false;
                var actionSurgesRemaining = hasActionSurge ? 1 : 0;

                foreach (var combat in setting.Combats)
                {
                    for (var round = 0; round < Math.Max(1, combat.Rounds); round++)
                    {
                        damages[index++] = SimulateRound(level.Attacks, ref nextAttackHasAdvantage, isFirstRoundOfCombat: round == 0, ref actionSurgesRemaining);
                    }

                    nextAttackHasAdvantage = false;
                    if (combat.ShortRestAfter && hasActionSurge)
                        actionSurgesRemaining = 1;
                }
            }

            Array.Sort(damages);

            results.Add(new LevelStats
            {
                LevelNumber = level.LevelNumber,
                Average = damages.Length > 0 ? damages.Average() : 0,
                P25 = Percentile(damages, 0.25),
                P50 = Percentile(damages, 0.50),
                P75 = Percentile(damages, 0.75),
                P90 = Percentile(damages, 0.90),
                P95 = Percentile(damages, 0.95)
            });
        }

        return results;
    }

    private static double SimulateRound(List<Attack> attacks, ref bool nextAttackHasAdvantage, bool isFirstRoundOfCombat, ref int actionSurgesRemaining)
    {
        var totalDamage = 0.0;
        var targetIsProne = false;

        totalDamage += SimulateAttackSequence(
            attacks,
            ref nextAttackHasAdvantage,
            ref targetIsProne,
            isFirstRoundOfCombat,
            includeOnlyActionAttacks: false,
            ignoreSetup: false);

        if (actionSurgesRemaining > 0 && attacks.Any(IsActionAttack))
        {
            totalDamage += SimulateAttackSequence(
                attacks,
                ref nextAttackHasAdvantage,
                ref targetIsProne,
                isFirstRoundOfCombat: false,
                includeOnlyActionAttacks: true,
                ignoreSetup: true);
            actionSurgesRemaining--;
        }

        return totalDamage;
    }

    private static double SimulateAttackSequence(
        List<Attack> attacks,
        ref bool nextAttackHasAdvantage,
        ref bool targetIsProne,
        bool isFirstRoundOfCombat,
        bool includeOnlyActionAttacks,
        bool ignoreSetup)
    {
        var totalDamage = 0.0;

        foreach (var attack in attacks)
        {
            if (includeOnlyActionAttacks && !IsActionAttack(attack))
                continue;

            if (!ignoreSetup && isFirstRoundOfCombat && attack.RequiresSetup)
                continue;

            var hasAdvantage = nextAttackHasAdvantage || targetIsProne;
            // Vex advantage is consumed after one use; prone persists
            if (nextAttackHasAdvantage && !targetIsProne)
                nextAttackHasAdvantage = false;

            var hitPct = attack.HitPercent / 100.0;
            var critPct = attack.CritPercent / 100.0;

            if (hasAdvantage)
            {
                var totalHitPct = hitPct; // HitPercent includes crits
                var effectiveTotal = 1.0 - (1.0 - totalHitPct) * (1.0 - totalHitPct);

                if (totalHitPct > 0)
                {
                    var ratio = critPct / totalHitPct;
                    critPct = effectiveTotal * ratio;
                    hitPct = effectiveTotal;
                }
            }

            var normalHitPct = hitPct - critPct;
            var roll = Random.Shared.NextDouble();

            if (roll < critPct)
            {
                // Crit: double dice quantity, same flat modifier
                totalDamage += RollDamage(attack, isCrit: true);

                if (attack.MasteryVex)
                    nextAttackHasAdvantage = true;

                if (attack.MasteryTopple)
                    TryTopple(attack, ref targetIsProne);
            }
            else if (roll < critPct + normalHitPct)
            {
                // Normal hit
                totalDamage += RollDamage(attack, isCrit: false);

                if (attack.MasteryVex)
                    nextAttackHasAdvantage = true;

                if (attack.MasteryTopple)
                    TryTopple(attack, ref targetIsProne);
            }
            else
            {
                // Miss
                if (attack.MasteryGraze && attack.GrazeValue > 0)
                    totalDamage += attack.GrazeValue;
            }
        }

        return totalDamage;
    }

    private static bool IsActionAttack(Attack attack) =>
        string.Equals(attack.ActionType, "action", StringComparison.OrdinalIgnoreCase);

    private static double RollDamage(Attack attack, bool isCrit)
    {
        var total = 0;

        foreach (var dg in attack.DiceGroups)
        {
            var qty = isCrit ? dg.Quantity * 2 : dg.Quantity;
            for (var i = 0; i < qty; i++)
                total += Random.Shared.Next(1, dg.DieSize + 1);
        }

        total += attack.FlatModifier;
        return total;
    }

    private static void TryTopple(Attack attack, ref bool targetIsProne)
    {
        if (targetIsProne) return; // Already prone
        var toppleRoll = Random.Shared.NextDouble();
        if (toppleRoll < attack.TopplePercent / 100.0)
            targetIsProne = true;
    }

    private static double Percentile(double[] sorted, double p)
    {
        if (sorted.Length == 0) return 0;
        if (sorted.Length == 1) return sorted[0];

        var index = p * (sorted.Length - 1);
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);

        if (lower == upper) return sorted[lower];

        var fraction = index - lower;
        return sorted[lower] + (sorted[upper] - sorted[lower]) * fraction;
    }
}
