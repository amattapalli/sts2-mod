using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Transmuter.TransmuterCode.Util;

namespace Transmuter.TransmuterCode.Cards;

/// <summary>
/// Sympathetic Tincture — Uncommon Skill, cost 1, AnyEnemy.
/// Count distinct debuffs (PowerType.Debuff) on target; for each, apply 2 (3) Sulfur in a single application.
/// </summary>
public sealed class SympatheticTincture() : TransmuterCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Sulfur", 2m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);

        int distinctDebuffs = play.Target.Powers
            .Where(p => p.Type == PowerType.Debuff)
            .Select(p => p.GetType())
            .Distinct()
            .Count();

        int totalSulfur = distinctDebuffs * DynamicVars["Sulfur"].IntValue;
        if (totalSulfur > 0 && play.Target.IsAlive)
        {
            await ReactionEngine.ApplyReagent(
                choiceContext,
                play.Target,
                Owner.Creature,
                ReagentType.Sulfur,
                totalSulfur);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Sulfur"].UpgradeValueBy(1m);
    }
}
