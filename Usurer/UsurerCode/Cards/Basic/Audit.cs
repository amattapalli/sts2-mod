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
/// Audit — Basic Skill, cost 1, Self.
/// Gain 5 (8) Block. Repay 5 Debt. Gain 1 additional Block for each Debt repaid.
/// </summary>
public sealed class Audit() : UsurerCard(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(5m, ValueProp.Move),
        new DynamicVar("Repay", 5m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);

        int repaid = await DebtEngine.RepayDebt(
            choiceContext,
            Owner.Creature,
            DynamicVars["Repay"].IntValue);

        if (repaid > 0 && Owner.Creature.IsAlive)
        {
            await CreatureCmd.GainBlock(Owner.Creature, repaid, ValueProp.Unpowered, play);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}
