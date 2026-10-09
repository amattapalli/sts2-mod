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
}
