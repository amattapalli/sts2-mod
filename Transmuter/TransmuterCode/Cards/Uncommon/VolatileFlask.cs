using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Transmuter.TransmuterCode.Util;

namespace Transmuter.TransmuterCode.Cards;

/// <summary>
/// Volatile Flask — Uncommon Attack, cost 2, AllEnemies.
/// Deal 8 (11) damage to ALL enemies. Apply 2 (3) Mercury to ALL hittable enemies.
/// </summary>
public sealed class VolatileFlask() : TransmuterCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move),
        new DynamicVar("Mercury", 2m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);

        if (CombatState == null)
            return;

        foreach (Creature enemy in CombatState.HittableEnemies.ToList())
        {
            if (enemy.IsAlive)
            {
                await ReactionEngine.ApplyReagent(
                    choiceContext,
                    enemy,
                    Owner.Creature,
                    ReagentType.Mercury,
                    DynamicVars["Mercury"].IntValue);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars["Mercury"].UpgradeValueBy(1m);
    }
}
