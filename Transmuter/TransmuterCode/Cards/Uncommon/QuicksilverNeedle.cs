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
/// Quicksilver Needle — Uncommon Attack, cost 1.
/// Deal 3 (5) damage twice. Apply 1 (2) Mercury after each hit.
/// </summary>
public sealed class QuicksilverNeedle() : TransmuterCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(3m, ValueProp.Move),
        new DynamicVar("Mercury", 1m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);

        for (int i = 0; i < 2; i++)
        {
            if (!play.Target.IsAlive)
                break;

            await CommonActions.CardAttack(this, play).Execute(choiceContext);

            if (play.Target.IsAlive)
            {
                await ReactionEngine.ApplyReagent(
                    choiceContext,
                    play.Target,
                    Owner.Creature,
                    ReagentType.Mercury,
                    DynamicVars["Mercury"].IntValue);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars["Mercury"].UpgradeValueBy(1m);
    }
}
