using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Transmuter.Tests;

public sealed class UsurerSimEnemy
{
    public string Name { get; init; } = "Enemy";
    public int Hp { get; set; } = 100;
    public int Block { get; set; }
    public int Lien { get; set; }
    public int Weak { get; set; }
    public int Vulnerable { get; set; }
    public int Artifact { get; set; }
    public bool IsAlive => Hp > 0;
}

public sealed class UsurerSimState
{
    public const int OverLeveragedThreshold = 10;

    public int PlayerHp { get; set; } = 75;
    public int PlayerBlock { get; set; }
    public int PlayerEnergy { get; set; }
    public int CardsDrawn { get; set; }
    public int PlayerGold { get; set; }

    public int Debt { get; set; }
    public int Moratorium { get; set; }

    public int CompoundInterestStacks { get; set; }
    public int ShadowBankingStacks { get; set; }
    public int GarnishWagesStacks { get; set; }
    public int DebtorsPrisonStacks { get; set; }
    public int SovereignDefaultStacks { get; set; }
    public int GoldenScalesStacks { get; set; }

    public bool HasInfernalLedger { get; set; }
    public bool InfernalLedgerTriggered { get; set; }
    public bool HasAbacusOfGreed { get; set; }
    public bool HasBloodSignet { get; set; }

    public List<UsurerSimEnemy> Enemies { get; } = new();

    public bool IsOverLeveraged => Debt >= OverLeveragedThreshold;

    public void StartCombat()
    {
        InfernalLedgerTriggered = false;
        if (HasInfernalLedger)
        {
            Debt += 5;
        }
    }

    public void StartPlayerTurn()
    {
        PlayerBlock = 0;

        if (HasBloodSignet && IsOverLeveraged)
        {
            PlayerBlock += 3;
            foreach (var enemy in Enemies.Where(e => e.IsAlive).ToList())
            {
                ApplyLien(enemy, 2);
            }
        }

        if (CompoundInterestStacks > 0 && Debt > 0)
        {
            foreach (var enemy in Enemies.Where(e => e.IsAlive).ToList())
            {
                ApplyLien(enemy, CompoundInterestStacks);
            }
        }

        if (DebtorsPrisonStacks > 0)
        {
            foreach (var enemy in Enemies.Where(e => e.IsAlive && e.Lien > 0))
            {
                ApplyDebuff(enemy, weak: DebtorsPrisonStacks);
            }
        }
    }

    public void BorrowDebt(int amount)
    {
        if (amount <= 0)
            return;

        Debt += amount;

        if (GoldenScalesStacks > 0)
        {
            var firstAlive = Enemies.FirstOrDefault(e => e.IsAlive);
            if (firstAlive != null)
            {
                ApplyLien(firstAlive, GoldenScalesStacks);
            }
        }

        if (IsOverLeveraged && HasInfernalLedger && !InfernalLedgerTriggered)
        {
            InfernalLedgerTriggered = true;
            PlayerEnergy += 1;
            CardsDrawn += 1;
        }
    }

    public int RepayDebt(int amount)
    {
        if (amount <= 0 || Debt <= 0)
            return 0;

        int repaid = Math.Min(Debt, amount);
        Debt -= repaid;

        if (ShadowBankingStacks > 0)
        {
            PlayerBlock += ShadowBankingStacks;
        }

        if (Debt == 0 && HasInfernalLedger && !InfernalLedgerTriggered)
        {
            InfernalLedgerTriggered = true;
            PlayerEnergy += 1;
            CardsDrawn += 1;
        }

        return repaid;
    }

    public void ApplyLien(UsurerSimEnemy target, int amount)
    {
        if (amount <= 0 || !target.IsAlive)
            return;

        if (target.Artifact > 0)
        {
            target.Artifact -= 1;
        }
        else
        {
            target.Lien += amount;
        }

        if (GarnishWagesStacks > 0)
        {
            PlayerBlock += GarnishWagesStacks;
        }
    }

    public void ApplyMoratorium(int turns = 1)
    {
        if (turns > 0)
        {
            Moratorium += turns;
        }
    }

    public void DoubleLien(UsurerSimEnemy target, int multiplier = 2)
    {
        if (!target.IsAlive || multiplier <= 1 || target.Lien <= 0)
            return;

        ApplyLien(target, target.Lien * (multiplier - 1));
    }

    public void DealAttackHit(UsurerSimEnemy target, int baseDamage)
    {
        if (!target.IsAlive)
            return;

        int dmg = baseDamage;
        if (target.Vulnerable > 0)
        {
            dmg = (int)Math.Floor(dmg * 1.5);
        }

        DealDamage(target, dmg);

        if (target.Lien > 0)
        {
            if (target.IsAlive)
            {
                DealDamage(target, target.Lien);
            }
            RepayDebt(1);
        }

        CheckAbacusOnKill(target);
    }

