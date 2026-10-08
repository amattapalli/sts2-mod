using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using Usurer.UsurerCode.Util;

namespace Usurer.UsurerCode.Cards;

/// <summary>
/// Soul Clause — Token Skill, cost 0, AnyEnemy, Exhaust.
/// Apply 6 (9) Lien and 2 (3) Vulnerable.
/// </summary>
[Pool(typeof(TokenCardPool))]
public sealed class SoulClauseToken() : UsurerCard(0, CardType.Skill, CardRarity.Token, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Lien", 6m),
        new DynamicVar("Vulnerable", 2m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);

        await DebtEngine.ApplyLien(
            choiceContext,
            play.Target,
            Owner.Creature,
            DynamicVars["Lien"].IntValue);

        if (play.Target.IsAlive)
        {
            await PowerCmd.Apply<VulnerablePower>(
                choiceContext,
                play.Target,
                DynamicVars["Vulnerable"].IntValue,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Lien"].UpgradeValueBy(3m);
        DynamicVars["Vulnerable"].UpgradeValueBy(1m);
    }
}
