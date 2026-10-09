using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Usurer.UsurerCode.Cards;
using Usurer.UsurerCode.Extensions;
using Usurer.UsurerCode.Relics;

namespace Usurer.UsurerCode.Character;

/// <summary>
/// The Usurer — Playable Slay the Spire 2 character centered around Borrowed Debt,
/// Liens, Moratoriums, and Foreclosures.
/// </summary>
public class Usurer : PlaceholderCharacterModel
{
    public const string CharacterId = "Usurer";

    public static readonly Color Color = new("c89b3c");

    public override string PlaceholderID => "regent";
    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Neutral;
    public override int StartingHp => 75;
    public override int StartingGold => 150;

    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<StrikeUsurer>(),
        ModelDb.Card<StrikeUsurer>(),
        ModelDb.Card<StrikeUsurer>(),
        ModelDb.Card<StrikeUsurer>(),
        ModelDb.Card<DefendUsurer>(),
        ModelDb.Card<DefendUsurer>(),
        ModelDb.Card<DefendUsurer>(),
        ModelDb.Card<DefendUsurer>(),
        ModelDb.Card<PredatoryLoan>(),
        ModelDb.Card<Audit>()
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<InfernalLedger>()
    ];

    public override CardPoolModel CardPool => ModelDb.CardPool<UsurerCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<UsurerRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<UsurerPotionPool>();

    public override float DeathAnimTime => 0.85f;

    protected override IEnumerable<string> ExtraAssetPaths =>
    [
        "char_select_bg_usurer.png".CharacterUiPath()
    ];

    public override NCreatureVisuals? CreateCustomVisuals()
    {
        var path = "usurer_combat.png".CharacterUiPath();
        return ResourceLoader.Exists(path)
            ? UsurerVisualsAndUiPatch.AttachCombatAnimations(
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

    public override string CustomIconTexturePath => "character_icon_usurer.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "char_select_usurer.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "char_select_usurer_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "map_marker_usurer.png".CharacterUiPath();
}
