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
        var results = new List<LevelStats>();

        foreach (var level in character.Levels)
        {
            var damages = new double[iterations];
            for (var i = 0; i < iterations; i++)
                damages[i] = SimulateTurn(level.Attacks);

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

    private static double SimulateTurn(List<Attack> attacks)
    {
        var totalDamage = 0.0;
        var nextAttackHasAdvantage = false;
        var targetIsProne = false;

        foreach (var attack in attacks)
        {
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

                if (attack.MasteryTopple)
                    TryTopple(attack, ref targetIsProne);
            }
            else if (roll < critPct + normalHitPct)
            {
                // Normal hit
                totalDamage += RollDamage(attack, isCrit: false);

                if (attack.MasteryTopple)
                    TryTopple(attack, ref targetIsProne);
            }
            else
            {
                // Miss
                if (attack.MasteryVex)
                    nextAttackHasAdvantage = true;
                
                if (attack.MasteryGraze && attack.GrazeValue > 0)
                    totalDamage += attack.GrazeValue;
            }
        }

        return totalDamage;
    }

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
