using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Transmuter.TransmuterCode.Powers;
using Transmuter.TransmuterCode.Relics;

namespace Transmuter.TransmuterCode.Util;

/// <summary>
/// The three primary alchemical Tria Prima Reagents.
/// </summary>
public enum ReagentType
{
    Salt,
    Sulfur,
    Mercury
}

/// <summary>
/// Core alchemical state machine that manages Reagent application,
/// 2-Reagent Reactions (Detonate, Calcify, Dissolve), Stabilize, and Magnum Opus.
/// </summary>
public static class ReactionEngine
{
    /// <summary>
    /// Applies stacks of the specified Reagent to <paramref name="target"/>, adding any
    /// <see cref="EmeraldTabletPower"/> bonus on <paramref name="applier"/>, and resolves Reactions.
    /// </summary>
    public static async Task ApplyReagent(
        PlayerChoiceContext ctx,
        Creature target,
        Creature? applier,
        ReagentType type,
        int amount,
        bool resolveReactions = true)
    {
        await ApplyReagentInternal(
            ctx,
            target,
            applier,
            type,
            amount,
            includeEmeraldBonus: true,
            resolveReactions: resolveReactions);
    }

    private static async Task ApplyReagentInternal(
        PlayerChoiceContext ctx,
        Creature target,
        Creature? applier,
        ReagentType type,
        int amount,
        bool includeEmeraldBonus,
        bool resolveReactions = true)
    {
        if (amount <= 0 || !target.IsAlive)
            return;

        int bonus = 0;
        if (includeEmeraldBonus && applier?.GetPower<EmeraldTabletPower>() is { Amount: > 0 } emeraldTablet)
        {
            emeraldTablet.Flash();
            bonus = emeraldTablet.Amount;
        }

        int finalAmount = amount + bonus;
        if (finalAmount <= 0)
            return;

        switch (type)
        {
            case ReagentType.Salt:
                await PowerCmd.Apply<SaltPower>(ctx, target, finalAmount, applier, null);
                break;
            case ReagentType.Sulfur:
                await PowerCmd.Apply<SulfurPower>(ctx, target, finalAmount, applier, null);
                break;
            case ReagentType.Mercury:
                await PowerCmd.Apply<MercuryPower>(ctx, target, finalAmount, applier, null);
                break;
        }

        if (resolveReactions)
        {
            await ResolveReactions(ctx, target, applier);
        }
    }

    /// <summary>
    /// Evaluates current Reagents on <paramref name="target"/> and triggers any applicable
    /// 2-Reagent Reaction or the 3-Reagent Magnum Opus if Stabilized.
    /// </summary>
    public static async Task ResolveReactions(
        PlayerChoiceContext ctx,
        Creature target,
        Creature? applier)
    {
        if (!target.IsAlive)
            return;

        int distinct = GetDistinctReagentTypes(target);
        if (distinct < 2)
            return;

        if (target.HasPower<StabilizedPower>())
        {
            if (distinct >= 3)
            {
                await TriggerMagnumOpus(ctx, target, applier);
            }
            return;
        }

        bool hasSulfur = GetReagentAmount(target, ReagentType.Sulfur) > 0;
        bool hasMercury = GetReagentAmount(target, ReagentType.Mercury) > 0;
        bool hasSalt = GetReagentAmount(target, ReagentType.Salt) > 0;

        ReagentType? firstReagent = GetFirstExistingReagentType(target);
        int potency = GetTotalReagentCount(target);
        if (potency <= 0)
            return;

        await ConsumeAllReagents(ctx, target, applier, firstReagent);

        int triggers = GetReactionTriggerCount(applier);
        for (int i = 0; i < triggers; i++)
        {
            if (hasSulfur && hasMercury)
            {
                await ExecuteDetonate(ctx, target, applier, potency);
            }

            if (hasSalt && hasSulfur)
            {
                await ExecuteCalcify(ctx, target, applier, potency);
            }

            if (hasSalt && hasMercury)
            {
                await ExecuteDissolve(ctx, target, applier, potency);
            }
        }

        await ExecutePostReactionHooks(ctx, target, applier, firstReagent);
    }

