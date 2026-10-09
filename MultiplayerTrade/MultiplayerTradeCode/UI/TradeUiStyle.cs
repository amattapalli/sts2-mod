using System.IO;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.Fonts;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MultiplayerTrade.MultiplayerTradeCode.UI;

/// <summary>
/// Centralized visual styling, texture loader, Kreon typography, color palette, and tween helper
/// that makes all <c>MultiplayerTrade</c> UI elements match vanilla <i>Slay the Spire 2</i>'s
/// dark-fantasy comic ink and cel-shaded aesthetic.
/// </summary>
public static class TradeUiStyle
{
    /// <summary>
    /// Standard click sound effect path in <i>Slay the Spire 2</i>.
    /// </summary>
    public const string ClickSfx = "event:/sfx/ui/clicks/ui_click";

    /// <summary>
    /// Standard back/cancel sound effect path in <i>Slay the Spire 2</i>.
    /// </summary>
    public const string BackSfx = "event:/sfx/ui/clicks/ui_back";

    /// <summary>
    /// Standard button hover sound effect path in <i>Slay the Spire 2</i>.
    /// </summary>
    public const string HoverSfx = "event:/sfx/ui/clicks/ui_hover";

    /// <summary>
    /// Warm dark-charcoal comic ink outline color matching STS2 UI labels (<c>#211B17</c>).
    /// </summary>
    public static readonly Color InkOutlineColor = new("211B17");

    /// <summary>
    /// Deep dungeon stone background color used for the barter modal (<c>#191412</c>).
    /// </summary>
    public static readonly Color StonePanelBg = new(0.10f, 0.08f, 0.07f, 0.98f);

    /// <summary>
    /// Recessed warm slate background color used for offer wells (<c>#14100E</c>).
    /// </summary>
    public static readonly Color RecessedWellBg = new(0.08f, 0.06f, 0.055f, 0.96f);

    /// <summary>
    /// Burnished antique brass border color (<c>#C89842</c>).
    /// </summary>
    public static readonly Color BrassBorder = new(0.78f, 0.60f, 0.26f, 0.96f);

    /// <summary>
    /// Muted weathered bronze border color for unlocked/neutral panels (<c>#6E563A</c>).
    /// </summary>
    public static readonly Color WeatheredBronzeBorder = new(0.43f, 0.34f, 0.23f, 0.92f);

    private static Font? _cachedRegularFont;
    private static Font? _cachedBoldFont;

    /// <summary>
    /// Resolves a texture path inside <c>res://MultiplayerTrade/images/ui/</c> and caches it in <see cref="PreloadManager"/>.
    /// </summary>
    /// <param name="fileName">The PNG filename under <c>images/ui</c>.</param>
    /// <returns>The loaded <see cref="Texture2D"/>, or <c>null</c> if unavailable.</returns>
    public static Texture2D? LoadUiTexture(string fileName)
    {
        string path = Path.Join(MainFile.ResPath, "images", "ui", fileName);
        if (PreloadManager.Cache.ContainsKey(path))
        {
            return PreloadManager.Cache.GetTexture2D(path);
        }

        if (!ResourceLoader.Exists(path))
        {
            return null;
        }

        Texture2D? tex = ResourceLoader.Load<Texture2D>(path);
        if (tex != null)
        {
            PreloadManager.Cache.SetAsset(path, tex);
        }

        return tex;
    }

    /// <summary>
    /// Resolves STS2's native Kreon font (or active locale substitute font) from the live scene tree or resource pack.
    /// </summary>
    /// <param name="bold">Whether to request the bold variant.</param>
    /// <returns>The resolved <see cref="Font"/>, or <c>null</c> if running headless without resources.</returns>
    public static Font? GetStsFont(bool bold = false)
    {
        if (bold && _cachedBoldFont != null)
        {
            return _cachedBoldFont;
        }

        if (!bold && _cachedRegularFont != null)
        {
            return _cachedRegularFont;
        }

        string[] candidatePaths = bold
            ? new[]
            {
                "res://themes/kreon_bold_shared.tres",
                "res://themes/fonts/kreon_bold_shared.tres",
                "res://themes/kreon_regular_shared.tres"
            }
            : new[]
            {
                "res://themes/kreon_regular_shared.tres",
                "res://themes/fonts/kreon_regular_shared.tres",
                "res://themes/kreon_bold_shared.tres"
            };

        foreach (string path in candidatePaths)
        {
            if (ResourceLoader.Exists(path))
            {
                Font? loaded = ResourceLoader.Load<Font>(path, null, ResourceLoader.CacheMode.Reuse);
                if (loaded != null)
                {
                    if (bold)
                    {
                        _cachedBoldFont = loaded;
                    }
                    else
                    {
                        _cachedRegularFont = loaded;
                    }
                    return loaded;
                }
            }
        }

        // Fallback: inspect live MegaLabel in NRestSiteRoom, NMerchantRoom, or NRun.GlobalUi
        Label? sampleLabel =
            NRestSiteRoom.Instance?.GetNodeOrNull<Label>("%Header") ??
            NRun.Instance?.GlobalUi?.TopBar?.GetNodeOrNull<Label>("%FloorLabel");
        if (sampleLabel != null && sampleLabel.HasThemeFontOverride(ThemeConstants.Label.font))
        {
            Font? liveFont = sampleLabel.GetThemeFont(ThemeConstants.Label.font);
            if (liveFont != null)
            {
                _cachedRegularFont ??= liveFont;
                _cachedBoldFont ??= liveFont;
                return liveFont;
            }
        }

        return null;
    }

