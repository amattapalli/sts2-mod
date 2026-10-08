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
/// Amortize — Common Skill, cost 1, Self.
/// Repay 6 (8) Debt. Gain 4 (6) Block. Draw 1 card.
/// </summary>
public sealed class Amortize() : UsurerCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Repay", 6m),
        new BlockVar(4m, ValueProp.Move),
        new CardsVar(1)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await DebtEngine.RepayDebt(choiceContext, Owner.Creature, DynamicVars["Repay"].IntValue);
        await CommonActions.CardBlock(this, play);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Repay"].UpgradeValueBy(2m);
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}
