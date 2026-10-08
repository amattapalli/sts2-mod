using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Usurer.UsurerCode.Powers;

namespace Usurer.UsurerCode.Cards;

/// <summary>
/// Golden Scales — Rare Power, cost 1, Self.
/// Whenever you Borrow Debt, apply 2 (3) Lien to a random enemy.
/// </summary>
public sealed class GoldenScales() : UsurerCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Lien", 2m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<GoldenScalesPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["Lien"].IntValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Lien"].UpgradeValueBy(1m);
    }
}
