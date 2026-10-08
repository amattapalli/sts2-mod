using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Usurer.UsurerCode.Util;

namespace Usurer.UsurerCode.Cards;

/// <summary>
/// Payday — Common Skill, cost 1, AnyEnemy.
/// Gain 5 (8) Gold. Apply 3 (4) Lien. Draw 2 cards.
/// </summary>
public sealed class Payday() : UsurerCard(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new GoldVar(5),
        new DynamicVar("Lien", 3m),
        new CardsVar(2)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);

        await PlayerCmd.GainGold(DynamicVars.Gold.IntValue, Owner);

        await DebtEngine.ApplyLien(
            choiceContext,
            play.Target,
            Owner.Creature,
            DynamicVars["Lien"].IntValue);

        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Gold.UpgradeValueBy(3m);
        DynamicVars["Lien"].UpgradeValueBy(1m);
    }
}
