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
/// Debt Collection — Common Attack, cost 1.
/// Deal 8 (11) damage. Seize 4 (6) from the enemy to forgive Debt (or gain excess as Gold).
/// </summary>
public sealed class DebtCollection() : UsurerCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move),
        new DynamicVar("Repay", 4m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);

        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        int amount = DynamicVars["Repay"].IntValue;
        int repaid = await DebtEngine.RepayDebt(choiceContext, Owner.Creature, amount, spendPlayerGold: false);
        int leftoverGold = amount - repaid;
        if (leftoverGold > 0)
        {
            await PlayerCmd.GainGold(leftoverGold, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars["Repay"].UpgradeValueBy(2m);
    }
}
