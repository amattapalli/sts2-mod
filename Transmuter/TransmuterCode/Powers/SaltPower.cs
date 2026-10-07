using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Transmuter.TransmuterCode.Powers;

/// <summary>
/// Salt (Body / Crystallization) — Enemy debuff Reagent.
/// Whenever the enemy is hit by a powered Attack, the attacker gains <see cref="PowerModel.Amount"/> Block.
/// </summary>
public sealed class SaltPower : TransmuterPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || !props.IsPoweredAttack() || Amount <= 0 || dealer is not { IsAlive: true })
            return;

        Flash();
        await CreatureCmd.GainBlock(dealer, Amount, ValueProp.Unpowered, null);
    }
}
