using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Transmuter.TransmuterCode.Util;

namespace Transmuter.TransmuterCode.Cards;

/// <summary>
/// Sublimate — Uncommon Skill, cost 1.
/// Convert all stacks of one Reagent on an enemy into another Reagent, then add 3 (5) stacks of it.
/// </summary>
public sealed class Sublimate() : TransmuterCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("BonusStacks", 3m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);

        await ReactionEngine.ConvertReagents(
            choiceContext,
            play.Target,
            Owner.Creature,
            DynamicVars["BonusStacks"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["BonusStacks"].UpgradeValueBy(2m);
    }
}
