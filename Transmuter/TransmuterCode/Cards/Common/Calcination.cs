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
/// Calcination — Common Attack, cost 1. Deal 8 (11) damage. Apply 3 (4) Sulfur.
/// </summary>
public sealed class Calcination() : TransmuterCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move),
        new DynamicVar("Sulfur", 3m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);

        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        await ReactionEngine.ApplyReagent(
            choiceContext,
            play.Target,
            Owner.Creature,
            ReagentType.Sulfur,
            DynamicVars["Sulfur"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars["Sulfur"].UpgradeValueBy(1m);
    }
}
