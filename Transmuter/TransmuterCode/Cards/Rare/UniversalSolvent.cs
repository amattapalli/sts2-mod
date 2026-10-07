using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Transmuter.TransmuterCode.Util;

namespace Transmuter.TransmuterCode.Cards;

/// <summary>
/// Universal Solvent — Rare Attack, cost 2, AllEnemies.
/// Deal 12 (16) damage to ALL enemies, strip Block and ArtifactPower from all hittable enemies,
/// then apply 3 (4) Salt and 3 (4) Mercury to each hittable enemy.
/// </summary>
public sealed class UniversalSolvent() : TransmuterCard(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(12m, ValueProp.Move),
        new DynamicVar("Salt", 3m),
        new DynamicVar("Mercury", 3m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);

        if (CombatState == null)
            return;

        int saltAmount = DynamicVars["Salt"].IntValue;
        int mercuryAmount = DynamicVars["Mercury"].IntValue;

        foreach (Creature enemy in CombatState.HittableEnemies.ToList())
        {
            if (!enemy.IsAlive)
                continue;

            if (enemy.Block > 0)
            {
                await CreatureCmd.Damage(
                    choiceContext,
                    enemy,
                    enemy.Block,
                    ValueProp.Unpowered,
                    Owner.Creature,
                    this);
            }

            ArtifactPower? artifact = enemy.GetPower<ArtifactPower>();
            if (artifact != null)
            {
                await PowerCmd.Remove(artifact);
            }

            await ReactionEngine.ApplyReagent(
                choiceContext,
                enemy,
                Owner.Creature,
                ReagentType.Salt,
                saltAmount);

            if (enemy.IsAlive)
            {
                await ReactionEngine.ApplyReagent(
                    choiceContext,
                    enemy,
                    Owner.Creature,
                    ReagentType.Mercury,
                    mercuryAmount);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
        DynamicVars["Salt"].UpgradeValueBy(1m);
        DynamicVars["Mercury"].UpgradeValueBy(1m);
    }
}
