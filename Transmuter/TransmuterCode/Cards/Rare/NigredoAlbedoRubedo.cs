using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Transmuter.TransmuterCode.Cards;

/// <summary>
/// Nigredo, Albedo, Rubedo — Rare Skill, cost 1, Self.
/// Choose 1 of 3 generated 0-cost token cards (NigredoToken, AlbedoToken, RubedoToken; upgraded if this card is upgraded) to add to Hand.
/// </summary>
public sealed class NigredoAlbedoRubedo() : TransmuterCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<NigredoToken>(IsUpgraded),
        HoverTipFactory.FromCard<AlbedoToken>(IsUpgraded),
        HoverTipFactory.FromCard<RubedoToken>(IsUpgraded)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null)
            return;

        List<CardModel> choices =
        [
            CombatState.CreateCard<NigredoToken>(Owner),
            CombatState.CreateCard<AlbedoToken>(Owner),
            CombatState.CreateCard<RubedoToken>(Owner)
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
