using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Transmuter.Tests;

public enum SimReagentType
{
    Salt,
    Sulfur,
    Mercury
}

public sealed class SimEnemy
{
    public string Name { get; init; } = "Enemy";
    public int Hp { get; set; } = 100;
    public int Block { get; set; }
    public Dictionary<SimReagentType, int> Reagents { get; } = new()
    {
        [SimReagentType.Salt] = 0,
        [SimReagentType.Sulfur] = 0,
        [SimReagentType.Mercury] = 0
    };
    public List<SimReagentType> ApplicationOrder { get; } = new();
    public int StabilizedTurns { get; set; }
    public int Weak { get; set; }
    public int Vulnerable { get; set; }

    public int GetReagent(SimReagentType type) => Reagents.GetValueOrDefault(type, 0);
    public int DistinctReagentCount => Reagents.Values.Count(v => v > 0);
    public int TotalReagentStacks => Reagents.Values.Sum();
}

public sealed class SimCombatState
{
    public int PlayerBlock { get; set; }
    public int PlayerEnergy { get; set; }
    public int CardsDrawn { get; set; }
    public int PlayerGold { get; set; }

    public int EmeraldTabletStacks { get; set; }
    public int PhilosophersEngineStacks { get; set; }
    public int ChainDetonationStacks { get; set; }
    public int CrucibleShieldStacks { get; set; }
    public int ResidualPrecipitateStacks { get; set; }

    public bool HasCrackedAlembic { get; set; }
    public bool CrackedAlembicTriggered { get; set; }
    public bool HasParacelsusScalpel { get; set; }

    public List<SimEnemy> Enemies { get; } = new();

    public void ApplyReagent(SimEnemy target, SimReagentType type, int baseAmount, bool isResidueReapply = false)
    {
        if (baseAmount <= 0)
            return;

        int finalAmount = baseAmount + (isResidueReapply ? 0 : EmeraldTabletStacks);
        if (target.Reagents[type] == 0 && !target.ApplicationOrder.Contains(type))
        {
            target.ApplicationOrder.Add(type);
        }
        target.Reagents[type] += finalAmount;

        ResolveReactions(target);
    }

    public void ApplyStabilize(SimEnemy target, int turns = 1)
    {
        target.StabilizedTurns += turns;
        ResolveReactions(target);
    }

    public void ResolveReactions(SimEnemy target)
    {
        int distinct = target.DistinctReagentCount;
        if (distinct < 2)
            return;

        if (target.StabilizedTurns > 0)
        {
            if (distinct == 3)
            {
                TriggerMagnumOpus(target);
            }
            return;
        }

        // Standard 2-Reagent Reaction
        bool hasSulfur = target.GetReagent(SimReagentType.Sulfur) > 0;
        bool hasMercury = target.GetReagent(SimReagentType.Mercury) > 0;
        bool hasSalt = target.GetReagent(SimReagentType.Salt) > 0;

        SimReagentType firstConsumed = target.ApplicationOrder.First(t => target.GetReagent(t) > 0);
        int potency = target.TotalReagentStacks;

        ConsumeReactingReagents(target, [SimReagentType.Salt, SimReagentType.Sulfur, SimReagentType.Mercury]);

        int triggerCount = 1 + PhilosophersEngineStacks;
        for (int i = 0; i < triggerCount; i++)
        {
            if (hasSulfur && hasMercury)
                ExecuteDetonate(target, potency);
            if (hasSalt && hasSulfur)
                ExecuteCalcify(target, potency);
            if (hasSalt && hasMercury)
                ExecuteDissolve(target, potency);
        }

        ExecutePostReactionHooks(target, firstConsumed);
    }

    public void TriggerMagnumOpus(SimEnemy target)
    {
        int potency = target.TotalReagentStacks;
        if (potency <= 0)
            return;

        SimReagentType firstConsumed = target.ApplicationOrder.FirstOrDefault(
            t => target.GetReagent(t) > 0,
            SimReagentType.Sulfur);

        target.StabilizedTurns = 0;
        ConsumeReactingReagents(target, [SimReagentType.Salt, SimReagentType.Sulfur, SimReagentType.Mercury]);

        int triggerCount = 1 + PhilosophersEngineStacks;
        for (int i = 0; i < triggerCount; i++)
        {
            ExecuteDetonate(target, potency);
            ExecuteCalcify(target, potency);
            ExecuteDissolve(target, potency);
        }

        ExecutePostReactionHooks(target, firstConsumed);
    }

