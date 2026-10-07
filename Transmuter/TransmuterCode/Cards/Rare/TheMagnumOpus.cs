using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Transmuter.TransmuterCode.Util;

namespace Transmuter.TransmuterCode.Cards;

/// <summary>
/// The Magnum Opus — Rare Skill, cost 2 (1), AnyEnemy, Exhaust.
/// Apply Stabilize, then apply 3 (5) Salt, 3 (5) Sulfur, and 3 (5) Mercury, then immediately trigger Magnum Opus.
/// </summary>
public sealed class TheMagnumOpus() : TransmuterCard(2, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Reagents", 3m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);

        int amount = DynamicVars["Reagents"].IntValue;

        await ReactionEngine.ApplyStabilize(choiceContext, play.Target, Owner.Creature, 1);
        await ReactionEngine.ApplyReagent(choiceContext, play.Target, Owner.Creature, ReagentType.Salt, amount, resolveReactions: false);
        await ReactionEngine.ApplyReagent(choiceContext, play.Target, Owner.Creature, ReagentType.Sulfur, amount, resolveReactions: false);
        await ReactionEngine.ApplyReagent(choiceContext, play.Target, Owner.Creature, ReagentType.Mercury, amount, resolveReactions: true);
    }

    protected override void OnUpgrade()
    {
        // UpgradeCostBy(-1) via EnergyCost.UpgradeBy(-1)
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Reagents"].UpgradeValueBy(2m);
    }
}
