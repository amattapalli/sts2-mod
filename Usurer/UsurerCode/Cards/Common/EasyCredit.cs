using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Usurer.UsurerCode.Util;

namespace Usurer.UsurerCode.Cards;

/// <summary>
/// Easy Credit — Common Skill, cost 0, Self.
/// Gain 1 Energy, draw 1 (2) card(s), and Borrow 5 Debt.
/// </summary>
public sealed class EasyCredit() : UsurerCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1),
        new CardsVar(1),
        new DynamicVar("Borrow", 5m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        await DebtEngine.BorrowDebt(choiceContext, Owner.Creature, DynamicVars["Borrow"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
