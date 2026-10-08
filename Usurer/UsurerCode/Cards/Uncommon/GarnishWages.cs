using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Usurer.UsurerCode.Powers;

namespace Usurer.UsurerCode.Cards;

/// <summary>
/// Garnish Wages — Uncommon Skill, cost 1, Self.
/// Gain 6 (8) Block. Whenever you apply Lien this turn, gain 3 (4) Block.
/// </summary>
public sealed class GarnishWages() : UsurerCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(6m, ValueProp.Move),
        new DynamicVar("LienBlock", 3m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        await PowerCmd.Apply<GarnishWagesPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["LienBlock"].IntValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
        DynamicVars["LienBlock"].UpgradeValueBy(1m);
    }
}
