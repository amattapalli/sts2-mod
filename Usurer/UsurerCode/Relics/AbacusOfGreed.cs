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
using Usurer.UsurerCode.Character;
using Usurer.UsurerCode.Powers;
using Usurer.UsurerCode.Util;

namespace Usurer.UsurerCode.Relics;

/// <summary>
/// Abacus of Greed — Uncommon Relic for The Usurer.
/// Whenever an enemy dies while it has Lien, transfer its Lien to a random living enemy and Repay 3 Debt.
/// </summary>
[Pool(typeof(UsurerRelicPool))]
public sealed class AbacusOfGreed : UsurerRelic
{
    private const int RepayOnKillAmount = 3;

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

        int lien = DebtEngine.GetLienAmount(target);
        if (lien <= 0)
            return;

        if (target.GetPower<LienPower>() is { } lienPower)
        {
            await PowerCmd.Remove(lienPower);
        }

        Flash();
        await DebtEngine.RepayDebt(choiceContext, Owner.Creature, RepayOnKillAmount, spendPlayerGold: false);
        await PlayerCmd.GainGold(RepayOnKillAmount, Owner);

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

        if (recipient.IsAlive)
        {
            await DebtEngine.ApplyLien(choiceContext, recipient, Owner.Creature, lien);
        }
    }
}