    /// <summary>
    /// Applies <see cref="StabilizedPower"/> to <paramref name="target"/>, pausing 2-Reagent Reactions
    /// until turn end or until all 3 Reagents trigger Magnum Opus.
    /// </summary>
    public static async Task ApplyStabilize(
        PlayerChoiceContext ctx,
        Creature target,
        Creature? applier,
        int amount = 1)
    {
        if (amount <= 0 || !target.IsAlive)
            return;

        await PowerCmd.Apply<StabilizedPower>(ctx, target, amount, applier, null);
        await ResolveReactions(ctx, target, applier);
    }

    /// <summary>
    /// Consumes all Salt, Sulfur, Mercury, and <see cref="StabilizedPower"/> on <paramref name="target"/>
    /// and fires Detonate, Calcify, and Dissolve simultaneously at full combined potency X.
    /// Requires all 3 distinct Reagents to be present.
    /// </summary>
    public static async Task TriggerMagnumOpus(
        PlayerChoiceContext ctx,
        Creature target,
        Creature? applier)
    {
        if (!target.IsAlive || GetDistinctReagentTypes(target) < 3)
            return;

        int potency = GetTotalReagentCount(target);
        if (potency <= 0)
            return;

        ReagentType? firstReagent = GetFirstExistingReagentType(target);

        if (target.GetPower<StabilizedPower>() is { } stabilized)
        {
            await PowerCmd.Remove(stabilized);
        }

        await ConsumeAllReagents(ctx, target, applier, firstReagent);

        int triggers = GetReactionTriggerCount(applier);
        for (int i = 0; i < triggers; i++)
        {
            await ExecuteDetonate(ctx, target, applier, potency);
            await ExecuteCalcify(ctx, target, applier, potency);
            await ExecuteDissolve(ctx, target, applier, potency);
        }

        await ExecutePostReactionHooks(ctx, target, applier, firstReagent);
    }

    /// <summary>
    /// Returns the stack count of a specific <see cref="ReagentType"/> on <paramref name="target"/>.
    /// </summary>
    public static int GetReagentAmount(Creature target, ReagentType type)
    {
        return type switch
        {
            ReagentType.Salt => target.GetPower<SaltPower>()?.Amount ?? 0,
            ReagentType.Sulfur => target.GetPower<SulfurPower>()?.Amount ?? 0,
            ReagentType.Mercury => target.GetPower<MercuryPower>()?.Amount ?? 0,
            _ => 0
        };
    }

    /// <summary>
    /// Returns the total combined stacks of Salt, Sulfur, and Mercury on <paramref name="target"/>.
    /// </summary>
    public static int GetTotalReagentCount(Creature target)
    {
        return GetReagentAmount(target, ReagentType.Salt)
             + GetReagentAmount(target, ReagentType.Sulfur)
             + GetReagentAmount(target, ReagentType.Mercury);
    }

    /// <summary>
    /// Returns how many distinct Reagent types (0 to 3) are currently present on <paramref name="target"/>.
    /// </summary>
    public static int GetDistinctReagentTypes(Creature target)
    {
        int count = 0;
        if (GetReagentAmount(target, ReagentType.Salt) > 0) count++;
        if (GetReagentAmount(target, ReagentType.Sulfur) > 0) count++;
        if (GetReagentAmount(target, ReagentType.Mercury) > 0) count++;
        return count;
    }

    /// <summary>
    /// Multiplies the stacks of a single Reagent on <paramref name="target"/> if and only if
    /// exactly 1 Reagent type is currently present on <paramref name="target"/>.
    /// </summary>
    public static async Task DoubleSingleReagent(
        PlayerChoiceContext ctx,
        Creature target,
        Creature? applier,
        int multiplier = 2)
    {
        if (!target.IsAlive || multiplier <= 1 || GetDistinctReagentTypes(target) != 1)
            return;

        PowerModel? singlePower =
            (PowerModel?)target.GetPower<SaltPower>()
            ?? (PowerModel?)target.GetPower<SulfurPower>()
            ?? target.GetPower<MercuryPower>();

        if (singlePower is { Amount: > 0 })
        {
            int delta = singlePower.Amount * (multiplier - 1);
            if (delta > 0)
            {
                await PowerCmd.ModifyAmount(ctx, singlePower, delta, applier, null);
            }
        }
    }