    public int Foreclose(UsurerSimEnemy target, int damagePerLien, int repayPerLien = 1)
    {
        if (!target.IsAlive || target.Lien <= 0)
            return 0;

        int consumed = target.Lien;
        target.Lien = 0;

        DealDamage(target, consumed * damagePerLien);
        RepayDebt(consumed * repayPerLien);
        CheckAbacusOnKill(target);
        return consumed;
    }

    public void EndPlayerTurn()
    {
        if (Moratorium > 0)
        {
            Moratorium -= 1;
            GarnishWagesStacks = 0;
            return;
        }

        if (Debt > 0)
        {
            int interest = (int)Math.Ceiling(Debt * 0.20);
            Debt += interest;

            if (IsOverLeveraged)
            {
                if (HasInfernalLedger && !InfernalLedgerTriggered)
                {
                    InfernalLedgerTriggered = true;
                    PlayerEnergy += 1;
                    CardsDrawn += 1;
                }

                if (SovereignDefaultStacks > 0)
                {
                    int aoeDmg = (int)Math.Ceiling(Debt / 2.0) * SovereignDefaultStacks;
                    foreach (var enemy in Enemies.Where(e => e.IsAlive).ToList())
                    {
                        DealDamage(enemy, aoeDmg);
                        CheckAbacusOnKill(enemy);
                    }
                }
                else
                {
                    int installment = Debt / 5;
                    int blocked = Math.Min(PlayerBlock, installment);
                    PlayerBlock -= blocked;
                    PlayerHp -= (installment - blocked);
                }
            }
        }

        GarnishWagesStacks = 0;
    }

    private void CheckAbacusOnKill(UsurerSimEnemy target)
    {
        if (target.IsAlive || !HasAbacusOfGreed || target.Lien <= 0)
            return;

        int transferred = target.Lien;
        target.Lien = 0;
        RepayDebt(3);

        var recipient = Enemies.FirstOrDefault(e => e.IsAlive && !ReferenceEquals(e, target));
        if (recipient != null)
        {
            ApplyLien(recipient, transferred);
        }
    }

    private static void ApplyDebuff(UsurerSimEnemy target, int weak = 0, int vuln = 0)
    {
        if (weak > 0)
        {
            if (target.Artifact > 0) target.Artifact--;
            else target.Weak += weak;
        }
        if (vuln > 0)
        {
            if (target.Artifact > 0) target.Artifact--;
            else target.Vulnerable += vuln;
        }
    }

    private static void DealDamage(UsurerSimEnemy target, int amount)
    {
        if (amount <= 0 || !target.IsAlive)
            return;

        int blocked = Math.Min(target.Block, amount);
        target.Block -= blocked;
        target.Hp -= (amount - blocked);
    }
}

public class UsurerSimulationTests
{
    [Fact]
    public void InfernalLedger_TriggersOnOverLeveragedOrDebtSettled_ExactlyOncePerCombat()
    {
        // Path A: PredatoryLoan (+5 Debt) from 5 starting Debt reaches 10 Debt (Over-Leveraged)
        var stateA = new UsurerSimState { HasInfernalLedger = true };
        stateA.StartCombat();
        Assert.Equal(5, stateA.Debt);

        stateA.BorrowDebt(5);
        Assert.Equal(10, stateA.Debt);
        Assert.True(stateA.InfernalLedgerTriggered);
        Assert.Equal(1, stateA.PlayerEnergy);
        Assert.Equal(1, stateA.CardsDrawn);

        // Repaying to 0 afterwards does not trigger a second time in the same combat
        stateA.RepayDebt(10);
        Assert.Equal(1, stateA.PlayerEnergy);

        // Path B: Audit (-5 Debt) from 5 starting Debt reaches 0 Debt (Settled)
        var stateB = new UsurerSimState { HasInfernalLedger = true };
        stateB.StartCombat();
        int repaid = stateB.RepayDebt(5);
        Assert.Equal(5, repaid);
        Assert.Equal(0, stateB.Debt);
        Assert.True(stateB.InfernalLedgerTriggered);
        Assert.Equal(1, stateB.PlayerEnergy);
        Assert.Equal(1, stateB.CardsDrawn);
    }

    [Theory]
    [InlineData(5, 6, 0)]   // 5 + ceil(1.0) = 6 (<10, no installment)
    [InlineData(8, 10, 2)]  // 8 + ceil(1.6) = 10 (>=10, 10/5 = 2 installment)
    [InlineData(10, 12, 2)] // 10 + ceil(2.0) = 12 (12/5 = 2 installment)
    [InlineData(15, 18, 3)] // 15 + ceil(3.0) = 18 (18/5 = 3 installment)
    public void TurnEndDebt_Accrues20PercentInterest_AndDealsInstallmentDamageWhenOverLeveraged(
        int initialDebt, int expectedEndDebt, int expectedInstallmentDamage)
    {
        var state = new UsurerSimState { Debt = initialDebt, PlayerHp = 75, PlayerBlock = 0 };
        state.EndPlayerTurn();

        Assert.Equal(expectedEndDebt, state.Debt);
        Assert.Equal(75 - expectedInstallmentDamage, state.PlayerHp);
    }

