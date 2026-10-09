using System;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using Usurer.UsurerCode.Extensions;

namespace Usurer.UsurerCode.Character;

/// <summary>
/// Provides programmatic combat animations (<c>Idle</c>, <c>Attack</c>, <c>Cast</c>, <c>Hit</c>, <c>Dead</c>, <c>Revive</c>)
/// for The Usurer's <see cref="NCreatureVisuals"/>, patches the character selection screen
/// so selecting The Usurer displays the custom Usurer vault background instead of The Regent,
/// and replaces The Regent's energy counter / energy icon across all combat and UI surfaces.
/// </summary>
[HarmonyPatch]
public static class UsurerVisualsAndUiPatch
{
    private const string CombatAnimLibraryName = "";
    private static readonly FieldInfo? EnergyCounterPlayerField =
        AccessTools.Field(typeof(NEnergyCounter), "_player");
    private static readonly MethodInfo? EnergyCounterRefreshLabelMethod =
        AccessTools.Method(typeof(NEnergyCounter), "RefreshLabel");

    /// <summary>
    /// Ensures every model belonging to The Usurer (cards including Token cards, relics, powers, and potions)
    /// always resolves its energy icon pool to <see cref="UsurerCardPool"/> even outside of an active run
    /// or when a token card is in <c>TokenCardPool</c>.
    /// </summary>
    [HarmonyPatch(typeof(EnergyIconHelper), nameof(EnergyIconHelper.GetPool))]
    [HarmonyPostfix]
    public static void EnsureUsurerEnergyPool(AbstractModel model, ref CardPoolModel __result)
    {
        if (model != null && model.GetType().Assembly == typeof(Usurer).Assembly)
        {
            EnsureEnergyAssetsCached();
            __result = ModelDb.CardPool<UsurerCardPool>();
        }
    }

    /// <summary>
    /// Replaces The Regent's placeholder energy counter orb in the combat HUD with The Usurer's
    /// custom 3-layer sovereign gold-and-crimson treasury seal energy counter orb.
    /// </summary>
    [HarmonyPatch(typeof(NEnergyCounter), nameof(NEnergyCounter._Ready))]
    [HarmonyPostfix]
    public static void ApplyCustomCombatEnergyCounter(NEnergyCounter __instance)
    {
        if (EnergyCounterPlayerField?.GetValue(__instance) is not Player player ||
            player.Character is not Usurer)
        {
            return;
        }

        EnsureEnergyAssetsCached();

        var layers = __instance.GetNodeOrNull<Control>("%Layers");
        var rotationLayers = __instance.GetNodeOrNull<Control>("%RotationLayers");
        if (layers == null || rotationLayers == null)
            return;

        string basePath = "energy_orb_base.png".CharacterUiPath();
        string rotPath = "energy_orb_rot.png".CharacterUiPath();
        string corePath = "energy_orb_core.png".CharacterUiPath();

        if (!ResourceLoader.Exists(basePath) || !ResourceLoader.Exists(rotPath) || !ResourceLoader.Exists(corePath))
            return;

        var baseTex = ResourceLoader.Load<Texture2D>(basePath);
        var rotTex = ResourceLoader.Load<Texture2D>(rotPath);
        var coreTex = ResourceLoader.Load<Texture2D>(corePath);
        if (baseTex == null || rotTex == null || coreTex == null)
            return;

        foreach (Node child in layers.GetChildren().ToList())
        {
            if (child == rotationLayers)
                continue;
            layers.RemoveChild(child);
            child.QueueFree();
        }

        foreach (Node child in rotationLayers.GetChildren().ToList())
        {
            rotationLayers.RemoveChild(child);
            child.QueueFree();
        }

        if (rotationLayers.GetParent() != layers)
        {
            rotationLayers.GetParent()?.RemoveChild(rotationLayers);
            layers.AddChild(rotationLayers);
        }

        rotationLayers.Position = Vector2.Zero;
        rotationLayers.Size = new Vector2(128f, 128f);
        rotationLayers.MouseFilter = Control.MouseFilterEnum.Ignore;

        var baseRect = CreateOrbLayerRect("UsurerOrbBase", baseTex);
        layers.AddChild(baseRect);
        layers.MoveChild(baseRect, 0);

        var rotRect = CreateOrbLayerRect("UsurerOrbRot", rotTex);
        rotRect.PivotOffset = new Vector2(64f, 64f);
        rotationLayers.AddChild(rotRect);

        var coreRect = CreateOrbLayerRect("UsurerOrbCore", coreTex);
        layers.AddChild(coreRect);

        Color goldCrimsonBurst = new(1.0f, 0.72f, 0.24f, 1.0f);
        if (__instance.GetNodeOrNull<CpuParticles2D>("%BurstBack") is { } burstBack)
        {
            burstBack.SelfModulate = goldCrimsonBurst;
        }
        if (__instance.GetNodeOrNull<CpuParticles2D>("%BurstFront") is { } burstFront)
        {
            burstFront.SelfModulate = goldCrimsonBurst;
        }

        EnergyCounterRefreshLabelMethod?.Invoke(__instance, null);
    }

