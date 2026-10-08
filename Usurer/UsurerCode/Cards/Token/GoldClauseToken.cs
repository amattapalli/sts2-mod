using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using Usurer.UsurerCode.Util;

namespace Usurer.UsurerCode.Cards;

/// <summary>
/// Gold Clause — Token Skill, cost 0, Self, Exhaust.
/// Repay 10 (14) Debt, gain 10 (14) Block, and gain 10 (15) Gold.
/// </summary>
[Pool(typeof(TokenCardPool))]
public sealed class GoldClauseToken() : UsurerCard(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Repay", 10m),
        new BlockVar(10m, ValueProp.Move),
        new GoldVar(10)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await DebtEngine.RepayDebt(choiceContext, Owner.Creature, DynamicVars["Repay"].IntValue, spendPlayerGold: false);
        await CommonActions.CardBlock(this, play);
        await PlayerCmd.GainGold(DynamicVars.Gold.IntValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Repay"].UpgradeValueBy(4m);
        DynamicVars.Block.UpgradeValueBy(4m);
        DynamicVars.Gold.UpgradeValueBy(5m);
    }
}
