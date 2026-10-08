using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Usurer.UsurerCode.Util;

namespace Usurer.UsurerCode.Cards;

/// <summary>
/// Ledger Slam — Common Attack, cost 1.
/// Deal 7 (10) damage. If you are Over-Leveraged (10+ Debt), deal 6 (8) additional damage.
/// </summary>
public sealed class LedgerSlam() : UsurerCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(7m, ValueProp.Move),
        new DynamicVar("BonusDamage", 6m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);

        await CommonActions.CardAttack(this, play).Execute(choiceContext);

        if (play.Target.IsAlive && DebtEngine.IsOverLeveraged(Owner.Creature))
        {
            await DamageCmd.Attack(DynamicVars["BonusDamage"].BaseValue)
                .FromCard(this)
                .Targeting(play.Target)
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars["BonusDamage"].UpgradeValueBy(2m);
    }
}
