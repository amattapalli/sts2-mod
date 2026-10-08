using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Usurer.UsurerCode.Util;

namespace Usurer.UsurerCode.Cards;

/// <summary>
/// Double Entry — Uncommon Skill, cost 1, AnyEnemy.
/// Double (Triple) the target's Lien. Borrow 4 Debt.
/// </summary>
public sealed class DoubleEntry() : UsurerCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Borrow", 4m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);

        int multiplier = IsUpgraded ? 3 : 2;
        await DebtEngine.DoubleLien(choiceContext, play.Target, Owner.Creature, multiplier);
        await DebtEngine.BorrowDebt(choiceContext, Owner.Creature, DynamicVars["Borrow"].IntValue);
    }

    protected override void OnUpgrade()
    {
    }
}