    /// <summary>
    /// Converts all stacks of one Reagent on <paramref name="target"/> into another Reagent,
    /// then adds <paramref name="bonusStacks"/> of the new Reagent.
    /// </summary>
    public static async Task ConvertReagents(
        PlayerChoiceContext ctx,
        Creature target,
        Creature? applier,
        int bonusStacks)
    {
        if (!target.IsAlive)
            return;

        ReagentType? firstType = GetFirstExistingReagentType(target);
        if (firstType == null)
        {
            if (bonusStacks > 0)
            {
                await ApplyReagent(ctx, target, applier, ReagentType.Sulfur, bonusStacks);
            }
            return;
        }

        int existingAmount = GetReagentAmount(target, firstType.Value);
        PowerModel? powerToRemove = GetReagentPower(target, firstType.Value);
        if (powerToRemove != null)
        {
            await PowerCmd.Remove(powerToRemove);
        }

        ReagentType convertedType = firstType.Value switch
        {
            ReagentType.Sulfur => ReagentType.Mercury,
            ReagentType.Mercury => ReagentType.Salt,
            _ => ReagentType.Sulfur
        };

        int totalNewStacks = existingAmount + Math.Max(0, bonusStacks);
        if (totalNewStacks > 0)
        {
            await ApplyReagent(ctx, target, applier, convertedType, totalNewStacks);
        }
    }

    /// <summary>
    /// Triggers a Reaction on <paramref name="target"/> if it has 2+ stacks of a single Reagent,
    /// treating the reaction as if 1 stack of Salt was applied. Returns true if a Reaction triggered.
    /// </summary>
    public static async Task<bool> SelfReactSingleReagent(
        PlayerChoiceContext ctx,
        Creature target,
        Creature? applier)
    {
        if (!target.IsAlive || GetDistinctReagentTypes(target) != 1)
            return false;

        ReagentType? activeType = GetFirstExistingReagentType(target);
        if (activeType == null)
            return false;

        int currentStacks = GetReagentAmount(target, activeType.Value);
        if (currentStacks < 2)
            return false;

        int potency = currentStacks + 1;
        await ConsumeAllReagents(ctx, target, applier, activeType);

        int triggers = GetReactionTriggerCount(applier);
        for (int i = 0; i < triggers; i++)
        {
            if (activeType.Value == ReagentType.Mercury)
            {
                await ExecuteDissolve(ctx, target, applier, potency);
            }
            else
            {
                await ExecuteCalcify(ctx, target, applier, potency);
            }
        }

        await ExecutePostReactionHooks(ctx, target, applier, activeType);
        return true;
    }

    private static int GetReactionTriggerCount(Creature? applier)
    {
        int extraTriggers = 0;
        if (applier?.GetPower<PhilosophersEnginePower>() is { Amount: > 0 } engine)
        {
            engine.Flash();
            extraTriggers = engine.Amount;
        }
        return 1 + extraTriggers;
    }

    private static ReagentType? GetFirstExistingReagentType(Creature target)
    {
        foreach (PowerModel power in target.Powers)
        {
            if (power.Amount <= 0)
                continue;

            if (power is SaltPower)
                return ReagentType.Salt;
            if (power is SulfurPower)
                return ReagentType.Sulfur;
            if (power is MercuryPower)
                return ReagentType.Mercury;
        }
        return null;
    }

    private static PowerModel? GetReagentPower(Creature target, ReagentType type)
    {
        return type switch
        {
            ReagentType.Salt => target.GetPower<SaltPower>(),
            ReagentType.Sulfur => target.GetPower<SulfurPower>(),
            ReagentType.Mercury => target.GetPower<MercuryPower>(),
            _ => null
        };
    }