    private static TextureRect CreateOrbLayerRect(string name, Texture2D texture)
    {
        var rect = new TextureRect
        {
            Name = name,
            Texture = texture,
            Position = Vector2.Zero,
            Size = new Vector2(128f, 128f),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        rect.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        return rect;
    }

    private static void EnsureEnergyAssetsCached()
    {
        foreach (string file in new[] { "big_energy.png", "text_energy.png", "energy_orb_base.png", "energy_orb_rot.png", "energy_orb_core.png" })
        {
            string path = file.CharacterUiPath();
            if (!PreloadManager.Cache.ContainsKey(path) && ResourceLoader.Exists(path))
            {
                var tex = ResourceLoader.Load<Texture2D>(path);
                if (tex != null)
                {
                    PreloadManager.Cache.SetAsset(path, tex);
                }
            }
        }
    }

    /// <summary>
    /// Attaches a Godot <see cref="AnimationPlayer"/> with full combat animations to The Usurer's
    /// <see cref="NCreatureVisuals"/> node so <c>BaseLib</c>'s <c>CustomAnimationPatch</c> plays
    /// movement and visual effects during combat.
    /// </summary>
    public static NCreatureVisuals AttachCombatAnimations(NCreatureVisuals visualsNode)
    {
        if (visualsNode.GetNodeOrNull<AnimationPlayer>("AnimationPlayer") != null)
            return visualsNode;

        var sprite = visualsNode.GetNodeOrNull<Sprite2D>("Visuals");
        Vector2 basePos = sprite?.Position ?? new Vector2(0f, -160f);
        Vector2 baseScale = sprite?.Scale ?? Vector2.One;

        var animPlayer = new AnimationPlayer
        {
            Name = "AnimationPlayer"
        };
        visualsNode.AddChild(animPlayer);
        animPlayer.RootNode = new NodePath("..");

        var library = new AnimationLibrary();

        Animation idleAnim = CreateIdleAnimation(basePos, baseScale);
        library.AddAnimation("Idle", idleAnim);
        library.AddAnimation("idle", (Animation)idleAnim.Duplicate());

        Animation attackAnim = CreateAttackAnimation(basePos, baseScale);
        library.AddAnimation("Attack", attackAnim);
        library.AddAnimation("attack", (Animation)attackAnim.Duplicate());

        Animation castAnim = CreateCastAnimation(basePos, baseScale);
        library.AddAnimation("Cast", castAnim);
        library.AddAnimation("cast", (Animation)castAnim.Duplicate());

        Animation hitAnim = CreateHitAnimation(basePos, baseScale);
        library.AddAnimation("Hit", hitAnim);
        library.AddAnimation("Hurt", (Animation)hitAnim.Duplicate());
        library.AddAnimation("hit", (Animation)hitAnim.Duplicate());
        library.AddAnimation("hurt", (Animation)hitAnim.Duplicate());

        Animation deadAnim = CreateDeadAnimation(basePos, baseScale);
        library.AddAnimation("Dead", deadAnim);
        library.AddAnimation("Die", (Animation)deadAnim.Duplicate());
        library.AddAnimation("dead", (Animation)deadAnim.Duplicate());
        library.AddAnimation("die", (Animation)deadAnim.Duplicate());

        Animation reviveAnim = CreateReviveAnimation(basePos, baseScale);
        library.AddAnimation("Revive", reviveAnim);
        library.AddAnimation("revive", (Animation)reviveAnim.Duplicate());

        animPlayer.AddAnimationLibrary(CombatAnimLibraryName, library);

        foreach (string transientAnim in new[] { "Attack", "attack", "Cast", "cast", "Hit", "Hurt", "hit", "hurt", "Revive", "revive" })
        {
            animPlayer.AnimationSetNext(transientAnim, "Idle");
        }

        animPlayer.AnimationFinished += animName =>
        {
            string name = animName.ToString();
            if (!name.Equals("Dead", StringComparison.OrdinalIgnoreCase) &&
                !name.Equals("Die", StringComparison.OrdinalIgnoreCase))
            {
                animPlayer.Play("Idle");
            }
        };

        animPlayer.Autoplay = "Idle";
        visualsNode.Ready += () =>
        {
            if (GodotObject.IsInstanceValid(animPlayer) && !animPlayer.IsPlaying())
            {
                animPlayer.Play("Idle");
            }
        };

        return visualsNode;
    }

    /// <summary>
    /// Ensures rapid consecutive card plays (e.g., playing multiple Attacks or Skills back-to-back)
    /// immediately restart the creature's <see cref="AnimationPlayer"/> from frame 0.
    /// </summary>
    [HarmonyPatch(typeof(NCreature), nameof(NCreature.SetAnimationTrigger))]
    [HarmonyPrefix]
    public static void RestartCustomTriggerOnRepeat(NCreature __instance, string trigger)
    {
        if (__instance.HasSpineAnimation || __instance.Entity?.Player?.Character is not Usurer)
            return;

        var animPlayer = __instance.Visuals?.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        if (animPlayer != null && animPlayer.IsPlaying() && !string.Equals(trigger, "Idle", StringComparison.OrdinalIgnoreCase))
        {
            animPlayer.Stop();
        }
    }

    /// <summary>
    /// Ensures the character selection button for The Usurer uses its crisp 132x195 portrait icon.
    /// </summary>
    [HarmonyPatch(typeof(NCharacterSelectButton), nameof(NCharacterSelectButton.Init))]
    [HarmonyPostfix]
    public static void FixCharacterSelectButtonIcon(NCharacterSelectButton __instance, CharacterModel character)
    {
        if (character is not Usurer)
            return;

        string iconPath = __instance.IsLocked
            ? "char_select_usurer_locked.png".CharacterUiPath()
            : "char_select_usurer.png".CharacterUiPath();

        if (!ResourceLoader.Exists(iconPath))
            return;

        var texture = ResourceLoader.Load<Texture2D>(iconPath);
        if (texture == null)
            return;

        if (__instance.GetNodeOrNull<TextureRect>("%Icon") is { } icon)
        {
            icon.Texture = texture;
            icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        }

        if (__instance.GetNodeOrNull<TextureRect>("%IconAdd") is { } iconAdd)
        {
            iconAdd.Texture = texture;
            iconAdd.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            iconAdd.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        }
    }

    /// <summary>
    /// Replaces The Regent's placeholder background scene with The Usurer's custom 1920x1080 vault
    /// background when The Usurer is selected on the character select screen.
    /// </summary>
    [HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.SelectCharacter))]
    [HarmonyPostfix]
    public static void ApplyCustomCharacterSelectBackground(
        NCharacterSelectScreen __instance,
        NCharacterSelectButton charSelectButton,
        CharacterModel characterModel)
    {
        if (characterModel is not Usurer)
            return;

        var bgContainer = __instance.GetNodeOrNull<Control>("AnimatedBg");
        if (bgContainer == null)
            return;

        foreach (Node child in bgContainer.GetChildren())
        {
            bgContainer.RemoveChild(child);
            child.QueueFree();
        }

        if (charSelectButton.IsLocked)
            return;

        string bgPath = "char_select_bg_usurer.png".CharacterUiPath();
        if (!ResourceLoader.Exists(bgPath))
            return;

        var texture = ResourceLoader.Load<Texture2D>(bgPath);
        if (texture == null)
            return;

        var wrapper = new Control
        {
            Name = "Usurer_custom_bg",
            MouseFilter = Control.MouseFilterEnum.Ignore
        };

        var art = new TextureRect
        {
            Name = "BgArt",
            Texture = texture,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            Modulate = Colors.White
        };

        wrapper.AddChild(art);
        AttachBackgroundBreathingAnimation(wrapper, art);
        bgContainer.AddChild(wrapper);

        LayoutFullViewportBackground(__instance, art, texture);
        Callable.From(() =>
        {
            if (GodotObject.IsInstanceValid(__instance) && GodotObject.IsInstanceValid(art))
            {
                LayoutFullViewportBackground(__instance, art, texture);
            }
        }).CallDeferred();
    }

    private static void LayoutFullViewportBackground(Control screen, TextureRect art, Texture2D texture)
    {
        Vector2 viewportSize = screen.GetViewportRect().Size;
        if (viewportSize.X <= 0f || viewportSize.Y <= 0f)
        {
            viewportSize = new Vector2(1920f, 1080f);
        }

        float texWidth = Math.Max(1, texture.GetWidth());
        float texHeight = Math.Max(1, texture.GetHeight());
        float scale = Math.Max(viewportSize.X / texWidth, viewportSize.Y / texHeight);
        Vector2 size = new(texWidth * scale, texHeight * scale);

        art.Size = size;
        art.PivotOffset = size * 0.5f;
        art.GlobalPosition = (viewportSize - size) * 0.5f;
    }

    private static void AttachBackgroundBreathingAnimation(Control wrapper, TextureRect art)
    {
        var animPlayer = new AnimationPlayer
        {
            Name = "BgAnimationPlayer"
        };
        wrapper.AddChild(animPlayer);
        animPlayer.RootNode = new NodePath("..");

        var anim = new Animation
        {
            Length = 5.0f,
            LoopMode = Animation.LoopModeEnum.Linear
        };

        int scaleTrack = anim.AddTrack(Animation.TrackType.Value);
        anim.TrackSetPath(scaleTrack, new NodePath("BgArt:scale"));
        anim.TrackSetInterpolationType(scaleTrack, Animation.InterpolationType.Cubic);
        anim.TrackInsertKey(scaleTrack, 0.0f, Vector2.One);
        anim.TrackInsertKey(scaleTrack, 2.5f, new Vector2(1.022f, 1.022f));
        anim.TrackInsertKey(scaleTrack, 5.0f, Vector2.One);

        var lib = new AnimationLibrary();
        lib.AddAnimation("Idle", anim);
        animPlayer.AddAnimationLibrary("", lib);
        animPlayer.Autoplay = "Idle";
        wrapper.Ready += () =>
        {
            if (GodotObject.IsInstanceValid(animPlayer))
            {
                animPlayer.Play("Idle");
            }
        };
    }

    private static Animation CreateIdleAnimation(Vector2 basePos, Vector2 baseScale)
    {
        var anim = new Animation
        {
            Length = 2.4f,
            LoopMode = Animation.LoopModeEnum.Linear
        };

        int posTrack = AddValueTrack(anim, "Visuals:position");
        anim.TrackInsertKey(posTrack, 0.0f, basePos);
        anim.TrackInsertKey(posTrack, 0.6f, basePos + new Vector2(0f, -4f));
        anim.TrackInsertKey(posTrack, 1.2f, basePos + new Vector2(0f, -7f));
        anim.TrackInsertKey(posTrack, 1.8f, basePos + new Vector2(0f, -3f));
        anim.TrackInsertKey(posTrack, 2.4f, basePos);

        int scaleTrack = AddValueTrack(anim, "Visuals:scale");
        anim.TrackInsertKey(scaleTrack, 0.0f, baseScale);
        anim.TrackInsertKey(scaleTrack, 1.2f, new Vector2(baseScale.X * 1.018f, baseScale.Y * 1.032f));
        anim.TrackInsertKey(scaleTrack, 2.4f, baseScale);

        int rotTrack = AddValueTrack(anim, "Visuals:rotation");
        anim.TrackInsertKey(rotTrack, 0.0f, 0f);
        anim.TrackInsertKey(rotTrack, 0.6f, 0.015f);
        anim.TrackInsertKey(rotTrack, 1.8f, -0.015f);
        anim.TrackInsertKey(rotTrack, 2.4f, 0f);

        int modTrack = AddValueTrack(anim, "Visuals:self_modulate");
        anim.TrackInsertKey(modTrack, 0.0f, Colors.White);
        anim.TrackInsertKey(modTrack, 2.4f, Colors.White);

        return anim;
    }

    private static Animation CreateAttackAnimation(Vector2 basePos, Vector2 baseScale)
    {
        var anim = new Animation
        {
            Length = 0.48f,
            LoopMode = Animation.LoopModeEnum.None
        };

        int posTrack = AddValueTrack(anim, "Visuals:position");
        anim.TrackInsertKey(posTrack, 0.00f, basePos);
        anim.TrackInsertKey(posTrack, 0.07f, basePos + new Vector2(-32f, 6f));
        anim.TrackInsertKey(posTrack, 0.17f, basePos + new Vector2(150f, -24f));
        anim.TrackInsertKey(posTrack, 0.25f, basePos + new Vector2(115f, -12f));
        anim.TrackInsertKey(posTrack, 0.37f, basePos + new Vector2(-12f, 3f));
        anim.TrackInsertKey(posTrack, 0.48f, basePos);

        int rotTrack = AddValueTrack(anim, "Visuals:rotation");
        anim.TrackInsertKey(rotTrack, 0.00f, 0f);
        anim.TrackInsertKey(rotTrack, 0.07f, -0.11f);
        anim.TrackInsertKey(rotTrack, 0.17f, 0.19f);
        anim.TrackInsertKey(rotTrack, 0.25f, 0.10f);
        anim.TrackInsertKey(rotTrack, 0.37f, -0.03f);
        anim.TrackInsertKey(rotTrack, 0.48f, 0f);

        int scaleTrack = AddValueTrack(anim, "Visuals:scale");
        anim.TrackInsertKey(scaleTrack, 0.00f, baseScale);
        anim.TrackInsertKey(scaleTrack, 0.07f, new Vector2(baseScale.X * 0.93f, baseScale.Y * 1.06f));
        anim.TrackInsertKey(scaleTrack, 0.17f, new Vector2(baseScale.X * 1.15f, baseScale.Y * 0.92f));
        anim.TrackInsertKey(scaleTrack, 0.25f, new Vector2(baseScale.X * 1.06f, baseScale.Y * 0.97f));
        anim.TrackInsertKey(scaleTrack, 0.48f, baseScale);

        int modTrack = AddValueTrack(anim, "Visuals:self_modulate");
        anim.TrackInsertKey(modTrack, 0.00f, Colors.White);
        anim.TrackInsertKey(modTrack, 0.48f, Colors.White);

        return anim;
    }

    private static Animation CreateCastAnimation(Vector2 basePos, Vector2 baseScale)
    {
        var anim = new Animation
        {
            Length = 0.52f,
            LoopMode = Animation.LoopModeEnum.None
        };

        int posTrack = AddValueTrack(anim, "Visuals:position");
        anim.TrackInsertKey(posTrack, 0.00f, basePos);
        anim.TrackInsertKey(posTrack, 0.12f, basePos + new Vector2(22f, -30f));
        anim.TrackInsertKey(posTrack, 0.26f, basePos + new Vector2(30f, -36f));
        anim.TrackInsertKey(posTrack, 0.39f, basePos + new Vector2(10f, -10f));
        anim.TrackInsertKey(posTrack, 0.52f, basePos);

        int rotTrack = AddValueTrack(anim, "Visuals:rotation");
        anim.TrackInsertKey(rotTrack, 0.00f, 0f);
        anim.TrackInsertKey(rotTrack, 0.12f, -0.08f);
        anim.TrackInsertKey(rotTrack, 0.26f, 0.07f);
        anim.TrackInsertKey(rotTrack, 0.52f, 0f);

        int scaleTrack = AddValueTrack(anim, "Visuals:scale");
        anim.TrackInsertKey(scaleTrack, 0.00f, baseScale);
        anim.TrackInsertKey(scaleTrack, 0.18f, new Vector2(baseScale.X * 1.09f, baseScale.Y * 1.10f));
        anim.TrackInsertKey(scaleTrack, 0.34f, new Vector2(baseScale.X * 1.04f, baseScale.Y * 1.04f));
        anim.TrackInsertKey(scaleTrack, 0.52f, baseScale);

        int modTrack = AddValueTrack(anim, "Visuals:self_modulate");
        anim.TrackInsertKey(modTrack, 0.00f, Colors.White);
        anim.TrackInsertKey(modTrack, 0.18f, new Color(1.24f, 1.14f, 0.85f, 1f));
        anim.TrackInsertKey(modTrack, 0.52f, Colors.White);

        return anim;
    }

    private static Animation CreateHitAnimation(Vector2 basePos, Vector2 baseScale)
    {
        var anim = new Animation
        {
            Length = 0.42f,
            LoopMode = Animation.LoopModeEnum.None
        };

        int posTrack = AddValueTrack(anim, "Visuals:position");
        anim.TrackInsertKey(posTrack, 0.00f, basePos);
        anim.TrackInsertKey(posTrack, 0.06f, basePos + new Vector2(-54f, 8f));
        anim.TrackInsertKey(posTrack, 0.15f, basePos + new Vector2(-36f, -4f));
        anim.TrackInsertKey(posTrack, 0.26f, basePos + new Vector2(-16f, 4f));
        anim.TrackInsertKey(posTrack, 0.42f, basePos);

        int rotTrack = AddValueTrack(anim, "Visuals:rotation");
        anim.TrackInsertKey(rotTrack, 0.00f, 0f);
        anim.TrackInsertKey(rotTrack, 0.06f, -0.17f);
        anim.TrackInsertKey(rotTrack, 0.18f, -0.09f);
        anim.TrackInsertKey(rotTrack, 0.42f, 0f);

        int scaleTrack = AddValueTrack(anim, "Visuals:scale");
        anim.TrackInsertKey(scaleTrack, 0.00f, baseScale);
        anim.TrackInsertKey(scaleTrack, 0.06f, new Vector2(baseScale.X * 0.89f, baseScale.Y * 1.09f));
        anim.TrackInsertKey(scaleTrack, 0.22f, new Vector2(baseScale.X * 1.04f, baseScale.Y * 0.96f));
        anim.TrackInsertKey(scaleTrack, 0.42f, baseScale);

        int modTrack = AddValueTrack(anim, "Visuals:self_modulate");
        anim.TrackInsertKey(modTrack, 0.00f, Colors.White);
        anim.TrackInsertKey(modTrack, 0.05f, new Color(1f, 0.42f, 0.42f, 1f));
        anim.TrackInsertKey(modTrack, 0.22f, new Color(1f, 0.75f, 0.75f, 1f));
        anim.TrackInsertKey(modTrack, 0.42f, Colors.White);

        return anim;
    }

    private static Animation CreateDeadAnimation(Vector2 basePos, Vector2 baseScale)
    {
        var anim = new Animation
        {
            Length = 0.85f,
            LoopMode = Animation.LoopModeEnum.None
        };

        int posTrack = AddValueTrack(anim, "Visuals:position");
        anim.TrackInsertKey(posTrack, 0.00f, basePos);
        anim.TrackInsertKey(posTrack, 0.18f, basePos + new Vector2(-30f, -10f));
        anim.TrackInsertKey(posTrack, 0.55f, basePos + new Vector2(-75f, 95f));
        anim.TrackInsertKey(posTrack, 0.85f, basePos + new Vector2(-90f, 125f));

        int rotTrack = AddValueTrack(anim, "Visuals:rotation");
        anim.TrackInsertKey(rotTrack, 0.00f, 0f);
        anim.TrackInsertKey(rotTrack, 0.18f, -0.22f);
        anim.TrackInsertKey(rotTrack, 0.55f, -1.15f);
        anim.TrackInsertKey(rotTrack, 0.85f, -1.45f);

        int scaleTrack = AddValueTrack(anim, "Visuals:scale");
        anim.TrackInsertKey(scaleTrack, 0.00f, baseScale);
        anim.TrackInsertKey(scaleTrack, 0.55f, new Vector2(baseScale.X * 0.95f, baseScale.Y * 0.88f));
        anim.TrackInsertKey(scaleTrack, 0.85f, new Vector2(baseScale.X * 0.90f, baseScale.Y * 0.80f));

        int modTrack = AddValueTrack(anim, "Visuals:self_modulate");
        anim.TrackInsertKey(modTrack, 0.00f, Colors.White);
        anim.TrackInsertKey(modTrack, 0.40f, new Color(0.65f, 0.55f, 0.55f, 0.85f));
        anim.TrackInsertKey(modTrack, 0.85f, new Color(0.35f, 0.30f, 0.30f, 0.0f));

        return anim;
    }

    private static Animation CreateReviveAnimation(Vector2 basePos, Vector2 baseScale)
    {
        var anim = new Animation
        {
            Length = 0.55f,
            LoopMode = Animation.LoopModeEnum.None
        };

        int posTrack = AddValueTrack(anim, "Visuals:position");
        anim.TrackInsertKey(posTrack, 0.00f, basePos + new Vector2(-75f, 95f));
        anim.TrackInsertKey(posTrack, 0.35f, basePos + new Vector2(0f, -18f));
        anim.TrackInsertKey(posTrack, 0.55f, basePos);

        int rotTrack = AddValueTrack(anim, "Visuals:rotation");
        anim.TrackInsertKey(rotTrack, 0.00f, -1.15f);
        anim.TrackInsertKey(rotTrack, 0.35f, 0.05f);
        anim.TrackInsertKey(rotTrack, 0.55f, 0f);

        int scaleTrack = AddValueTrack(anim, "Visuals:scale");
        anim.TrackInsertKey(scaleTrack, 0.00f, new Vector2(baseScale.X * 0.90f, baseScale.Y * 0.80f));
        anim.TrackInsertKey(scaleTrack, 0.35f, new Vector2(baseScale.X * 1.05f, baseScale.Y * 1.05f));
        anim.TrackInsertKey(scaleTrack, 0.55f, baseScale);

        int modTrack = AddValueTrack(anim, "Visuals:self_modulate");
        anim.TrackInsertKey(modTrack, 0.00f, new Color(0.5f, 0.5f, 0.5f, 0.2f));
        anim.TrackInsertKey(modTrack, 0.55f, Colors.White);

        return anim;
    }

    private static int AddValueTrack(Animation animation, string nodePath)
    {
        int trackIdx = animation.AddTrack(Animation.TrackType.Value);
        animation.TrackSetPath(trackIdx, new NodePath(nodePath));
        animation.TrackSetInterpolationType(trackIdx, Animation.InterpolationType.Cubic);
        return trackIdx;
    }
}
