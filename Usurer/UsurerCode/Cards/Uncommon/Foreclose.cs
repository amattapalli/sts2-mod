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
/// Foreclose — Uncommon Attack, cost 1.
/// Deal 8 (11) damage, then Foreclose the target's Lien (consuming all Lien stacks to deal
/// 3 (4) unpowered damage per stack and Repay 1 Debt per stack).
/// </summary>
public sealed class Foreclose() : UsurerCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move),
        new DynamicVar("DamagePerLien", 3m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);

        await CommonActions.CardAttack(this, play).Execute(choiceContext);

        if (play.Target.IsAlive)
        {
            await DebtEngine.ForecloseLien(
                choiceContext,
                play.Target,
                Owner.Creature,
                DynamicVars["DamagePerLien"].IntValue,
                repayPerLien: 1);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars["DamagePerLien"].UpgradeValueBy(1m);
    }
}