    public void DoubleSingleReagent(SimEnemy target, int multiplier = 2)
    {
        if (target.DistinctReagentCount != 1)
            return;

        var activeType = target.Reagents.First(kv => kv.Value > 0).Key;
        int current = target.Reagents[activeType];
        int delta = current * (multiplier - 1);
        if (delta > 0)
        {
            target.Reagents[activeType] += delta;
        }
    }

    public void SelfReactSingleReagent(SimEnemy target)
    {
        if (target.DistinctReagentCount != 1)
            return;

        var active = target.Reagents.First(kv => kv.Value >= 2);
        SimReagentType existingType = active.Key;
        int potency = active.Value + 1;

        ConsumeReactingReagents(target, [existingType]);

        int triggerCount = 1 + PhilosophersEngineStacks;
        for (int i = 0; i < triggerCount; i++)
        {
            if (existingType == SimReagentType.Sulfur)
                ExecuteCalcify(target, potency);
            else if (existingType == SimReagentType.Mercury)
                ExecuteDissolve(target, potency);
            else
                ExecuteCalcify(target, potency);
        }

        ExecutePostReactionHooks(target, existingType);
    }

    private void ConsumeReactingReagents(SimEnemy target, IEnumerable<SimReagentType> types)
    {
        int floor = HasParacelsusScalpel ? 1 : 0;
        foreach (var type in types)
        {
            if (target.Reagents[type] > 0)
            {
                target.Reagents[type] = Math.Min(target.Reagents[type], floor);
                if (target.Reagents[type] == 0)
                    target.ApplicationOrder.Remove(type);
            }
        }
    }

    private void ExecuteDetonate(SimEnemy target, int potency)
    {
        DealUnpoweredDamage(target, 3 * potency);
        foreach (var other in Enemies.Where(e => !ReferenceEquals(e, target) && e.Hp > 0))
        {
            DealUnpoweredDamage(other, 2 * potency);
            if (ChainDetonationStacks > 0)
            {
                ApplyReagent(other, SimReagentType.Sulfur, ChainDetonationStacks);
            }
        }
    }

    private void ExecuteCalcify(SimEnemy target, int potency)
    {
        PlayerBlock += 2 * potency;
        int weakStacks = (int)Math.Ceiling(potency / 3.0);
        target.Weak += weakStacks;
    }

    private void ExecuteDissolve(SimEnemy target, int potency)
    {
        target.Block = 0;
        int vulnStacks = (int)Math.Ceiling(potency / 2.0);
        target.Vulnerable += vulnStacks;
        CardsDrawn += potency >= 6 ? 2 : 1;
    }

    private void ExecutePostReactionHooks(SimEnemy target, SimReagentType firstConsumed)
    {
        if (HasCrackedAlembic && !CrackedAlembicTriggered)
        {
            CrackedAlembicTriggered = true;
            PlayerEnergy += 1;
            CardsDrawn += 1;
        }

        if (CrucibleShieldStacks > 0)
        {
            PlayerBlock += CrucibleShieldStacks;
        }

        if (ResidualPrecipitateStacks > 0)
        {
            ApplyReagent(target, firstConsumed, ResidualPrecipitateStacks, isResidueReapply: true);
        }
    }

    private static void DealUnpoweredDamage(SimEnemy enemy, int damage)
    {
        int blocked = Math.Min(enemy.Block, damage);
        enemy.Block -= blocked;
        enemy.Hp -= (damage - blocked);
    }
}

public class ReactionSimulationTests
{
    [Fact]
    public void SameReagent_StacksWithoutTriggeringReaction()
    {
        var state = new SimCombatState();
        var enemy = new SimEnemy { Hp = 100 };
        state.Enemies.Add(enemy);

        state.ApplyReagent(enemy, SimReagentType.Sulfur, 3);
        state.ApplyReagent(enemy, SimReagentType.Sulfur, 4);

        Assert.Equal(7, enemy.GetReagent(SimReagentType.Sulfur));
        Assert.Equal(100, enemy.Hp);
        Assert.Equal(1, enemy.DistinctReagentCount);
    }

