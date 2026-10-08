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
/// Predatory Loan — Basic Attack, cost 1.
/// Deal 7 (10) damage. Apply 2 (3) Lien. Borrow 5 Debt.
/// </summary>
public sealed class PredatoryLoan() : UsurerCard(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(7m, ValueProp.Move),
        new DynamicVar("Lien", 2m),
        new DynamicVar("Borrow", 5m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);

        await CommonActions.CardAttack(this, play).Execute(choiceContext);

        if (play.Target.IsAlive)
        {
            await DebtEngine.ApplyLien(
                choiceContext,
                play.Target,
                Owner.Creature,
                DynamicVars["Lien"].IntValue);
        }

        await DebtEngine.BorrowDebt(choiceContext, Owner.Creature, DynamicVars["Borrow"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars["Lien"].UpgradeValueBy(1m);
    }
}
