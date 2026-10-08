using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using Usurer.UsurerCode.Character;
using Usurer.UsurerCode.Util;

namespace Usurer.UsurerCode.Relics;

/// <summary>
/// Blood Signet — Rare Relic for The Usurer.
/// At the start of your turn, if you are Over-Leveraged (10+ Debt), gain 3 Block and apply 2 Lien to ALL enemies.
/// </summary>
[Pool(typeof(UsurerRelicPool))]
public sealed class BloodSignet : UsurerRelic
{
    private const int BonusBlock = 3;
    private const int BonusLien = 2;

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != Owner.Creature.Side || !Owner.Creature.IsAlive || !DebtEngine.IsOverLeveraged(Owner.Creature))
            return;

        Flash();
        await CreatureCmd.GainBlock(Owner.Creature, BonusBlock, ValueProp.Unpowered, null);

        List<Creature> enemies = combatState.HittableEnemies
            .Where(e => e.IsAlive)
            .ToList();

        foreach (Creature enemy in enemies)
        {
            if (enemy.IsAlive)
            {
                await DebtEngine.ApplyLien(choiceContext, enemy, Owner.Creature, BonusLien);
            }
        }
    }
}
