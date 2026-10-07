using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Transmuter.TransmuterCode.Powers;

namespace Transmuter.TransmuterCode.Cards;

/// <summary>
/// Athanor Furnace — Uncommon Power, cost 1, Self.
/// At the start of your turn, apply 2 (3) Sulfur to the enemy with the highest HP.
/// </summary>
public sealed class AthanorFurnace() : TransmuterCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<AthanorFurnacePower>("Sulfur", 2m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<AthanorFurnacePower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["Sulfur"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Sulfur"].UpgradeValueBy(1m);
    }
}
