using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Transmuter.TransmuterCode.Powers;

/// <summary>
/// Sulfur (Soul / Combustion) — Enemy debuff Reagent.
/// Deals <see cref="PowerModel.Amount"/> unpowered damage at the end of the enemy's turn.
/// </summary>
public sealed class SulfurPower : TransmuterPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task BeforeSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side || Amount <= 0 || !Owner.IsAlive)
            return;

        Flash();
        await CreatureCmd.Damage(
            choiceContext,
            Owner,
            Amount,
            ValueProp.Unpowered,
            Owner,
            null);
    }
}
