using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Transmuter.TransmuterCode.Powers;

namespace Transmuter.TransmuterCode.Cards;

/// <summary>
/// Crucible Shield — Uncommon Skill, cost 2, Self.
/// Gain 12 (16) Block and apply 4 (6) CrucibleShieldPower to self.
/// </summary>
public sealed class CrucibleShield() : TransmuterCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(12m, ValueProp.Move),
        new PowerVar<CrucibleShieldPower>("ReactionBlock", 4m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        await PowerCmd.Apply<CrucibleShieldPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["ReactionBlock"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(4m);
        DynamicVars["ReactionBlock"].UpgradeValueBy(2m);
    }
}
