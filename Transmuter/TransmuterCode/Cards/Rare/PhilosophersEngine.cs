using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Transmuter.TransmuterCode.Powers;

namespace Transmuter.TransmuterCode.Cards;

/// <summary>
/// Philosopher's Engine — Rare Power, cost 3 (2), Self.
/// Your Reactions trigger twice.
/// </summary>
public sealed class PhilosophersEngine() : TransmuterCard(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<PhilosophersEnginePower>(1m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<PhilosophersEnginePower>(
            choiceContext,
            Owner.Creature,
            DynamicVars.Power<PhilosophersEnginePower>().BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        // UpgradeCostBy(-1) via EnergyCost.UpgradeBy(-1)
        EnergyCost.UpgradeBy(-1);
    }
}
