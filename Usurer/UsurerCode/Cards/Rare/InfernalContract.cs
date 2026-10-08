using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Usurer.UsurerCode.Cards;

/// <summary>
/// Infernal Contract — Rare Skill, cost 1, Self.
/// Choose 1 of 3 generated 0-cost Clause token cards (BloodClauseToken, GoldClauseToken, SoulClauseToken;
/// upgraded if this card is upgraded) to add to your Hand.
/// </summary>
public sealed class InfernalContract() : UsurerCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<BloodClauseToken>(IsUpgraded),
        HoverTipFactory.FromCard<GoldClauseToken>(IsUpgraded),
        HoverTipFactory.FromCard<SoulClauseToken>(IsUpgraded)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null)
            return;

        List<CardModel> choices =
        [
            CombatState.CreateCard<BloodClauseToken>(Owner),
            CombatState.CreateCard<GoldClauseToken>(Owner),
            CombatState.CreateCard<SoulClauseToken>(Owner)
        ];

        if (IsUpgraded)
        {
            foreach (CardModel option in choices)
            {
                CardCmd.Upgrade(option);
            }
        }

        CardModel? selected = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            choices,
            Owner,
            canSkip: false);

        if (selected != null)
        {
            await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, Owner);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