    [Fact]
    public void Detonate_FullConsumption_Deals3XToTargetAnd2XToOthers()
    {
        var state = new SimCombatState();
        var primary = new SimEnemy { Name = "Primary", Hp = 100 };
        var secondary = new SimEnemy { Name = "Secondary", Hp = 100 };
        state.Enemies.Add(primary);
        state.Enemies.Add(secondary);

        // Build 10 Sulfur, spark with 1 Mercury => X = 11
        state.ApplyReagent(primary, SimReagentType.Sulfur, 10);
        state.ApplyReagent(primary, SimReagentType.Mercury, 1);

        Assert.Equal(0, primary.TotalReagentStacks);
        Assert.Equal(100 - 33, primary.Hp);    // 3 * 11 = 33
        Assert.Equal(100 - 22, secondary.Hp);  // 2 * 11 = 22
    }

    [Theory]
    [InlineData(2, 1, 6, 1)]  // X = 3 => 6 Block, ceil(3/3) = 1 Weak
    [InlineData(3, 1, 8, 2)]  // X = 4 => 8 Block, ceil(4/3) = 2 Weak
    [InlineData(4, 2, 12, 2)] // X = 6 => 12 Block, ceil(6/3) = 2 Weak
    [InlineData(5, 2, 14, 3)] // X = 7 => 14 Block, ceil(7/3) = 3 Weak
    public void Calcify_SaltPlusSulfur_Grants2XBlockAndCeilXDiv3Weak(
        int salt, int sulfur, int expectedBlock, int expectedWeak)
    {
        var state = new SimCombatState();
        var enemy = new SimEnemy();
        state.Enemies.Add(enemy);

        state.ApplyReagent(enemy, SimReagentType.Salt, salt);
        state.ApplyReagent(enemy, SimReagentType.Sulfur, sulfur);

        Assert.Equal(0, enemy.TotalReagentStacks);
        Assert.Equal(expectedBlock, state.PlayerBlock);
        Assert.Equal(expectedWeak, enemy.Weak);
    }

    [Theory]
    [InlineData(2, 1, 2, 1)] // X = 3 => ceil(3/2) = 2 Vuln, 1 card drawn
    [InlineData(3, 2, 3, 1)] // X = 5 => ceil(5/2) = 3 Vuln, 1 card drawn
    [InlineData(3, 3, 3, 2)] // X = 6 => ceil(6/2) = 3 Vuln, 2 cards drawn
    [InlineData(5, 2, 4, 2)] // X = 7 => ceil(7/2) = 4 Vuln, 2 cards drawn
    public void Dissolve_SaltPlusMercury_StripsBlockAppliesVulnAndDraws(
        int salt, int mercury, int expectedVuln, int expectedDraws)
    {
        var state = new SimCombatState();
        var enemy = new SimEnemy { Hp = 80, Block = 25 };
        state.Enemies.Add(enemy);

        state.ApplyReagent(enemy, SimReagentType.Salt, salt);
        state.ApplyReagent(enemy, SimReagentType.Mercury, mercury);

        Assert.Equal(0, enemy.Block);
        Assert.Equal(0, enemy.TotalReagentStacks);
        Assert.Equal(expectedVuln, enemy.Vulnerable);
        Assert.Equal(expectedDraws, state.CardsDrawn);
    }

    [Fact]
    public void Stabilize_PausesTwoReagentReaction_UntilThirdTriggersMagnumOpus()
    {
        var state = new SimCombatState();
        var primary = new SimEnemy { Hp = 200, Block = 15 };
        var secondary = new SimEnemy { Hp = 200 };
        state.Enemies.Add(primary);
        state.Enemies.Add(secondary);

        state.ApplyStabilize(primary, 1);
        state.ApplyReagent(primary, SimReagentType.Salt, 3);
        state.ApplyReagent(primary, SimReagentType.Sulfur, 3);

        // Paused at 2 Reagents!
        Assert.Equal(3, primary.GetReagent(SimReagentType.Salt));
        Assert.Equal(3, primary.GetReagent(SimReagentType.Sulfur));
        Assert.Equal(200, primary.Hp);
        Assert.Equal(15, primary.Block);

        // Apply 3rd Reagent (Mercury 3) => Magnum Opus fires at X = 9!
        state.ApplyReagent(primary, SimReagentType.Mercury, 3);

        Assert.Equal(0, primary.TotalReagentStacks);
        Assert.Equal(0, primary.StabilizedTurns);
        // Detonate: 3*9 = 27 damage (15 absorbed by Block before Dissolve strips, or 12 HP lost)
        Assert.Equal(188, primary.Hp);
        Assert.Equal(200 - 18, secondary.Hp); // 2 * 9 = 18
        // Calcify: 2*9 = 18 Block, ceil(9/3) = 3 Weak
        Assert.Equal(18, state.PlayerBlock);
        Assert.Equal(3, primary.Weak);
        // Dissolve: Block = 0, ceil(9/2) = 5 Vulnerable, X >= 6 => 2 cards drawn
        Assert.Equal(0, primary.Block);
        Assert.Equal(5, primary.Vulnerable);
        Assert.Equal(2, state.CardsDrawn);
    }

