using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using Usurer.UsurerCode.Powers;
using Usurer.UsurerCode.Relics;

namespace Usurer.UsurerCode.Util;

/// <summary>
/// Core financial combat state machine for The Usurer.
/// Manages Borrowing and Repaying Debt, Over-Leveraged threshold checks (10+ Debt),
/// Lien application and Foreclosure, Moratorium pauses, and turn-end Interest/Installments.
/// </summary>
public static class DebtEngine
{
    public const int OverLeveragedThreshold = 10;

    /// <summary>
    /// Returns the current stacks of <see cref="DebtPower"/> on <paramref name="creature"/>.
    /// </summary>
    public static int GetDebtAmount(Creature? creature) =>
        creature?.GetPower<DebtPower>()?.Amount ?? 0;

    /// <summary>
    /// Returns true if <paramref name="creature"/> has at least <see cref="OverLeveragedThreshold"/> (10) Debt.
    /// </summary>
    public static bool IsOverLeveraged(Creature? creature) =>
        GetDebtAmount(creature) >= OverLeveragedThreshold;

    /// <summary>
    /// Returns the current stacks of <see cref="LienPower"/> on <paramref name="creature"/>.
    /// </summary>
    public static int GetLienAmount(Creature? creature) =>
        creature?.GetPower<LienPower>()?.Amount ?? 0;

    /// <summary>
    /// Returns the current stacks of <see cref="MoratoriumPower"/> on <paramref name="creature"/>.
    /// </summary>
    public static int GetMoratoriumAmount(Creature? creature) =>
        creature?.GetPower<MoratoriumPower>()?.Amount ?? 0;

    /// <summary>
    /// Applies <paramref name="amount"/> stacks of <see cref="DebtPower"/> to <paramref name="borrower"/>,
    /// triggers <see cref="GoldenScalesPower"/> if active, and checks <see cref="InfernalLedger"/>.
    /// </summary>
    public static async Task BorrowDebt(
        PlayerChoiceContext ctx,
        Creature? borrower,
        int amount)
    {
        if (amount <= 0 || borrower is not { IsAlive: true })
            return;

        await PowerCmd.Apply<DebtPower>(ctx, borrower, amount, borrower, null);

        if (borrower.GetPower<GoldenScalesPower>() is { Amount: > 0 } goldenScales)
        {
            var combatState = borrower.CombatState;
            if (combatState != null)
            {
                List<Creature> livingEnemies = combatState.HittableEnemies
                    .Where(e => e.IsAlive)
                    .ToList();

                if (livingEnemies.Count > 0)
                {
                    Creature target = borrower.Player?.RunState.Rng.CombatTargets.NextItem(livingEnemies)
                        ?? livingEnemies[0];
                    goldenScales.Flash();
                    await ApplyLien(ctx, target, borrower, goldenScales.Amount);
                }
            }
        }

        if (IsOverLeveraged(borrower) && borrower.Player?.GetRelic<InfernalLedger>() is { } ledger)
        {
            await ledger.OnThresholdOrSettledReached(ctx);
        }
    }

    /// <summary>
    /// Removes up to <paramref name="amount"/> stacks of <see cref="DebtPower"/> from <paramref name="borrower"/>,
    /// triggers <see cref="ShadowBankingPower"/> if any Debt was repaid, and checks <see cref="InfernalLedger"/>
    /// if Debt reached 0. Returns the exact amount of Debt repaid.
    /// </summary>
    public static async Task<int> RepayDebt(
        PlayerChoiceContext ctx,
        Creature? borrower,
        int amount)
    {
        if (amount <= 0 || borrower is not { IsAlive: true })
            return 0;

        DebtPower? debtPower = borrower.GetPower<DebtPower>();
        if (debtPower is not { Amount: > 0 })
            return 0;

        int currentDebt = debtPower.Amount;
        int actualRepaid = Math.Min(currentDebt, amount);
        if (actualRepaid <= 0)
            return 0;

        if (actualRepaid >= currentDebt)
        {
            await PowerCmd.Remove(debtPower);
        }
        else
        {
            await PowerCmd.ModifyAmount(ctx, debtPower, -actualRepaid, borrower, null);
        }

        if (borrower.GetPower<ShadowBankingPower>() is { Amount: > 0 } shadowBanking)
        {
            shadowBanking.Flash();
            await CreatureCmd.GainBlock(borrower, shadowBanking.Amount, ValueProp.Unpowered, null);
        }

        if (GetDebtAmount(borrower) == 0 && borrower.Player?.GetRelic<InfernalLedger>() is { } ledger)
        {
            await ledger.OnThresholdOrSettledReached(ctx);
        }

        return actualRepaid;
    }

    /// <summary>
    /// Applies <paramref name="amount"/> stacks of <see cref="LienPower"/> to <paramref name="target"/>
    /// and triggers <see cref="GarnishWagesPower"/> on <paramref name="applier"/> if active.
    /// </summary>
    public static async Task ApplyLien(
        PlayerChoiceContext ctx,
        Creature target,
        Creature? applier,
        int amount)
    {
        if (amount <= 0 || !target.IsAlive)
            return;

        await PowerCmd.Apply<LienPower>(ctx, target, amount, applier, null);

        if (applier is { IsAlive: true } && applier.GetPower<GarnishWagesPower>() is { Amount: > 0 } garnishWages)
        {
            garnishWages.Flash();
            await CreatureCmd.GainBlock(applier, garnishWages.Amount, ValueProp.Unpowered, null);
        }
    }

