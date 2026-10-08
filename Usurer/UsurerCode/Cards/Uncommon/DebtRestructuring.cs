using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Usurer.UsurerCode.Util;

namespace Usurer.UsurerCode.Cards;

/// <summary>
/// Debt Restructuring — Uncommon Skill, cost 1, Self.
/// Repay 7 (10) Debt and apply 1 (2) Moratorium.
/// </summary>
public sealed class DebtRestructuring() : UsurerCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Repay", 7m),
        new DynamicVar("Moratorium", 1m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await DebtEngine.RepayDebt(choiceContext, Owner.Creature, DynamicVars["Repay"].IntValue);
        await DebtEngine.ApplyMoratorium(choiceContext, Owner.Creature, DynamicVars["Moratorium"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Repay"].UpgradeValueBy(3m);
        DynamicVars["Moratorium"].UpgradeValueBy(1m);
    }
}
