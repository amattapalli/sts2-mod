using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using Usurer.UsurerCode.Util;

namespace Usurer.UsurerCode.Powers;

/// <summary>
/// Debtor's Prison — At the start of your turn, apply <see cref="MegaCrit.Sts2.Core.Models.Powers.PowerModel.Amount"/>
/// Weak to ALL enemies that have Lien.
/// </summary>
public sealed class DebtorsPrisonPower : UsurerPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != Owner.Side || Amount <= 0 || !Owner.IsAlive)
            return;

        List<Creature> indebtedEnemies = combatState.HittableEnemies
            .Where(e => e.IsAlive && DebtEngine.GetLienAmount(e) > 0)
            .ToList();

        if (indebtedEnemies.Count == 0)
            return;

        Flash();
        foreach (Creature enemy in indebtedEnemies)
        {
            if (enemy.IsAlive)
            {
                await PowerCmd.Apply<WeakPower>(choiceContext, enemy, Amount, Owner, null);
            }
        }
    }
}
