using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Transmuter.TransmuterCode.Powers;

namespace Transmuter.TransmuterCode.Cards;

/// <summary>
/// Emerald Tablet — Rare Power, cost 2, Self.
/// Whenever you apply a Reagent, apply +1 (+2) additional stack.
/// </summary>
public sealed class EmeraldTablet() : TransmuterCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<EmeraldTabletPower>("BonusReagent", 1m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<EmeraldTabletPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["BonusReagent"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["BonusReagent"].UpgradeValueBy(1m);
    }
}
