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
/// Leveraged Buyout — Rare Attack, cost 2, AnyEnemy.
/// Deal 14 (18) damage. Deal 2 (3) additional unpowered damage for each Debt you have, plus 1 per 15 Gold you have.
/// </summary>
public sealed class LeveragedBuyout() : UsurerCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    private const int GoldPerBonusDamage = 15;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(14m, ValueProp.Move),
        new DynamicVar("DamagePerDebt", 2m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);

        await CommonActions.CardAttack(this, play).Execute(choiceContext);

        int debt = DebtEngine.GetDebtAmount(Owner.Creature);
        int goldBonus = DebtEngine.GetGoldAmount(Owner.Creature) / GoldPerBonusDamage;
        int bonusDamage = (debt * DynamicVars["DamagePerDebt"].IntValue) + goldBonus;
        if (bonusDamage > 0 && play.Target.IsAlive)
        {
            await CreatureCmd.Damage(
                choiceContext,
                play.Target,
                bonusDamage,
                ValueProp.Unpowered,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
        DynamicVars["DamagePerDebt"].UpgradeValueBy(1m);
    }
}
