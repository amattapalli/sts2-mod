using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Transmuter.TransmuterCode.Util;

namespace Transmuter.TransmuterCode.Powers;

/// <summary>
/// Athanor Furnace — At the start of your turn, apply <see cref="PowerModel.Amount"/> Sulfur
/// to the enemy with the highest HP.
/// </summary>
public sealed class AthanorFurnacePower : TransmuterPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player != Owner.Player || Amount <= 0 || CombatState == null)
            return;

        Creature? target = CombatState.HittableEnemies
            .Where(e => e.IsAlive)
            .OrderByDescending(e => e.CurrentHp)
            .FirstOrDefault();

        if (target == null)
            return;

        Flash();
        await ReactionEngine.ApplyReagent(
            choiceContext,
            target,
            Owner,
            ReagentType.Sulfur,
            Amount);
    }
}
