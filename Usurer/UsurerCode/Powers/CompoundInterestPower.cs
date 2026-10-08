using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Usurer.UsurerCode.Util;

namespace Usurer.UsurerCode.Powers;

/// <summary>
/// Compound Interest — At the start of your turn, if you have any Debt, apply <see cref="MegaCrit.Sts2.Core.Models.Powers.PowerModel.Amount"/>
/// Lien to ALL enemies.
/// </summary>
public sealed class CompoundInterestPower : UsurerPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != Owner.Side || Amount <= 0 || !Owner.IsAlive || DebtEngine.GetDebtAmount(Owner) <= 0)
            return;

        List<Creature> enemies = combatState.HittableEnemies
            .Where(e => e.IsAlive)
            .ToList();

        if (enemies.Count == 0)
            return;

        Flash();
        foreach (Creature enemy in enemies)
        {
            if (enemy.IsAlive)
            {
                await DebtEngine.ApplyLien(choiceContext, enemy, Owner, Amount);
            }
        }
    }
}