    private static async Task ConsumeAllReagents(
        PlayerChoiceContext ctx,
        Creature target,
        Creature? applier,
        ReagentType? firstReagent)
    {
        ParacelsusScalpel? scalpel = applier?.Player?.GetRelic<ParacelsusScalpel>();
        bool leaveFirstBehind = scalpel != null && firstReagent.HasValue;
        if (leaveFirstBehind)
        {
            scalpel!.Flash();
        }

        PowerModel? retainedPower = leaveFirstBehind ? GetReagentPower(target, firstReagent!.Value) : null;

        PowerModel?[] reagentPowers =
        [
            target.GetPower<SaltPower>(),
            target.GetPower<SulfurPower>(),
            target.GetPower<MercuryPower>()
        ];

        foreach (PowerModel? power in reagentPowers)
        {
            if (power == null || power.Amount <= 0)
                continue;

            if (ReferenceEquals(power, retainedPower))
            {
                int excess = power.Amount - 1;
                if (excess > 0)
                {
                    await PowerCmd.ModifyAmount(ctx, power, -excess, applier, null);
                }
            }
            else
            {
                await PowerCmd.Remove(power);
            }
        }
    }

    private static async Task ExecuteDetonate(
        PlayerChoiceContext ctx,
        Creature target,
        Creature? applier,
        int potency)
    {
        if (target.IsAlive)
        {
            await CreatureCmd.Damage(
                ctx,
                target,
                3 * potency,
                ValueProp.Unpowered,
                applier,
                null);
        }

        var combatState = target.CombatState ?? applier?.CombatState;
        if (combatState == null)
            return;

        List<Creature> otherEnemies = combatState.HittableEnemies
            .Where(e => e != target && e.IsAlive)
            .ToList();

        if (otherEnemies.Count == 0)
            return;

        await CreatureCmd.Damage(
            ctx,
            otherEnemies,
            2 * potency,
            ValueProp.Unpowered,
            applier,
            null);

        if (applier?.GetPower<ChainDetonationPower>() is { Amount: > 0 } chainDetonation)
        {
            chainDetonation.Flash();
            int chainSulfur = chainDetonation.Amount;
            foreach (Creature other in otherEnemies.Where(e => e.IsAlive))
            {
                await ApplyReagent(ctx, other, applier, ReagentType.Sulfur, chainSulfur);
            }
        }
    }

    private static async Task ExecuteCalcify(
        PlayerChoiceContext ctx,
        Creature target,
        Creature? applier,
        int potency)
    {
        if (applier is { IsAlive: true })
        {
            await CreatureCmd.GainBlock(applier, 2 * potency, ValueProp.Unpowered, null);
        }

        int weakStacks = (int)Math.Ceiling(potency / 3.0);
        if (weakStacks > 0 && target.IsAlive)
        {
            await PowerCmd.Apply<WeakPower>(ctx, target, weakStacks, applier, null);
        }
    }

    private static async Task ExecuteDissolve(
        PlayerChoiceContext ctx,
        Creature target,
        Creature? applier,
        int potency)
    {
        if (target.IsAlive && target.Block > 0)
        {
            await CreatureCmd.Damage(
                ctx,
                target,
                target.Block,
                ValueProp.Unpowered | ValueProp.SkipHurtAnim,
                applier,
                null);
        }

        int vulnStacks = (int)Math.Ceiling(potency / 2.0);
        if (vulnStacks > 0 && target.IsAlive)
        {
            await PowerCmd.Apply<VulnerablePower>(ctx, target, vulnStacks, applier, null);
        }

        int drawCount = potency >= 6 ? 2 : 1;
        if (applier?.Player != null)
        {
            await CardPileCmd.Draw(ctx, drawCount, applier.Player);
        }
    }

    private static async Task ExecutePostReactionHooks(
        PlayerChoiceContext ctx,
        Creature target,
        Creature? applier,
        ReagentType? firstConsumed)
    {
        if (applier?.Player?.GetRelic<CrackedAlembic>() is { } alembic)
        {
            await alembic.OnReactionTriggered(ctx);
        }

        if (applier is { IsAlive: true } && applier.GetPower<CrucibleShieldPower>() is { Amount: > 0 } crucibleShield)
        {
            crucibleShield.Flash();
            await CreatureCmd.GainBlock(applier, crucibleShield.Amount, ValueProp.Unpowered, null);
        }

        if (firstConsumed.HasValue && target.IsAlive &&
            applier?.GetPower<ResidualPrecipitatePower>() is { Amount: > 0 } residualPrecipitate)
        {
            residualPrecipitate.Flash();
            await ApplyReagentInternal(
                ctx,
                target,
                applier,
                firstConsumed.Value,
                residualPrecipitate.Amount,
                includeEmeraldBonus: false,
                resolveReactions: false);
        }
    }
}
