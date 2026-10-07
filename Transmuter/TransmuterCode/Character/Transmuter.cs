using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using Transmuter.TransmuterCode.Cards;
using Transmuter.TransmuterCode.Extensions;
using Transmuter.TransmuterCode.Relics;

namespace Transmuter.TransmuterCode.Character;

public class Transmuter : PlaceholderCharacterModel
{
    public const string CharacterId = "Transmuter";

    public static readonly Color Color = new("d99b26");

    public override string PlaceholderID => "silent";
    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Neutral;
    public override int StartingHp => 74;

    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<StrikeTransmuter>(),
        ModelDb.Card<StrikeTransmuter>(),
        ModelDb.Card<StrikeTransmuter>(),
        ModelDb.Card<StrikeTransmuter>(),
        ModelDb.Card<DefendTransmuter>(),
        ModelDb.Card<DefendTransmuter>(),
        ModelDb.Card<DefendTransmuter>(),
        ModelDb.Card<DefendTransmuter>(),
        ModelDb.Card<BrimstoneToss>(),
        ModelDb.Card<SalineSolution>()
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<CrackedAlembic>()
    ];
    
    public override CardPoolModel CardPool => ModelDb.CardPool<TransmuterCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<TransmuterRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<TransmuterPotionPool>();
    
    /*  PlaceholderCharacterModel will utilize placeholder basegame assets for most of your character assets until you
        override all the other methods that define those assets. 
        These are just some of the simplest assets, given some placeholders to differentiate your character with. 
        You don't have to, but you're suggested to rename these images. */
    public override Control CustomIcon
    {
        get
        {
            var icon = NodeFactory<Control>.CreateFromResource(CustomIconTexturePath);
            icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            return icon;
        }
    }
    public override string CustomIconTexturePath => "character_icon_char_name.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "char_select_char_name.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "char_select_char_name_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "map_marker_char_name.png".CharacterUiPath();
}