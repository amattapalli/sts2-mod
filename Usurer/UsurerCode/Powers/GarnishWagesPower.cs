using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Usurer.UsurerCode.Powers;

/// <summary>
/// Garnish Wages — Temporary 1-turn buff. Whenever you apply Lien this turn,
/// gain <see cref="MegaCrit.Sts2.Core.Models.Powers.PowerModel.Amount"/> Block.
/// </summary>
public sealed class GarnishWagesPower : UsurerPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side || !Owner.IsAlive)
            return;

        await PowerCmd.Remove(this);
    }
}
