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
                var hasExtraActionSurge = level.Resources?.HasExtraActionSurge == true;
                var hasShieldMaster = level.Resources?.HasShieldMaster == true;
                var hasHeroicInspiration = level.Resources?.HasHeroicInspiration == true;
                var hasStudiedAttacks = level.Resources?.HasStudiedAttacks == true;
                var hasBoonOfCombatProwess = level.Resources?.HasBoonOfCombatProwess == true;
                var hasPureAdvantage = level.Resources?.HasPureAdvantage == true;
                var hasSurprisingStrikes = level.Resources?.HasSurprisingStrikes == true;
                var hasDeathStrikes = level.Resources?.HasDeathStrikes == true;
                var shieldMasterTopplePercent = level.Resources?.ShieldMasterTopplePercent ?? 0;
                var pureAdvantagePercent = level.Resources?.PureAdvantagePercent ?? 0;
                var deathStrikesResistPercent = level.Resources?.DeathStrikesResistPercent ?? 0;
                var nextAttackHasAdvantage = false;
                var actionSurgesRemaining = hasActionSurge ? 1 : 0;
                var extraActionSurgesRemaining = hasExtraActionSurge ? 1 : 0;
                var studiedAttacksAdvantagePending = false;
                var studiedAttacksTurnsRemaining = 0;

                foreach (var combat in setting.Combats)
                {
                    var surprisingStrikesAvailableForCombat = hasSurprisingStrikes;
                    var deathStrikesAvailableForCombat = hasDeathStrikes;

                    for (var round = 0; round < Math.Max(1, combat.Rounds); round++)
                    {
                        if (round > 0)
                            nextAttackHasAdvantage = false;

                        damages[index++] = SimulateRound(
                            level.Attacks,
                            ref nextAttackHasAdvantage,
                            isFirstRoundOfCombat: round == 0,
                            ref actionSurgesRemaining,
                            ref extraActionSurgesRemaining,
                            hasShieldMaster,
                            shieldMasterTopplePercent,
                            hasHeroicInspiration,
                            hasStudiedAttacks,
                            hasBoonOfCombatProwess,
                            hasPureAdvantage,
                            pureAdvantagePercent,
                            level.LevelNumber,
                            ref surprisingStrikesAvailableForCombat,
                            deathStrikesResistPercent,
                            ref deathStrikesAvailableForCombat,
                            ref studiedAttacksAdvantagePending,
                            ref studiedAttacksTurnsRemaining);

                        if (studiedAttacksAdvantagePending && studiedAttacksTurnsRemaining > 0)
                        {
                            studiedAttacksTurnsRemaining--;
                            if (studiedAttacksTurnsRemaining <= 0)
                                studiedAttacksAdvantagePending = false;
                        }
                    }

                    nextAttackHasAdvantage = false;
                    if (combat.ShortRestAfter && hasActionSurge)
                        actionSurgesRemaining = 1;
                    if (combat.ShortRestAfter && hasExtraActionSurge)
                        extraActionSurgesRemaining = 1;
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

    private static double SimulateRound(
        List<Attack> attacks,
        ref bool nextAttackHasAdvantage,
        bool isFirstRoundOfCombat,
        ref int actionSurgesRemaining,
        ref int extraActionSurgesRemaining,
        bool hasShieldMaster,
        int shieldMasterTopplePercent,
        bool hasHeroicInspiration,
        bool hasStudiedAttacks,
        bool hasBoonOfCombatProwess,
        bool hasPureAdvantage,
        int pureAdvantagePercent,
        int levelNumber,
        ref bool surprisingStrikesAvailableForCombat,
        int deathStrikesResistPercent,
        ref bool deathStrikesAvailableForCombat,
        ref bool studiedAttacksAdvantagePending,
        ref int studiedAttacksTurnsRemaining)
    {
        var totalDamage = 0.0;
        var highestSuccessfulHitDamageThisRound = 0.0;
        var targetIsProne = false;
        var shieldMasterUsedThisTurn = false;
        var heroicInspirationAvailableThisTurn = hasHeroicInspiration;
        var boonOfCombatProwessAvailableThisTurn = hasBoonOfCombatProwess;
        var surgeUsedThisTurn = false;

        totalDamage += SimulateAttackSequence(
            attacks,
            ref nextAttackHasAdvantage,
            ref targetIsProne,
            isFirstRoundOfCombat,
            includeOnlyActionAttacks: false,
            ignoreSetup: false,
            hasShieldMaster,
            shieldMasterTopplePercent,
            ref shieldMasterUsedThisTurn,
            ref heroicInspirationAvailableThisTurn,
            hasStudiedAttacks,
            ref boonOfCombatProwessAvailableThisTurn,
            hasPureAdvantage,
            pureAdvantagePercent,
            levelNumber,
            ref surprisingStrikesAvailableForCombat,
            ref highestSuccessfulHitDamageThisRound,
            ref studiedAttacksAdvantagePending,
            ref studiedAttacksTurnsRemaining);

        if (!surgeUsedThisTurn && actionSurgesRemaining > 0 && attacks.Any(IsActionAttack))
        {
            totalDamage += SimulateAttackSequence(
                attacks,
                ref nextAttackHasAdvantage,
                ref targetIsProne,
                isFirstRoundOfCombat,
                includeOnlyActionAttacks: true,
                ignoreSetup: true,
                hasShieldMaster,
                shieldMasterTopplePercent,
                ref shieldMasterUsedThisTurn,
                ref heroicInspirationAvailableThisTurn,
                hasStudiedAttacks,
                ref boonOfCombatProwessAvailableThisTurn,
                hasPureAdvantage,
                pureAdvantagePercent,
                levelNumber,
                ref surprisingStrikesAvailableForCombat,
                ref highestSuccessfulHitDamageThisRound,
                ref studiedAttacksAdvantagePending,
                ref studiedAttacksTurnsRemaining);
            actionSurgesRemaining--;
            surgeUsedThisTurn = true;
        }

        if (!surgeUsedThisTurn && extraActionSurgesRemaining > 0 && attacks.Any(IsActionAttack))
        {
            totalDamage += SimulateAttackSequence(
                attacks,
                ref nextAttackHasAdvantage,
                ref targetIsProne,
                isFirstRoundOfCombat,
                includeOnlyActionAttacks: true,
                ignoreSetup: true,
                hasShieldMaster,
                shieldMasterTopplePercent,
                ref shieldMasterUsedThisTurn,
                ref heroicInspirationAvailableThisTurn,
                hasStudiedAttacks,
                ref boonOfCombatProwessAvailableThisTurn,
                hasPureAdvantage,
                pureAdvantagePercent,
                levelNumber,
                ref surprisingStrikesAvailableForCombat,
                ref highestSuccessfulHitDamageThisRound,
                ref studiedAttacksAdvantagePending,
                ref studiedAttacksTurnsRemaining);
            extraActionSurgesRemaining--;
        }

        TryApplyDeathStrikes(
            isFirstRoundOfCombat,
            deathStrikesResistPercent,
            highestSuccessfulHitDamageThisRound,
            ref deathStrikesAvailableForCombat,
            ref totalDamage);

        return totalDamage;
    }

    private static double SimulateAttackSequence(
        List<Attack> attacks,
        ref bool nextAttackHasAdvantage,
        ref bool targetIsProne,
        bool isFirstRoundOfCombat,
        bool includeOnlyActionAttacks,
        bool ignoreSetup,
        bool hasShieldMaster,
        int shieldMasterTopplePercent,
        ref bool shieldMasterUsedThisTurn,
        ref bool heroicInspirationAvailableThisTurn,
        bool hasStudiedAttacks,
        ref bool boonOfCombatProwessAvailableThisTurn,
        bool hasPureAdvantage,
        int pureAdvantagePercent,
        int levelNumber,
        ref bool surprisingStrikesAvailableForCombat,
        ref double highestSuccessfulHitDamageThisRound,
        ref bool studiedAttacksAdvantagePending,
        ref int studiedAttacksTurnsRemaining)
    {
        var totalDamage = 0.0;

        foreach (var attack in attacks)
        {
            if (includeOnlyActionAttacks && !IsActionAttack(attack))
                continue;
            if (IsTriggeredAttack(attack) && !TriggerOccurs(attack))
                continue;

            if (!ignoreSetup && isFirstRoundOfCombat && attack.RequiresSetup)
                continue;

            var hasAdvantage = nextAttackHasAdvantage || targetIsProne || studiedAttacksAdvantagePending;
            if (!hasAdvantage && hasPureAdvantage && Random.Shared.NextDouble() < Math.Clamp(pureAdvantagePercent, 0, 100) / 100.0)
                hasAdvantage = true;
            // Vex advantage is consumed after one use; prone persists
            if (nextAttackHasAdvantage && !targetIsProne)
                nextAttackHasAdvantage = false;
            if (studiedAttacksAdvantagePending)
            {
                studiedAttacksAdvantagePending = false;
                studiedAttacksTurnsRemaining = 0;
            }

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
            var isHit = RollHits(critPct, normalHitPct, out var isCrit);
            if (!isHit && boonOfCombatProwessAvailableThisTurn)
            {
                boonOfCombatProwessAvailableThisTurn = false;
                isHit = true;
                isCrit = false;
            }
            else if (!isHit && heroicInspirationAvailableThisTurn)
            {
                heroicInspirationAvailableThisTurn = false;
                isHit = RollHits(critPct, normalHitPct, out isCrit);
            }

            if (isHit && isCrit)
            {
                // Crit: double dice quantity, same flat modifier
                var attackDamage = RollDamage(attack, isCrit: true);
                TryApplySurprisingStrikes(isFirstRoundOfCombat, levelNumber, ref surprisingStrikesAvailableForCombat, ref attackDamage);
                totalDamage += attackDamage;
                TrackHighestSuccessfulHitDamage(attackDamage, ref highestSuccessfulHitDamageThisRound);
                TryShieldMasterTopple(hasShieldMaster, shieldMasterTopplePercent, ref shieldMasterUsedThisTurn, ref targetIsProne);

                if (attack.MasteryVex)
                    nextAttackHasAdvantage = true;

                if (attack.MasteryTopple)
                    TryTopple(attack, ref targetIsProne);
            }
            else if (isHit)
            {
                // Normal hit
                var attackDamage = RollDamage(attack, isCrit: false);
                TryApplySurprisingStrikes(isFirstRoundOfCombat, levelNumber, ref surprisingStrikesAvailableForCombat, ref attackDamage);
                totalDamage += attackDamage;
                TrackHighestSuccessfulHitDamage(attackDamage, ref highestSuccessfulHitDamageThisRound);
                TryShieldMasterTopple(hasShieldMaster, shieldMasterTopplePercent, ref shieldMasterUsedThisTurn, ref targetIsProne);

                if (attack.MasteryVex)
                    nextAttackHasAdvantage = true;

                if (attack.MasteryTopple)
                    TryTopple(attack, ref targetIsProne);
            }
            else
            {
                // Miss
                if (hasStudiedAttacks)
                {
                    studiedAttacksAdvantagePending = true;
                    studiedAttacksTurnsRemaining = 2;
                }

                if (attack.MasteryGraze && attack.GrazeValue > 0)
                    totalDamage += attack.GrazeValue;
            }
        }

        return totalDamage;
    }

    private static bool RollHits(double critPct, double normalHitPct, out bool isCrit)
    {
        var roll = Random.Shared.NextDouble();
        if (roll < critPct)
        {
            isCrit = true;
            return true;
        }

        if (roll < critPct + normalHitPct)
        {
            isCrit = false;
            return true;
        }

        isCrit = false;
        return false;
    }

    private static bool IsActionAttack(Attack attack) =>
        string.Equals(attack.ActionType, "action", StringComparison.OrdinalIgnoreCase);

    private static bool IsReactionAttack(Attack attack) =>
        string.Equals(attack.ActionType, "reaction", StringComparison.OrdinalIgnoreCase);

    private static bool IsBonusActionAttack(Attack attack) =>
        string.Equals(attack.ActionType, "bonus_action", StringComparison.OrdinalIgnoreCase);

    private static bool IsTriggeredAttack(Attack attack) =>
        IsReactionAttack(attack) || IsBonusActionAttack(attack);

    private static bool TriggerOccurs(Attack attack)
    {
        var chance = Math.Clamp(attack.ReactionChancePercent, 0, 100) / 100.0;
        return Random.Shared.NextDouble() < chance;
    }

    private static void TryApplySurprisingStrikes(
        bool isFirstRoundOfCombat,
        int levelNumber,
        ref bool surprisingStrikesAvailableForCombat,
        ref double totalDamage)
    {
        if (!isFirstRoundOfCombat || !surprisingStrikesAvailableForCombat)
            return;

        totalDamage += levelNumber;
        surprisingStrikesAvailableForCombat = false;
    }

    private static void TrackHighestSuccessfulHitDamage(double attackDamage, ref double highestSuccessfulHitDamageThisRound)
    {
        if (attackDamage > highestSuccessfulHitDamageThisRound)
            highestSuccessfulHitDamageThisRound = attackDamage;
    }

    private static void TryApplyDeathStrikes(
        bool isFirstRoundOfCombat,
        int deathStrikesResistPercent,
        double highestSuccessfulHitDamageThisRound,
        ref bool deathStrikesAvailableForCombat,
        ref double totalDamage)
    {
        if (!isFirstRoundOfCombat || !deathStrikesAvailableForCombat)
            return;

        deathStrikesAvailableForCombat = false;
        if (highestSuccessfulHitDamageThisRound <= 0)
            return;
        if (DeathStrikesResisted(deathStrikesResistPercent))
            return;

        totalDamage += highestSuccessfulHitDamageThisRound;
    }

    private static bool DeathStrikesResisted(int deathStrikesResistPercent)
    {
        var chance = Math.Clamp(deathStrikesResistPercent, 0, 100) / 100.0;
        return Random.Shared.NextDouble() < chance;
    }

    private static void TryShieldMasterTopple(
        bool hasShieldMaster,
        int shieldMasterTopplePercent,
        ref bool shieldMasterUsedThisTurn,
        ref bool targetIsProne)
    {
        if (!hasShieldMaster || shieldMasterUsedThisTurn)
            return;

        shieldMasterUsedThisTurn = true;
        TryTopplePercent(shieldMasterTopplePercent, ref targetIsProne);
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
        TryTopplePercent(attack.TopplePercent, ref targetIsProne);
    }

    private static void TryTopplePercent(int topplePercent, ref bool targetIsProne)
    {
        if (targetIsProne) return; // Already prone
        var toppleRoll = Random.Shared.NextDouble();
        if (toppleRoll < Math.Clamp(topplePercent, 0, 100) / 100.0)
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