using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Transmuter.TransmuterCode.Powers;

/// <summary>
/// Mercury (Spirit / Volatility) — Enemy debuff Reagent.
/// Enemy takes +<see cref="PowerModel.Amount"/> additional damage when hit by a powered Attack.
/// </summary>
public sealed class MercuryPower : TransmuterPower
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
        if (target != Owner || !props.IsPoweredAttack() || Amount <= 0 || !Owner.IsAlive)
            return;

        Flash();
        await CreatureCmd.Damage(
            choiceContext,
            Owner,
            Amount,
            ValueProp.Unpowered | ValueProp.SkipHurtAnim,
            dealer,
            cardSource);
    }
}
