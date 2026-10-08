using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Usurer.UsurerCode.Util;

namespace Usurer.UsurerCode.Powers;

/// <summary>
/// Debt — Neutral financial counter on the player (uses <see cref="PowerType.Buff"/> so player Artifact
/// is not consumed by Borrowing). At the end of the player's turn, compounds by +20% Interest (rounded up)
/// and triggers an Installment payment of Debt / 5 unpowered damage if Debt is 10 or more.
/// </summary>
public sealed class DebtPower : UsurerPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task BeforeSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side || Amount <= 0 || !Owner.IsAlive)
            return;

        await DebtEngine.ResolveTurnEndDebt(choiceContext, Owner);
    }
}
