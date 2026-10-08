using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Usurer.UsurerCode.Powers;

namespace Usurer.UsurerCode.Cards;

/// <summary>
/// Sovereign Default — Rare Power, cost 2 (1), Self.
/// Prevent turn-end Debt Installment self-damage. Whenever you are Over-Leveraged (10+ Debt)
/// at the end of your turn, deal ceil(Debt / 2) unpowered damage to ALL enemies.
/// </summary>
public sealed class SovereignDefault() : UsurerCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<SovereignDefaultPower>(
            choiceContext,
            Owner.Creature,
            1,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
