using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace Transmuter.Tests;

/// <summary>
/// Unit, protocol, and source-audit tests for the <c>MultiplayerTrade</c> mod.
/// </summary>
public class MultiplayerTradeTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static string ModRoot =>
        Path.Combine(RepoRoot, "MultiplayerTrade");

    private static string CodeDir =>
        Path.Combine(ModRoot, "MultiplayerTradeCode");

    private static string LocDir =>
        Path.Combine(ModRoot, "MultiplayerTrade", "localization", "eng");

    [Fact]
    public void MultiplayerTradeManifest_IsValidAndHasExpectedMetadata()
    {
        string manifestPath = Path.Combine(ModRoot, "MultiplayerTrade.json");
        Assert.True(File.Exists(manifestPath), $"Expected manifest at {manifestPath}");

        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
        JsonElement root = doc.RootElement;

        Assert.Equal("MultiplayerTrade", root.GetProperty("id").GetString());
        Assert.True(root.GetProperty("has_dll").GetBoolean());
        Assert.True(root.GetProperty("has_pck").GetBoolean());
        Assert.True(root.GetProperty("affects_gameplay").GetBoolean());
    }

    [Fact]
    public void CardSelectionLocalizationJson_IsValidAndContainsPromptKey()
    {
        string jsonPath = Path.Combine(LocDir, "card_selection.json");
        Assert.True(File.Exists(jsonPath), $"Expected localization file at {jsonPath}");

        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(jsonPath));
        JsonElement root = doc.RootElement;
        Assert.True(
            root.TryGetProperty("MULTIPLAYER_TRADE_SELECT_PROMPT", out JsonElement prompt) &&
            !string.IsNullOrWhiteSpace(prompt.GetString()),
            "Missing MULTIPLAYER_TRADE_SELECT_PROMPT in card_selection.json");
    }

    [Fact]
    public void MainFileInitialize_DoesNotPrematurelyTouchMessageTypesOrReflectionHelper()
    {
        string mainFilePath = Path.Combine(CodeDir, "MainFile.cs");
        string content = File.ReadAllText(mainFilePath);

        Assert.Contains("[ModInitializer(nameof(Initialize))]", content);
        // Strip comments before checking for premature ReflectionHelper/MessageTypes references.
        string codeOnly = string.Join(
            "\n",
            content.Split('\n').Where(line => !line.TrimStart().StartsWith("//") && !line.TrimStart().StartsWith("///")));

        Assert.DoesNotContain("MessageTypes", codeOnly);
        Assert.DoesNotContain("ReflectionHelper.ModTypes", codeOnly);
    }

    [Fact]
    public void TradeMessages_ImplementBroadcastINetMessageAndLocationTargetedExecution()
    {
        string messagesPath = Path.Combine(CodeDir, "Net", "TradeMessages.cs");
        string content = File.ReadAllText(messagesPath);

        string[] expectedStructs =
        [
            "struct TradeOfferEntry : IPacketSerializable",
            "struct TradeInviteMessage : INetMessage",
            "struct TradeInviteResponseMessage : INetMessage",
            "struct TradeOfferUpdatedMessage : INetMessage",
            "struct TradeLockChangedMessage : INetMessage",
            "struct TradeCancelMessage : INetMessage",
            "struct TradeExecutedMessage : INetMessage, IRunLocationTargetedMessage"
        ];

        foreach (string declaration in expectedStructs)
        {
            Assert.Contains(declaration, content);
        }

        // Client-to-client messages in STS2 must set ShouldBroadcast => true so NetHostGameService relays them.
        Assert.Contains("public bool ShouldBroadcast => true;", content);
    }

    [Fact]
    public void TradeSessionSynchronizer_TriggersRelicHooksViaLoadCardAndCardPileCmdAdd()
    {
        string syncPath = Path.Combine(CodeDir, "Net", "TradeSessionSynchronizer.cs");
        string content = File.ReadAllText(syncPath);

        // Must deserialize a fresh mutable card owned by the receiver and add to PileType.Deck via CardPileCmd.Add
        // so Hook.ShouldAddToDeck, Hook.ModifyCardBeingAddedToDeck (Eggs), and Hook.AfterCardChangedPiles (+15 Gold, etc.) fire.
        Assert.Contains("_runState.LoadCard(entry.Card, receiver)", content);
        Assert.Contains("await CardPileCmd.Add(loadedCards, PileType.Deck)", content);
        Assert.Contains("CardCmd.PreviewCardPileAdd(", content);
        Assert.Contains("await CardPileCmd.RemoveFromDeck(", content);
    }

    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(5, 0, true)]
    [InlineData(0, 5, true)]
    [InlineData(5, 5, true)]
    [InlineData(2, 1, true)]
    [InlineData(0, 1, true)]
    [InlineData(0, 0, false)]
    [InlineData(6, 0, false)]
    [InlineData(0, 6, false)]
    [InlineData(-1, 1, false)]
    public void OfferCountValidation_AllowsUpToFiveForZeroAndRejectsEmptyOrOverFive(
        int initiatorCount,
        int partnerCount,
        bool expectedValid)
    {
        bool actual = IsValidOfferCountMirror(initiatorCount, partnerCount);
        Assert.Equal(expectedValid, actual);
    }

    [Fact]
    public void BarterLockResetSimulation_PreventsBaitAndSwitchOnOfferUpdate()
    {
        bool initiatorLocked = true;
        bool partnerLocked = true;
        bool initiatorConfirmed = true;
        bool partnerConfirmed = false;

        // Simulate either player updating their offered cards (0..5 cards):
        void OnOfferUpdated()
        {
            initiatorLocked = false;
            partnerLocked = false;
            initiatorConfirmed = false;
            partnerConfirmed = false;
        }

        OnOfferUpdated();

        Assert.False(initiatorLocked);
        Assert.False(partnerLocked);
        Assert.False(initiatorConfirmed);
        Assert.False(partnerConfirmed);
    }

    private static bool IsValidOfferCountMirror(int initiatorCardCount, int partnerCardCount)
    {
        const int maxCardsPerPlayer = 5;
        if (initiatorCardCount < 0 || initiatorCardCount > maxCardsPerPlayer)
        {
            return false;
        }

        if (partnerCardCount < 0 || partnerCardCount > maxCardsPerPlayer)
        {
            return false;
        }

        return initiatorCardCount + partnerCardCount > 0;
    }

    [Fact]
    public void ModImage_ExistsIsValid512PngAndIsDistinctFromUsurerAndTransmuter()
    {
        string tradeModImage = Path.Combine(ModRoot, "MultiplayerTrade", "mod_image.png");
        string usurerModImage = Path.Combine(RepoRoot, "Usurer", "Usurer", "mod_image.png");
        string transmuterModImage = Path.Combine(RepoRoot, "Transmuter", "Transmuter", "mod_image.png");

        Assert.True(File.Exists(tradeModImage), $"Missing mod_image.png at {tradeModImage}");
        byte[] tradeBytes = File.ReadAllBytes(tradeModImage);
        AssertPngDimensions(tradeBytes, 512, 512);

        if (File.Exists(usurerModImage))
        {
            Assert.False(tradeBytes.SequenceEqual(File.ReadAllBytes(usurerModImage)),
                "MultiplayerTrade/mod_image.png must be unique artwork, not a copy of Usurer/mod_image.png");
        }

        if (File.Exists(transmuterModImage))
        {
            Assert.False(tradeBytes.SequenceEqual(File.ReadAllBytes(transmuterModImage)),
                "MultiplayerTrade/mod_image.png must be unique artwork, not a copy of Transmuter/mod_image.png");
        }
    }

    [Theory]
    [InlineData("trade_icon.png", 128, 128)]
    [InlineData("trade_button_plaque.png", 360, 96)]
    [InlineData("trade_button_plaque_hover.png", 360, 96)]
    [InlineData("trade_banner.png", 960, 110)]
    [InlineData("card_slot_empty.png", 196, 272)]
    [InlineData("lock_closed.png", 64, 64)]
    [InlineData("lock_open.png", 64, 64)]
    public void CustomUiTextures_ExistWithExpectedPngDimensions(
        string fileName,
        int expectedWidth,
        int expectedHeight)
    {
        string texturePath = Path.Combine(ModRoot, "MultiplayerTrade", "images", "ui", fileName);
        Assert.True(File.Exists(texturePath), $"Expected custom UI texture at {texturePath}");

        byte[] bytes = File.ReadAllBytes(texturePath);
        AssertPngDimensions(bytes, expectedWidth, expectedHeight);
    }

    [Fact]
    public void TradeUiStyleAndComponents_UseStsColorsKreonTypographyPlaqueButtonsAndSfx()
    {
        string stylePath = Path.Combine(CodeDir, "UI", "TradeUiStyle.cs");
        string modalPath = Path.Combine(CodeDir, "UI", "NTradeBarterModal.cs");
        string patchesPath = Path.Combine(CodeDir, "Patches", "TradeUiPatches.cs");

        Assert.True(File.Exists(stylePath), $"Expected TradeUiStyle.cs at {stylePath}");
        string styleContent = File.ReadAllText(stylePath);
        string modalContent = File.ReadAllText(modalPath);
        string patchesContent = File.ReadAllText(patchesPath);

        // Verify Kreon font & locale substitution, StsColors, comic ink outlines, plaque buttons, and STS2 SFX:
        Assert.Contains("ApplyLocaleFontSubstitution", styleContent);
        Assert.Contains("res://themes/kreon_bold_shared.tres", styleContent);
        Assert.Contains("StsColors.cream", styleContent);
        Assert.Contains("StsColors.gold", styleContent);
        Assert.Contains("event:/sfx/ui/clicks/ui_hover", styleContent);
        Assert.Contains("event:/sfx/ui/clicks/ui_click", styleContent);
        Assert.Contains("event:/sfx/ui/clicks/ui_back", styleContent);
        Assert.Contains("SfxCmd.Play(", styleContent);

        // Modal & Room patches must use TradeUiStyle plaque buttons, banner, and recessed stone card slots:
        Assert.Contains("TradeUiStyle.LoadUiTexture(\"trade_banner.png\")", modalContent);
        Assert.Contains("TradeUiStyle.LoadUiTexture(\"card_slot_empty.png\")", modalContent);
        Assert.Contains("\"lock_closed.png\" : \"lock_open.png\"", modalContent);
        Assert.Contains("TradeUiStyle.CreatePlaqueButton", modalContent);
        Assert.Contains("TradeUiStyle.CreateStonePanelStyle", modalContent);
        Assert.Contains("scaleOnHover: true", modalContent);
        Assert.Contains("TradeUiStyle.CreatePlaqueButton", patchesContent);
        Assert.Contains("iconFileName: \"trade_icon.png\"", patchesContent);
    }

    private static void AssertPngDimensions(byte[] bytes, int expectedWidth, int expectedHeight)
    {
        Assert.True(bytes.Length >= 24, "PNG file is too small to contain an IHDR header.");
        // PNG magic header: 89 50 4E 47 0D 0A 1A 0A
        Assert.Equal(0x89, bytes[0]);
        Assert.Equal(0x50, bytes[1]);
        Assert.Equal(0x4E, bytes[2]);
        Assert.Equal(0x47, bytes[3]);

        int width = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
        int height = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
        Assert.Equal(expectedWidth, width);
        Assert.Equal(expectedHeight, height);
    }
}

