using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Usurer.UsurerCode.Powers;

namespace Usurer.UsurerCode.Cards;

/// <summary>
/// Debtor's Prison — Uncommon Power, cost 1, Self.
/// At the start of your turn, apply 1 (2) Weak to ALL enemies that have Lien.
/// </summary>
public sealed class DebtorsPrison() : UsurerCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Weak", 1m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<DebtorsPrisonPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["Weak"].IntValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Weak"].UpgradeValueBy(1m);
    }
}