    [Fact]
    public void Moratorium_PausesTurnEndInterestAndInstallmentDamage()
    {
        var state = new UsurerSimState { Debt = 15, PlayerHp = 75 };
        state.ApplyMoratorium(1);

        state.EndPlayerTurn();

        Assert.Equal(0, state.Moratorium);
        Assert.Equal(15, state.Debt);
        Assert.Equal(75, state.PlayerHp);
    }

    [Fact]
    public void Lien_DealsBonusDamageOnEveryAttackHit_AndRepaysOneDebtPerHit()
    {
        var state = new UsurerSimState { Debt = 6 };
        var enemy = new UsurerSimEnemy { Hp = 100 };
        state.Enemies.Add(enemy);

        state.ApplyLien(enemy, 3);

        // Bailiff Strike: 2 hits of 5 damage, applying 1 Lien after each hit
        state.DealAttackHit(enemy, 5); // 5 + 3 Lien = 8 dmg, Debt 6 -> 5
        state.ApplyLien(enemy, 1);     // Lien 3 -> 4
        state.DealAttackHit(enemy, 5); // 5 + 4 Lien = 9 dmg, Debt 5 -> 4
        state.ApplyLien(enemy, 1);     // Lien 4 -> 5

        Assert.Equal(100 - 17, enemy.Hp);
        Assert.Equal(5, enemy.Lien);
        Assert.Equal(4, state.Debt);
    }

    [Fact]
    public void Foreclose_ConsumesAllLien_DealsBurstDamageAndRepaysDebtPerStack()
    {
        var state = new UsurerSimState { Debt = 8 };
        var enemy = new UsurerSimEnemy { Hp = 100 };
        state.Enemies.Add(enemy);

        state.ApplyLien(enemy, 5);
        int consumed = state.Foreclose(enemy, damagePerLien: 3, repayPerLien: 1);

        Assert.Equal(5, consumed);
        Assert.Equal(0, enemy.Lien);
        Assert.Equal(100 - 15, enemy.Hp);
        Assert.Equal(3, state.Debt);
    }

    [Fact]
    public void SovereignDefault_RedirectsInstallmentSelfDamageIntoAoEDamage()
    {
        var state = new UsurerSimState
        {
            Debt = 15,
            PlayerHp = 75,
            SovereignDefaultStacks = 1
        };
        var e1 = new UsurerSimEnemy { Hp = 100 };
        var e2 = new UsurerSimEnemy { Hp = 100 };
        state.Enemies.Add(e1);
        state.Enemies.Add(e2);

        // End of turn: Debt 15 -> +3 interest = 18. ceil(18/2) = 9 AoE damage to all enemies, 0 self-damage!
        state.EndPlayerTurn();

        Assert.Equal(18, state.Debt);
        Assert.Equal(75, state.PlayerHp);
        Assert.Equal(91, e1.Hp);
        Assert.Equal(91, e2.Hp);
    }

    [Fact]
    public void EnginePowers_GoldenScales_ShadowBanking_GarnishWages_CompoundInterest_DebtorsPrison_Synergize()
    {
        var state = new UsurerSimState
        {
            GoldenScalesStacks = 2,
            ShadowBankingStacks = 3,
            GarnishWagesStacks = 3,
            CompoundInterestStacks = 2,
            DebtorsPrisonStacks = 1
        };
        var enemy = new UsurerSimEnemy { Hp = 100 };
        state.Enemies.Add(enemy);

        // Borrow 5 Debt -> GoldenScales applies 2 Lien -> GarnishWages grants 3 Block
        state.BorrowDebt(5);
        Assert.Equal(5, state.Debt);
        Assert.Equal(2, enemy.Lien);
        Assert.Equal(3, state.PlayerBlock);

        // Repay 2 Debt -> ShadowBanking grants 3 Block
        state.RepayDebt(2);
        Assert.Equal(3, state.Debt);
        Assert.Equal(6, state.PlayerBlock);

        // Start next turn -> CompoundInterest applies 2 Lien (total 4), DebtorsPrison applies 1 Weak
        state.StartPlayerTurn();
        Assert.Equal(4, enemy.Lien);
        Assert.Equal(1, enemy.Weak);
    }

    [Fact]
    public void AbacusOfGreed_TransfersLienOnEnemyKill_AndRepaysThreeDebt()
    {
        var state = new UsurerSimState
        {
            Debt = 7,
            HasAbacusOfGreed = true
        };
        var minion = new UsurerSimEnemy { Name = "Minion", Hp = 6, Lien = 4 };
        var boss = new UsurerSimEnemy { Name = "Boss", Hp = 100, Lien = 0 };
        state.Enemies.Add(minion);
        state.Enemies.Add(boss);

        // Attack kills minion -> 1 Debt repaid from Lien hit + 3 Debt repaid from Abacus = 4 repaid; 4 Lien transferred to Boss
        state.DealAttackHit(minion, 6);

        Assert.False(minion.IsAlive);
        Assert.Equal(3, state.Debt);
        Assert.Equal(4, boss.Lien);
    }
}
