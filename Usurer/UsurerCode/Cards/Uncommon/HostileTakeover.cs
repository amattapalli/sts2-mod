using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Usurer.UsurerCode.Util;

namespace Usurer.UsurerCode.Cards;

/// <summary>
/// Hostile Takeover — Uncommon Attack, cost 2, AnyEnemy.
/// Destroy the target's Block, deal 12 (16) damage, apply 4 (6) Lien, and Borrow 4 Debt.
/// </summary>
public sealed class HostileTakeover() : UsurerCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(12m, ValueProp.Move),
        new DynamicVar("Lien", 4m),
        new DynamicVar("Borrow", 4m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);

        if (play.Target.IsAlive && play.Target.Block > 0)
        {
            await CreatureCmd.Damage(
                choiceContext,
                play.Target,
                play.Target.Block,
                ValueProp.Unpowered | ValueProp.SkipHurtAnim,
                Owner.Creature,
                this);
        }

        if (play.Target.IsAlive)
        {
            await CommonActions.CardAttack(this, play).Execute(choiceContext);
        }

        if (play.Target.IsAlive)
        {
            await DebtEngine.ApplyLien(
                choiceContext,
                play.Target,
                Owner.Creature,
                DynamicVars["Lien"].IntValue);
        }

        await DebtEngine.BorrowDebt(choiceContext, Owner.Creature, DynamicVars["Borrow"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
        DynamicVars["Lien"].UpgradeValueBy(2m);
    }
}
