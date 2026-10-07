using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Transmuter.TransmuterCode.Powers;

/// <summary>
/// Crucible Shield — Temporary buff. Whenever you trigger a Reaction this turn,
/// gain <see cref="PowerModel.Amount"/> Block. Expires at turn end.
/// </summary>
public sealed class CrucibleShieldPower : TransmuterPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == Owner.Side)
        {
            await PowerCmd.Remove(this);
        }
    }
}
