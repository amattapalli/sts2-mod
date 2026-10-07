using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Transmuter.TransmuterCode.Util;

namespace Transmuter.TransmuterCode.Cards;

/// <summary>
/// Fume Cloud — Common Skill, cost 1. Apply 2 (3) Sulfur to ALL hittable enemies.
/// </summary>
public sealed class FumeCloud() : TransmuterCard(1, CardType.Skill, CardRarity.Common, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Sulfur", 2m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null)
            return;

        int amount = DynamicVars["Sulfur"].IntValue;
        foreach (Creature enemy in CombatState.HittableEnemies.ToList())
        {
            await ReactionEngine.ApplyReagent(
                choiceContext,
                enemy,
                Owner.Creature,
                ReagentType.Sulfur,
                amount);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Sulfur"].UpgradeValueBy(1m);
    }
}
