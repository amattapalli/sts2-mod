using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Transmuter.TransmuterCode.Util;

namespace Transmuter.TransmuterCode.Cards;

/// <summary>
/// Lead to Gold — Uncommon Skill, cost 1 (0). Exhaust.
/// Trigger a Reaction on an enemy with 2+ stacks of a single Reagent as if 1 Salt was applied.
/// If a Reaction triggered, gain 5 (8) Gold.
/// </summary>
public sealed class LeadToGold() : TransmuterCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new GoldVar(5)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);

        bool triggered = await ReactionEngine.SelfReactSingleReagent(choiceContext, play.Target, Owner.Creature);
        if (triggered)
        {
            await PlayerCmd.GainGold(DynamicVars.Gold.IntValue, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        // UpgradeCostBy(-1) via EnergyCost.UpgradeBy(-1)
        EnergyCost.UpgradeBy(-1);
        DynamicVars.Gold.UpgradeValueBy(3m);
    }
}
