using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
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
    
    public override float DeathAnimTime => 0.85f;

    protected override IEnumerable<string> ExtraAssetPaths =>
    [
        "char_select_bg_transmuter.png".CharacterUiPath()
    ];

    public override NCreatureVisuals? CreateCustomVisuals()
    {
        var path = "transmuter_combat.png".CharacterUiPath();
        return ResourceLoader.Exists(path)
            ? TransmuterVisualsAndUiPatch.AttachCombatAnimations(
                NodeFactory<NCreatureVisuals>.CreateFromResource(path))
            : null;
    }

    public override Control CustomIcon
    {
        get
        {
            var icon = NodeFactory<Control>.CreateFromResource(CustomIconTexturePath);
            icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            return icon;
        }
    }
    public override string CustomIconTexturePath => "character_icon_transmuter.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "char_select_transmuter.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "char_select_transmuter_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "map_marker_transmuter.png".CharacterUiPath();
}