using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using Transmuter.TransmuterCode.Util;

namespace Transmuter.TransmuterCode.Cards;

/// <summary>
/// Rubedo — Token Skill, cost 0, AnyEnemy, Exhaust.
/// Apply 6 (8) Sulfur.
/// </summary>
[Pool(typeof(TokenCardPool))]
public sealed class RubedoToken() : TransmuterCard(0, CardType.Skill, CardRarity.Token, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Sulfur", 6m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);

        await ReactionEngine.ApplyReagent(
            choiceContext,
            play.Target,
            Owner.Creature,
            ReagentType.Sulfur,
            DynamicVars["Sulfur"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Sulfur"].UpgradeValueBy(2m);
    }
}
