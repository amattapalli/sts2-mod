using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Transmuter.TransmuterCode.Character;
using Transmuter.TransmuterCode.Util;

namespace Transmuter.TransmuterCode.Relics;

/// <summary>
/// Cracked Alembic — Starter Relic for The Transmuter.
/// At the start of combat, apply 2 Sulfur to a random enemy.
/// The first Reaction you trigger each combat grants 1 Energy and draws 1 card.
/// </summary>
[Pool(typeof(TransmuterRelicPool))]
public sealed class CrackedAlembic : TransmuterRelic
{
    private const int InitialSulfurStacks = 2;
    private bool _triggeredFirstReactionThisCombat;

    public override RelicRarity Rarity => RelicRarity.Starter;

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != Owner.Creature.Side || combatState.RoundNumber > 1)
            return;

        _triggeredFirstReactionThisCombat = false;

        List<Creature> livingEnemies = combatState.HittableEnemies
            .Where(e => e.IsAlive)
            .ToList();

        if (livingEnemies.Count == 0)
            return;

        Creature? target = Owner.RunState.Rng.CombatTargets.NextItem(livingEnemies)
            ?? livingEnemies[0];

        Flash();
        await ReactionEngine.ApplyReagent(
            choiceContext,
            target,
            Owner.Creature,
            ReagentType.Sulfur,
            InitialSulfurStacks);
    }

    public async Task OnReactionTriggered(PlayerChoiceContext choiceContext)
    {
        if (_triggeredFirstReactionThisCombat)
            return;

        _triggeredFirstReactionThisCombat = true;
        Flash();

        await PlayerCmd.GainEnergy(1, Owner);
        await CardPileCmd.Draw(choiceContext, 1, Owner);
    }
}
