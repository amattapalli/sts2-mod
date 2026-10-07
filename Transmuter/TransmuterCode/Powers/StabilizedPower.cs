using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Transmuter.TransmuterCode.Util;

namespace Transmuter.TransmuterCode.Powers;

/// <summary>
/// Stabilized — Pauses 2-Reagent Reactions on the owner until the end of the player's turn,
/// allowing all 3 Reagents to accumulate for Magnum Opus.
/// </summary>
public sealed class StabilizedPower : TransmuterPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || !Owner.IsAlive)
            return;

        Flash();
        Creature? applier = CombatState?.Allies.FirstOrDefault(c => c.IsPlayer && c.IsAlive);

        if (ReactionEngine.GetDistinctReagentTypes(Owner) >= 3)
        {
            await ReactionEngine.TriggerMagnumOpus(choiceContext, Owner, applier);
            return;
        }

        if (Amount > 1)
        {
            await PowerCmd.ModifyAmount(choiceContext, this, -1, applier, null);
        }
        else
        {
            await PowerCmd.Remove(this);
            await ReactionEngine.ResolveReactions(choiceContext, Owner, applier);
        }
    }
}
