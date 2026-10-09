using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Usurer.UsurerCode.Character;
using Usurer.UsurerCode.Extensions;
using Usurer.UsurerCode.Util;

namespace Usurer.UsurerCode.Cards;

/// <summary>
/// Base class for all Usurer cards, wiring portrait paths and automatic keyword hover tips.
/// </summary>
[Pool(typeof(UsurerCardPool))]
public abstract class UsurerCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    CustomCardModel(cost, type, rarity, target)
{
    public override CardPoolModel VisualCardPool => ModelDb.CardPool<UsurerCardPool>();

    public override string CustomPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePath();
    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
    public override string BetaPortraitPath => $"beta/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        UsurerHoverTips.ForCard(Id.Entry);

    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card == this && Owner?.Creature != null)
        {
            string trigger = Type == CardType.Attack ? "Attack" : "Cast";
            await CreatureCmd.TriggerAnim(Owner.Creature, trigger, 0f);
        }
    }
}
