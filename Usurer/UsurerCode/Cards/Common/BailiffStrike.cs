using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Usurer.UsurerCode.Util;

namespace Usurer.UsurerCode.Cards;

/// <summary>
/// Bailiff Strike — Common Attack, cost 1.
/// Deal 5 (7) damage twice. Apply 1 Lien on each hit (triggering existing Lien and repaying 1 Debt per hit).
/// </summary>
public sealed class BailiffStrike() : UsurerCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5m, ValueProp.Move),
        new DynamicVar("Lien", 1m)
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
                await DebtEngine.ApplyLien(
                    choiceContext,
                    play.Target,
                    Owner.Creature,
                    DynamicVars["Lien"].IntValue);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}
