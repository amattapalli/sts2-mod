using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Usurer.UsurerCode.Powers;

namespace Usurer.UsurerCode.Cards;

/// <summary>
/// Shadow Banking — Uncommon Power, cost 1, Self.
/// Whenever you Repay any Debt, gain 3 (4) Block.
/// </summary>
public sealed class ShadowBanking() : UsurerCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("BonusBlock", 3m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<ShadowBankingPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["BonusBlock"].IntValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["BonusBlock"].UpgradeValueBy(1m);
    }
}
