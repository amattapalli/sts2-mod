using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Transmuter.TransmuterCode.Powers;
using Transmuter.TransmuterCode.Util;

namespace Transmuter.TransmuterCode.Cards;

/// <summary>
/// Chain Detonation — Uncommon Attack, cost 2.
/// Deal 10 (14) damage. Apply 3 (4) Sulfur. Whenever Detonate triggers this turn, apply 2 (3) Sulfur to ALL other enemies.
/// </summary>
public sealed class ChainDetonation() : TransmuterCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10m, ValueProp.Move),
        new DynamicVar("Sulfur", 3m),
        new PowerVar<ChainDetonationPower>("ChainSulfur", 2m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);

        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        await PowerCmd.Apply<ChainDetonationPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["ChainSulfur"].BaseValue,
            Owner.Creature,
            this);

        if (play.Target.IsAlive)
        {
            await ReactionEngine.ApplyReagent(
                choiceContext,
                play.Target,
                Owner.Creature,
                ReagentType.Sulfur,
                DynamicVars["Sulfur"].IntValue);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
        DynamicVars["Sulfur"].UpgradeValueBy(1m);
        DynamicVars["ChainSulfur"].UpgradeValueBy(1m);
    }
}
