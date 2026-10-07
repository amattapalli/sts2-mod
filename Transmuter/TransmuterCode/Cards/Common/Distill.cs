using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Transmuter.TransmuterCode.Util;

namespace Transmuter.TransmuterCode.Cards;

/// <summary>
/// Distill — Common Skill, cost 1. Double (Triple on upgrade) a single Reagent on target.
/// </summary>
public sealed class Distill() : TransmuterCard(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Multiplier", 2m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);

        int multiplier = IsUpgraded ? 3 : 2;
        await ReactionEngine.DoubleSingleReagent(
            choiceContext,
            play.Target,
            Owner.Creature,
            multiplier);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Multiplier"].UpgradeValueBy(1m);
    }
}
