using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using Usurer.UsurerCode.Powers;
using Usurer.UsurerCode.Relics;

namespace Usurer.UsurerCode.Util;

/// <summary>
/// Core financial combat state machine for The Usurer.
/// Connects real in-game <see cref="Player.Gold"/> directly with <see cref="DebtPower"/>:
/// - Borrowing Debt immediately credits real Gold to the player's pouch.
/// - Standard Repayment spends real Gold to pay down Debt.
/// - Enemy-paid / forgiven Debt (via Liens, Foreclosure, Debt Collection, Debt Restructuring)
///   pays down Debt without spending the player's Gold, letting the player keep the borrowed Gold as profit.
/// - Unpaid Debt at the end of combat is settled from the player's Gold pouch.
/// </summary>
public static class DebtEngine
{
    public const int OverLeveragedThreshold = 10;
    public const int MaxNetLoanProfitPerCombat = 30;

    private static int _netCombatLoanedGold;

    /// <summary>
    /// Tracks net Gold loaned via <see cref="BorrowDebt"/> minus Gold spent via <see cref="RepayDebt"/>
    /// during the current combat encounter.
    /// </summary>
    public static int NetCombatLoanedGold => _netCombatLoanedGold;

    /// <summary>
    /// Resets the per-combat loan ledger at the start of each encounter.
    /// </summary>
    public static void ResetCombatLedger() => _netCombatLoanedGold = 0;

    /// <summary>
    /// Returns the current stacks of <see cref="DebtPower"/> on <paramref name="creature"/>.
    /// </summary>
    public static int GetDebtAmount(Creature? creature) =>
        creature?.GetPower<DebtPower>()?.Amount ?? 0;

    /// <summary>
    /// Returns the real in-game <see cref="Player.Gold"/> held by <paramref name="creature"/>'s owner.
    /// </summary>
    public static int GetGoldAmount(Creature? creature) =>
        creature?.Player?.Gold ?? 0;

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
    /// Loans <paramref name="amount"/> real <see cref="Player.Gold"/> to <paramref name="borrower"/>,
    /// applies <paramref name="amount"/> stacks of <see cref="DebtPower"/>,
    /// triggers <see cref="GoldenScalesPower"/> if active, and checks <see cref="InfernalLedger"/>.
    /// </summary>
    public static async Task BorrowDebt(
        PlayerChoiceContext ctx,
        Creature? borrower,
        int amount)
    {
        if (amount <= 0 || borrower is not { IsAlive: true })
            return;

        if (borrower.Player is { } player)
        {
            await PlayerCmd.GainGold(amount, player);
            _netCombatLoanedGold += amount;
        }

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
    /// Spends up to <paramref name="goldCost"/> real <see cref="Player.Gold"/> from <paramref name="borrower"/>;
    /// if the player has less than <paramref name="goldCost"/> Gold, borrows the shortfall as <see cref="DebtPower"/>.
    /// </summary>
    public static async Task SpendGoldOrBorrow(
        PlayerChoiceContext ctx,
        Creature? borrower,
        int goldCost)
    {
        if (goldCost <= 0 || borrower is not { IsAlive: true })
            return;

        int spentFromPouch = 0;
        if (borrower.Player is { Gold: > 0 } player)
        {
            spentFromPouch = Math.Min(player.Gold, goldCost);
            if (spentFromPouch > 0)
            {
                await PlayerCmd.LoseGold(spentFromPouch, player, GoldLossType.Spent);
            }
        }

        int shortfall = goldCost - spentFromPouch;
        if (shortfall > 0)
        {
            await PowerCmd.Apply<DebtPower>(ctx, borrower, shortfall, borrower, null);
            if (IsOverLeveraged(borrower) && borrower.Player?.GetRelic<InfernalLedger>() is { } ledger)
            {
                await ledger.OnThresholdOrSettledReached(ctx);
            }
        }
    }

    /// <summary>
    /// Removes up to <paramref name="amount"/> stacks of <see cref="DebtPower"/> from <paramref name="borrower"/>.
    /// When <paramref name="spendPlayerGold"/> is true (default), spends up to the repaid amount from
    /// <paramref name="borrower"/>'s real <see cref="Player.Gold"/>. When false (enemy-paid or forgiven Debt),
    /// reduces Debt without costing the player's Gold.
    /// Triggers <see cref="ShadowBankingPower"/> and checks <see cref="InfernalLedger"/> if Debt reaches 0.
    /// </summary>
    public static async Task<int> RepayDebt(
        PlayerChoiceContext ctx,
        Creature? borrower,
        int amount,
        bool spendPlayerGold = true)
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

        if (spendPlayerGold && borrower.Player is { Gold: > 0 } player)
        {
            int goldToSpend = Math.Min(player.Gold, actualRepaid);
            if (goldToSpend > 0)
            {
                await PlayerCmd.LoseGold(goldToSpend, player, GoldLossType.Spent);
                _netCombatLoanedGold = Math.Max(0, _netCombatLoanedGold - goldToSpend);
            }
        }

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
    /// Settles unpaid <see cref="DebtPower"/> from <paramref name="player"/>'s real <see cref="Player.Gold"/>
    /// at the end of combat, capping net unbacked loan profit per combat at <see cref="MaxNetLoanProfitPerCombat"/>.
    /// </summary>
    public static async Task SettleCombatEndDebt(Player? player)
    {
        if (player == null)
            return;

        int remainingDebt = GetDebtAmount(player.Creature);
        int excessLoanProfit = Math.Max(0, _netCombatLoanedGold - remainingDebt - MaxNetLoanProfitPerCombat);
        int totalSettlement = remainingDebt + excessLoanProfit;

        if (totalSettlement > 0 && player.Gold > 0)
        {
            int goldDeducted = Math.Min(player.Gold, totalSettlement);
            if (goldDeducted > 0)
            {
                await PlayerCmd.LoseGold(goldDeducted, player, GoldLossType.Spent);
            }
        }

        _netCombatLoanedGold = 0;
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
    /// <paramref name="damagePerLien"/> unpowered damage per stack and forcing the enemy to repay
    /// <paramref name="repayPerLien"/> Debt per stack on <paramref name="applier"/> (without costing player Gold).
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
            await RepayDebt(ctx, applier, totalRepay, spendPlayerGold: false);
        }

        return consumed;
    }

    /// <summary>
    /// Resolves turn-end Debt Interest (+20% rounded up) and Installment penalty (Debt / 5 Gold lost + HP damage
    /// if Debt &gt;= 10, or redirected to all enemies if <see cref="SovereignDefaultPower"/> is active),
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
            if (borrower.Player is { Gold: > 0 } player)
            {
                int garnishedGold = Math.Min(player.Gold, installmentDamage);
                if (garnishedGold > 0)
                {
                    await PlayerCmd.LoseGold(garnishedGold, player, GoldLossType.Lost);
                }
            }

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