    [Fact]
    public void PhilosophersEngine_TriggersReactionsTwice()
    {
        var state = new SimCombatState { PhilosophersEngineStacks = 1 };
        var enemy = new SimEnemy { Hp = 100 };
        state.Enemies.Add(enemy);

        state.ApplyReagent(enemy, SimReagentType.Salt, 2);
        state.ApplyReagent(enemy, SimReagentType.Sulfur, 2); // X = 4

        // Single Calcify at X=4 gives 8 Block and 2 Weak; doubled => 16 Block and 4 Weak
        Assert.Equal(16, state.PlayerBlock);
        Assert.Equal(4, enemy.Weak);
    }

    [Fact]
    public void EmeraldTablet_AddsBonusStacksToEveryApplication()
    {
        var state = new SimCombatState { EmeraldTabletStacks = 2 };
        var enemy = new SimEnemy { Hp = 100 };
        state.Enemies.Add(enemy);

        state.ApplyReagent(enemy, SimReagentType.Sulfur, 2); // +2 => 4
        Assert.Equal(4, enemy.GetReagent(SimReagentType.Sulfur));

        state.ApplyReagent(enemy, SimReagentType.Mercury, 1); // +2 => 3, total X = 7
        Assert.Equal(100 - 21, enemy.Hp); // 3 * 7 = 21
    }

    [Fact]
    public void ParacelsusScalpel_LeavesOneStackOfEachReactingReagentBehind()
    {
        var state = new SimCombatState { HasParacelsusScalpel = true };
        var enemy = new SimEnemy { Hp = 100 };
        state.Enemies.Add(enemy);

        state.ApplyReagent(enemy, SimReagentType.Sulfur, 4);
        state.ApplyReagent(enemy, SimReagentType.Mercury, 2); // X = 6 Detonate

        Assert.Equal(100 - 18, enemy.Hp);
        Assert.Equal(1, enemy.GetReagent(SimReagentType.Sulfur));
        Assert.Equal(1, enemy.GetReagent(SimReagentType.Mercury));
    }

    [Fact]
    public void PostReactionHooks_CrackedAlembic_CrucibleShield_ResidualPrecipitate()
    {
        var state = new SimCombatState
        {
            HasCrackedAlembic = true,
            CrucibleShieldStacks = 4,
            ResidualPrecipitateStacks = 2
        };
        var enemy = new SimEnemy { Hp = 100 };
        state.Enemies.Add(enemy);

        // 1st Reaction: Sulfur (applied first) + Mercury
        state.ApplyReagent(enemy, SimReagentType.Sulfur, 3);
        state.ApplyReagent(enemy, SimReagentType.Mercury, 1);

        Assert.True(state.CrackedAlembicTriggered);
        Assert.Equal(1, state.PlayerEnergy);
        Assert.Equal(1, state.CardsDrawn);
        Assert.Equal(4, state.PlayerBlock);
        // Residual Precipitate reapplies 2 Sulfur!
        Assert.Equal(2, enemy.GetReagent(SimReagentType.Sulfur));
        Assert.Equal(0, enemy.GetReagent(SimReagentType.Mercury));

        // 2nd Reaction: CrackedAlembic does not trigger a second time
        state.ApplyReagent(enemy, SimReagentType.Mercury, 1);
        Assert.Equal(1, state.PlayerEnergy);
        Assert.Equal(8, state.PlayerBlock); // CrucibleShield triggers again (+4)
    }

    [Fact]
    public void Distill_DoublesOrTriplesOnlyWhenSingleReagentPresent()
    {
        var state = new SimCombatState();
        var enemy = new SimEnemy();
        state.Enemies.Add(enemy);

        state.ApplyReagent(enemy, SimReagentType.Sulfur, 5);
        state.DoubleSingleReagent(enemy, multiplier: 2);
        Assert.Equal(10, enemy.GetReagent(SimReagentType.Sulfur));

        state.DoubleSingleReagent(enemy, multiplier: 3);
        Assert.Equal(30, enemy.GetReagent(SimReagentType.Sulfur));
    }
}
