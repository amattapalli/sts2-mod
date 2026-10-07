using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Transmuter.TransmuterCode.Util;

namespace Transmuter.TransmuterCode.Cards;

/// <summary>
/// Hermetic Seal — Common Skill, cost 1. Gain 6 (9) Block. Apply 1 Stabilize to target.
/// </summary>
public sealed class HermeticSeal() : TransmuterCard(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(6m, ValueProp.Move),
        new DynamicVar("Stabilize", 1m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);

        await CommonActions.CardBlock(this, play);
        await ReactionEngine.ApplyStabilize(
            choiceContext,
            play.Target,
            Owner.Creature,
            DynamicVars["Stabilize"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}
