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
/// Collateral Shield — Common Skill, cost 1, AnyEnemy.
/// Gain 6 (8) Block. Gain additional Block equal to the target's Lien, then apply 2 (3) Lien.
/// </summary>
public sealed class CollateralShield() : UsurerCard(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(6m, ValueProp.Move),
        new DynamicVar("Lien", 2m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);

        await CommonActions.CardBlock(this, play);

        int existingLien = DebtEngine.GetLienAmount(play.Target);
        if (existingLien > 0 && Owner.Creature.IsAlive)
        {
            await CreatureCmd.GainBlock(Owner.Creature, existingLien, ValueProp.Move, play);
        }

        if (play.Target.IsAlive)
        {
            await DebtEngine.ApplyLien(
                choiceContext,
                play.Target,
                Owner.Creature,
                DynamicVars["Lien"].IntValue);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
        DynamicVars["Lien"].UpgradeValueBy(1m);
    }
}