    /// <summary>
    /// Applies <paramref name="amount"/> stacks of <see cref="MoratoriumPower"/> to <paramref name="target"/>,
    /// pausing turn-end Debt Interest and Installment damage.
    /// </summary>
    public static async Task ApplyMoratorium(
        PlayerChoiceContext ctx,
        Creature? target,
        int amount = 1)
    {
        if (amount <= 0 || target is not { IsAlive: true })
            return;

        await PowerCmd.Apply<MoratoriumPower>(ctx, target, amount, target, null);
    }

    /// <summary>
    /// Multiplies the existing <see cref="LienPower"/> stacks on <paramref name="target"/> by <paramref name="multiplier"/>.
    /// </summary>
    public static async Task DoubleLien(
        PlayerChoiceContext ctx,
        Creature target,
        Creature? applier,
        int multiplier = 2)
    {
        if (!target.IsAlive || multiplier <= 1)
            return;

        int existing = GetLienAmount(target);
        if (existing <= 0)
            return;

        int delta = existing * (multiplier - 1);
        if (delta > 0)
        {
            await ApplyLien(ctx, target, applier, delta);
        }
    }

    /// <summary>
    /// Consumes all <see cref="LienPower"/> on <paramref name="target"/>, dealing
    /// <paramref name="damagePerLien"/> unpowered damage per stack and repaying
    /// <paramref name="repayPerLien"/> Debt per stack on <paramref name="applier"/>.
    /// </summary>
    public static async Task<int> ForecloseLien(
        PlayerChoiceContext ctx,
        Creature target,
        Creature? applier,
        int damagePerLien,
        int repayPerLien = 1)
    {
        if (!target.IsAlive)
            return 0;

        LienPower? lienPower = target.GetPower<LienPower>();
        if (lienPower is not { Amount: > 0 })
            return 0;

        int consumed = lienPower.Amount;
        await PowerCmd.Remove(lienPower);

        int totalDamage = consumed * damagePerLien;
        if (totalDamage > 0 && target.IsAlive)
        {
            await CreatureCmd.Damage(
                ctx,
                target,
                totalDamage,
                ValueProp.Unpowered,
                applier,
                null);
        }

        int totalRepay = consumed * repayPerLien;
        if (totalRepay > 0 && applier is { IsAlive: true })
        {
            await RepayDebt(ctx, applier, totalRepay);
        }

        return consumed;
    }

    /// <summary>
    /// Resolves turn-end Debt Interest (+20% rounded up) and Installment damage (Debt / 5 if Debt &gt;= 10,
    /// or redirected to all enemies if <see cref="SovereignDefaultPower"/> is active),
    /// unless paused by <see cref="MoratoriumPower"/>.
    /// </summary>
    public static async Task ResolveTurnEndDebt(
        PlayerChoiceContext ctx,
        Creature borrower)
    {
        if (!borrower.IsAlive)
            return;

        if (borrower.GetPower<MoratoriumPower>() is { Amount: > 0 } moratorium)
        {
            moratorium.Flash();
            if (moratorium.Amount > 1)
            {
                await PowerCmd.ModifyAmount(ctx, moratorium, -1, borrower, null);
            }
            else
            {
                await PowerCmd.Remove(moratorium);
            }
            return;
        }

        DebtPower? debtPower = borrower.GetPower<DebtPower>();
        if (debtPower is not { Amount: > 0 })
            return;

        debtPower.Flash();
        int interest = (int)Math.Ceiling(debtPower.Amount * 0.20);
        if (interest > 0)
        {
            await PowerCmd.ModifyAmount(ctx, debtPower, interest, borrower, null);
        }

        int totalDebt = GetDebtAmount(borrower);
        if (totalDebt < OverLeveragedThreshold)
            return;

        if (borrower.Player?.GetRelic<InfernalLedger>() is { } ledger)
        {
            await ledger.OnThresholdOrSettledReached(ctx);
        }

        if (borrower.GetPower<SovereignDefaultPower>() is { Amount: > 0 } sovereignDefault)
        {
            sovereignDefault.Flash();
            int aoeDamage = (int)Math.Ceiling(totalDebt / 2.0) * sovereignDefault.Amount;
            var combatState = borrower.CombatState;
            if (combatState != null && aoeDamage > 0)
            {
                List<Creature> enemies = combatState.HittableEnemies
                    .Where(e => e.IsAlive)
                    .ToList();

                if (enemies.Count > 0)
                {
                    await CreatureCmd.Damage(
                        ctx,
                        enemies,
                        aoeDamage,
                        ValueProp.Unpowered,
                        borrower,
                        null);
                }
            }
            return;
        }

        int installmentDamage = totalDebt / 5;
        if (installmentDamage > 0)
        {
            await CreatureCmd.Damage(
                ctx,
                borrower,
                installmentDamage,
                ValueProp.Unpowered,
                borrower,
                null);
        }
    }
}
