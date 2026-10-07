using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Transmuter.TransmuterCode.Character;
using Transmuter.TransmuterCode.Powers;
using Transmuter.TransmuterCode.Util;

namespace Transmuter.TransmuterCode.Relics;

/// <summary>
/// Ouroboros Ring — Uncommon Relic for The Transmuter.
/// Whenever an enemy dies while it has any Reagents, transfer those Reagents to a random living enemy.
/// </summary>
[Pool(typeof(TransmuterRelicPool))]
public sealed class OuroborosRing : TransmuterRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (!target.IsEnemy || (!result.WasTargetKilled && target.IsAlive && target.CurrentHp > 0))
            return;

        int salt = ReactionEngine.GetReagentAmount(target, ReagentType.Salt);
        int sulfur = ReactionEngine.GetReagentAmount(target, ReagentType.Sulfur);
        int mercury = ReactionEngine.GetReagentAmount(target, ReagentType.Mercury);

        if (salt + sulfur + mercury <= 0)
            return;

        if (target.GetPower<SaltPower>() is { } saltPower)
            await PowerCmd.Remove(saltPower);
        if (target.GetPower<SulfurPower>() is { } sulfurPower)
            await PowerCmd.Remove(sulfurPower);
        if (target.GetPower<MercuryPower>() is { } mercuryPower)
            await PowerCmd.Remove(mercuryPower);

        var combatState = target.CombatState ?? Owner.Creature.CombatState;
        if (combatState == null)
            return;

        List<Creature> livingEnemies = combatState.HittableEnemies
            .Where(e => e != target && e.IsAlive)
            .ToList();

        if (livingEnemies.Count == 0)
            return;

        Creature recipient = Owner.RunState.Rng.CombatTargets.NextItem(livingEnemies)
            ?? livingEnemies[0];

        Flash();

        if (salt > 0 && recipient.IsAlive)
            await ReactionEngine.ApplyReagent(choiceContext, recipient, Owner.Creature, ReagentType.Salt, salt);
        if (sulfur > 0 && recipient.IsAlive)
            await ReactionEngine.ApplyReagent(choiceContext, recipient, Owner.Creature, ReagentType.Sulfur, sulfur);
        if (mercury > 0 && recipient.IsAlive)
            await ReactionEngine.ApplyReagent(choiceContext, recipient, Owner.Creature, ReagentType.Mercury, mercury);
    }
}