    /// <summary>
    /// Creates a <see cref="Label"/> styled with <i>Slay the Spire 2</i>'s Kreon font, thick dark comic ink outline,
    /// subtle drop shadow, and <see cref="StsColors"/> palette.
    /// </summary>
    /// <param name="text">Initial label text.</param>
    /// <param name="fontSize">Font size in pixels.</param>
    /// <param name="color">Text fill color (defaults to <see cref="StsColors.cream"/>).</param>
    /// <param name="bold">Whether to use the bold Kreon typeface.</param>
    /// <param name="outlineSize">Ink outline thickness in pixels.</param>
    /// <param name="alignment">Horizontal text alignment.</param>
    /// <returns>A configured <see cref="Label"/> matching STS2's native typography.</returns>
    public static Label CreateStsLabel(
        string text,
        int fontSize = 20,
        Color? color = null,
        bool bold = true,
        int outlineSize = 8,
        HorizontalAlignment alignment = HorizontalAlignment.Left)
    {
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = alignment,
            VerticalAlignment = VerticalAlignment.Center
        };

        ApplyStsLabelTheme(label, fontSize, color ?? StsColors.cream, bold, outlineSize);
        return label;
    }

    /// <summary>
    /// Applies STS2 Kreon typography, dark ink outlines, and drop shadow overrides to an existing <see cref="Label"/>.
    /// </summary>
    /// <param name="label">Target label control.</param>
    /// <param name="fontSize">Font size in pixels.</param>
    /// <param name="color">Primary text color.</param>
    /// <param name="bold">Whether to apply bold Kreon font.</param>
    /// <param name="outlineSize">Outline thickness in pixels.</param>
    public static void ApplyStsLabelTheme(
        Label label,
        int fontSize,
        Color color,
        bool bold = true,
        int outlineSize = 8)
    {
        Font? stsFont = GetStsFont(bold);
        if (stsFont != null)
        {
            label.AddThemeFontOverride(ThemeConstants.Label.font, stsFont);
        }

        label.ApplyLocaleFontSubstitution(bold ? FontType.Bold : FontType.Regular, ThemeConstants.Label.font);
        label.AddThemeFontSizeOverride(ThemeConstants.Label.fontSize, fontSize);
        label.AddThemeColorOverride(ThemeConstants.Label.fontColor, color);
        label.AddThemeConstantOverride(ThemeConstants.Label.outlineSize, outlineSize);
        label.AddThemeColorOverride(ThemeConstants.Label.fontOutlineColor, InkOutlineColor);
        label.AddThemeColorOverride(ThemeConstants.Label.fontShadowColor, new Color(0f, 0f, 0f, 0.58f));
        label.AddThemeConstantOverride("shadow_offset_x", 2);
        label.AddThemeConstantOverride("shadow_offset_y", 2);
    }

    /// <summary>
    /// Creates an ornate dark-fantasy STS2 plaque <see cref="Button"/> backed by hand-inked stone/brass textures
    /// (<c>trade_button_plaque.png</c>), optional die-cut icon, Kreon outlined text, hover/press scale tweens,
    /// and native STS2 click/hover sound effects.
    /// </summary>
    /// <param name="name">Node name for the button.</param>
    /// <param name="text">Button label text.</param>
    /// <param name="minSize">Minimum button dimensions.</param>
    /// <param name="fontSize">Font size in pixels.</param>
    /// <param name="tint">Optional modulate tint for the button plaque (e.g., warm gold, emerald confirm, or crimson cancel).</param>
    /// <param name="iconFileName">Optional icon filename under <c>images/ui/</c> (e.g. <c>"trade_icon.png"</c>).</param>
    /// <param name="iconSize">Display size for the icon.</param>
    /// <param name="isBackOrCancel">If true, plays <see cref="BackSfx"/> instead of <see cref="ClickSfx"/> on press.</param>
    /// <returns>A styled <see cref="Button"/> with STS2 visuals, tweens, and audio.</returns>
    public static Button CreatePlaqueButton(
        string name,
        string text,
        Vector2 minSize,
        int fontSize = 20,
        Color? tint = null,
        string? iconFileName = null,
        Vector2? iconSize = null,
        bool isBackOrCancel = false)
    {
        Color baseTint = tint ?? Colors.White;

        var button = new Button
        {
            Name = name,
            Text = string.Empty,
            CustomMinimumSize = minSize,
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
            FocusMode = Control.FocusModeEnum.None
        };

        Texture2D? normalTex = LoadUiTexture("trade_button_plaque.png");
        Texture2D? hoverTex = LoadUiTexture("trade_button_plaque_hover.png") ?? normalTex;

        if (normalTex != null && hoverTex != null)
        {
            button.AddThemeStyleboxOverride("normal", CreateNinePatchPlaqueStyle(normalTex, baseTint));
            button.AddThemeStyleboxOverride("hover", CreateNinePatchPlaqueStyle(hoverTex, baseTint.Lightened(0.10f)));
            button.AddThemeStyleboxOverride("pressed", CreateNinePatchPlaqueStyle(normalTex, baseTint.Darkened(0.16f)));
            button.AddThemeStyleboxOverride("disabled", CreateNinePatchPlaqueStyle(normalTex, new Color(0.48f, 0.45f, 0.42f, 0.65f)));
            button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
        }
        else
        {
            StyleBoxFlat fallback = CreateStonePanelStyle(
                new Color(0.18f, 0.14f, 0.12f, 0.96f) * baseTint,
                BrassBorder,
                borderWidth: 3,
                cornerRadius: 10,
                contentMargin: 12);
            button.AddThemeStyleboxOverride("normal", fallback);
            button.AddThemeStyleboxOverride("hover", CreateStonePanelStyle(
                new Color(0.26f, 0.19f, 0.15f, 0.98f) * baseTint,
                StsColors.gold,
                borderWidth: 3,
                cornerRadius: 10,
                contentMargin: 12));
            button.AddThemeStyleboxOverride("pressed", fallback);
            button.AddThemeStyleboxOverride("disabled", CreateStonePanelStyle(
                new Color(0.12f, 0.11f, 0.10f, 0.65f),
                WeatheredBronzeBorder,
                borderWidth: 2,
                cornerRadius: 10,
                contentMargin: 12));
        }

        // Centered HBox content inside the button so the die-cut icon and Kreon outlined label align cleanly
        var contentRow = new HBoxContainer
        {
            Name = "PlaqueContent",
            Alignment = BoxContainer.AlignmentMode.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        contentRow.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        contentRow.AddThemeConstantOverride("separation", 10);
        button.AddChildSafely(contentRow);

        if (!string.IsNullOrWhiteSpace(iconFileName))
        {
            Texture2D? iconTex = LoadUiTexture(iconFileName);
            if (iconTex != null)
            {
                Vector2 targetIconSize = iconSize ?? new Vector2(34f, 34f);
                var iconRect = new TextureRect
                {
                    Name = "Icon",
                    Texture = iconTex,
                    CustomMinimumSize = targetIconSize,
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                    MouseFilter = Control.MouseFilterEnum.Ignore
                };
                contentRow.AddChildSafely(iconRect);
            }
        }

        Label label = CreateStsLabel(
            text,
            fontSize: fontSize,
            color: StsColors.cream,
            bold: true,
            outlineSize: fontSize >= 18 ? 8 : 6,
            alignment: HorizontalAlignment.Center);
        label.Name = "Label";
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        contentRow.AddChildSafely(label);

        AttachStsButtonTweensAndAudio(button, label, isBackOrCancel);
        return button;
    }

    /// <summary>
    /// Updates the text of a button created by <see cref="CreatePlaqueButton"/>.
    /// </summary>
    /// <param name="button">The plaque button to update.</param>
    /// <param name="newText">New label text.</param>
    public static void SetPlaqueButtonText(Button button, string newText)
    {
        if (button.GetNodeOrNull<Label>("PlaqueContent/Label") is { } innerLabel)
        {
            innerLabel.Text = newText;
        }
        else
        {
            button.Text = newText;
        }
    }

    /// <summary>
    /// Creates a dark-fantasy stone and brass <see cref="StyleBoxFlat"/> matching STS2's popup frames.
    /// </summary>
    /// <param name="bgColor">Interior stone fill color.</param>
    /// <param name="borderColor">Outer brass/bronze trim color.</param>
    /// <param name="borderWidth">Border width in pixels.</param>
    /// <param name="cornerRadius">Corner radius in pixels.</param>
    /// <param name="contentMargin">Content padding in pixels.</param>
    /// <param name="shadowSize">Drop shadow size in pixels.</param>
    /// <returns>A configured <see cref="StyleBoxFlat"/>.</returns>
    public static StyleBoxFlat CreateStonePanelStyle(
        Color bgColor,
        Color borderColor,
        int borderWidth = 3,
        int cornerRadius = 10,
        int contentMargin = 18,
        int shadowSize = 14)
    {
        return new StyleBoxFlat
        {
            BgColor = bgColor,
            BorderColor = borderColor,
            BorderWidthLeft = borderWidth,
            BorderWidthTop = borderWidth,
            BorderWidthRight = borderWidth,
            BorderWidthBottom = borderWidth,
            CornerRadiusTopLeft = cornerRadius,
            CornerRadiusTopRight = cornerRadius,
            CornerRadiusBottomLeft = cornerRadius,
            CornerRadiusBottomRight = cornerRadius,
            ContentMarginLeft = contentMargin,
            ContentMarginTop = contentMargin,
            ContentMarginRight = contentMargin,
            ContentMarginBottom = contentMargin,
            ShadowColor = new Color(0f, 0f, 0f, 0.72f),
            ShadowSize = shadowSize,
            ShadowOffset = new Vector2(0f, 4f)
        };
    }

    private static StyleBoxTexture CreateNinePatchPlaqueStyle(Texture2D texture, Color modulate)
    {
        return new StyleBoxTexture
        {
            Texture = texture,
            ModulateColor = modulate,
            TextureMarginLeft = 28f,
            TextureMarginRight = 28f,
            TextureMarginTop = 20f,
            TextureMarginBottom = 20f,
            ContentMarginLeft = 18f,
            ContentMarginRight = 18f,
            ContentMarginTop = 8f,
            ContentMarginBottom = 8f
        };
    }

    private static void AttachStsButtonTweensAndAudio(Button button, Label label, bool isBackOrCancel)
    {
        Tween? activeTween = null;

        button.Resized += () =>
        {
            button.PivotOffset = button.Size * 0.5f;
        };

        button.MouseEntered += () =>
        {
            if (button.Disabled)
            {
                return;
            }

            button.PivotOffset = button.Size * 0.5f;
            SfxCmd.Play(HoverSfx);
            label.AddThemeColorOverride(ThemeConstants.Label.fontColor, StsColors.gold);

            activeTween?.Kill();
            activeTween = button.CreateTween().SetParallel();
            activeTween.TweenProperty(button, "scale", Vector2.One * 1.045f, 0.06)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Expo);
        };

        button.MouseExited += () =>
        {
            button.PivotOffset = button.Size * 0.5f;
            label.AddThemeColorOverride(
                ThemeConstants.Label.fontColor,
                button.Disabled ? StsColors.halfTransparentCream : StsColors.cream);

            activeTween?.Kill();
            activeTween = button.CreateTween().SetParallel();
            activeTween.TweenProperty(button, "scale", Vector2.One, 0.35)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Expo);
        };

        button.ButtonDown += () =>
        {
            if (button.Disabled)
            {
                return;
            }

            button.PivotOffset = button.Size * 0.5f;
            activeTween?.Kill();
            activeTween = button.CreateTween().SetParallel();
            activeTween.TweenProperty(button, "scale", Vector2.One * 0.96f, 0.12)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
        };

        button.ButtonUp += () =>
        {
            if (button.Disabled)
            {
                return;
            }

            button.PivotOffset = button.Size * 0.5f;
            activeTween?.Kill();
            activeTween = button.CreateTween().SetParallel();
            activeTween.TweenProperty(button, "scale", Vector2.One * 1.02f, 0.12)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Expo);
        };

        button.Pressed += () =>
        {
            SfxCmd.Play(isBackOrCancel ? BackSfx : ClickSfx);
        };
    }
}
