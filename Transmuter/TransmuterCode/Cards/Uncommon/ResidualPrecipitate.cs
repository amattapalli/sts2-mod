using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Transmuter.TransmuterCode.Powers;

namespace Transmuter.TransmuterCode.Cards;

/// <summary>
/// Residual Precipitate — Uncommon Power, cost 1, Self.
/// Whenever a Reaction triggers on an enemy, reapply 2 (3) stacks of the first Reagent consumed.
/// </summary>
public sealed class ResidualPrecipitate() : TransmuterCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<ResidualPrecipitatePower>("Residue", 2m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<ResidualPrecipitatePower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["Residue"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Residue"].UpgradeValueBy(1m);
    }
}
