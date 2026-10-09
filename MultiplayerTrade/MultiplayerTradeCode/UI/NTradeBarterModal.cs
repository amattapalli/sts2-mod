using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Runs;
using MultiplayerTrade.MultiplayerTradeCode.Net;
using MultiplayerTrade.MultiplayerTradeCode.Rules;

namespace MultiplayerTrade.MultiplayerTradeCode.UI;

/// <summary>
/// Full-screen overlay modal implementing the two-way multiplayer card barter window.
/// Supports selecting a teammate (in 3–4 player co-op), accepting/declining incoming trade invites,
/// offering 0 to 5 cards per side (including 5-for-0 gifting), inspecting offered cards with hover tips,
/// locking offers, and confirming the trade.
/// </summary>
public partial class NTradeBarterModal : Control, IOverlayScreen
{
    private static NTradeBarterModal? _activeInstance;

    private VBoxContainer _rootLayout = null!;
    private Label _titleLabel = null!;
    private Label _statusLabel = null!;
    private Control _bodyContainer = null!;
    private HBoxContainer _footerBar = null!;
    private bool _isSelectingDeckCards;

    /// <inheritdoc />
    public NetScreenType ScreenType => NetScreenType.SharedRelicPicker;

    /// <inheritdoc />
    public bool UseSharedBackstop => true;

    /// <inheritdoc />
    public Control? DefaultFocusedControl => null;

    /// <inheritdoc />
    public Control? FocusedControlFromTopBar => null;

    /// <summary>
    /// Opens the trade barter modal. If the run has exactly 2 players (or a specific <paramref name="partnerNetId"/>
    /// is provided), starts a trade session immediately with that teammate; otherwise shows the teammate selector.
    /// </summary>
    /// <param name="runState">The current run state.</param>
    /// <param name="partnerNetId">Optional target teammate network ID.</param>
    public static void OpenTradeModal(IRunState? runState, ulong? partnerNetId = null)
    {
        if (!TradeEligibility.CanTradeInCurrentRoom(runState) || NOverlayStack.Instance == null)
        {
            return;
        }

        TradeSessionSynchronizer? sync = TradeSessionSynchronizer.Instance;
        if (sync == null)
        {
            return;
        }

        // If a Capstone screen (like NMultiplayerPlayerExpandedState) is open, close it first so Overlays are visible.
        if (NCapstoneContainer.Instance != null && NCapstoneContainer.Instance.InUse)
        {
            NCapstoneContainer.Instance.Close();
        }

        ulong? localNetId = LocalContext.NetId;
        if (!localNetId.HasValue)
        {
            return;
        }

        // If there is no active session yet, auto-pick partner if specified or if in a 2-player game.
        if (sync.ActiveSession == null)
        {
            ulong? targetId = partnerNetId;
            if (!targetId.HasValue)
            {
                List<Player> teammates = runState!.Players
                    .Where(p => p.NetId != localNetId.Value)
                    .ToList();
                if (teammates.Count == 1)
                {
                    targetId = teammates[0].NetId;
                }
            }

            if (targetId.HasValue)
            {
                sync.StartTradeSession(targetId.Value);
            }
        }

        ShowOrRefreshModal();
    }

    /// <summary>
    /// Automatically opens or refreshes the trade modal when an incoming trade invite arrives from a teammate.
    /// </summary>
    /// <param name="session">The incoming trade session state.</param>
    public static void OpenForIncomingInvite(TradeSessionState session)
    {
        if (NOverlayStack.Instance == null)
        {
            return;
        }

        if (NCapstoneContainer.Instance != null && NCapstoneContainer.Instance.InUse)
        {
            NCapstoneContainer.Instance.Close();
        }

        ShowOrRefreshModal();
    }

    private static void ShowOrRefreshModal()
    {
        if (NOverlayStack.Instance == null)
        {
            return;
        }

        if (_activeInstance != null && IsInstanceValid(_activeInstance) && _activeInstance.IsInsideTree())
        {
            _activeInstance.RefreshUi();
            return;
        }

        var modal = new NTradeBarterModal
        {
            Name = "NTradeBarterModal"
        };
        _activeInstance = modal;
        NOverlayStack.Instance.Push(modal);
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;

        var centerContainer = new CenterContainer
        {
            Name = "CenterContainer",
            MouseFilter = MouseFilterEnum.Pass
        };
        centerContainer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        this.AddChildSafely(centerContainer);

        var mainPanel = new PanelContainer
        {
            Name = "MainTradePanel",
            CustomMinimumSize = new Vector2(1540f, 840f)
        };
        mainPanel.AddThemeStyleboxOverride("panel", CreatePanelStyle(
            new Color(0.08f, 0.10f, 0.14f, 0.97f),
            new Color(0.83f, 0.68f, 0.28f, 0.95f),
            borderWidth: 3,
            cornerRadius: 14,
            contentMargin: 24));
        centerContainer.AddChildSafely(mainPanel);

        _rootLayout = new VBoxContainer
        {
            Name = "RootLayout",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        _rootLayout.AddThemeConstantOverride("separation", 16);
        mainPanel.AddChildSafely(_rootLayout);

        // Header
        var headerBox = new VBoxContainer();
        headerBox.AddThemeConstantOverride("separation", 6);
        _rootLayout.AddChildSafely(headerBox);

        _titleLabel = new Label
        {
            Text = "MULTIPLAYER CARD BARTER",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 28);
        _titleLabel.AddThemeColorOverride("font_color", new Color(0.96f, 0.84f, 0.42f));
        headerBox.AddChildSafely(_titleLabel);

        _statusLabel = new Label
        {
            Text = "Select up to 5 cards to trade (5-for-0 gifting supported). Traded cards trigger relics!",
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _statusLabel.AddThemeFontSizeOverride("font_size", 18);
        _statusLabel.AddThemeColorOverride("font_color", new Color(0.82f, 0.88f, 0.94f));
        headerBox.AddChildSafely(_statusLabel);

        // Body
        _bodyContainer = new MarginContainer
        {
            Name = "BodyContainer",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        _rootLayout.AddChildSafely(_bodyContainer);

        // Footer
        _footerBar = new HBoxContainer
        {
            Name = "FooterBar",
            Alignment = BoxContainer.AlignmentMode.Center,
            CustomMinimumSize = new Vector2(0f, 56f)
        };
        _footerBar.AddThemeConstantOverride("separation", 20);
        _rootLayout.AddChildSafely(_footerBar);

        if (TradeSessionSynchronizer.Instance != null)
        {
            TradeSessionSynchronizer.Instance.SessionStateChanged += OnSessionStateChanged;
        }

        RefreshUi();
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        if (TradeSessionSynchronizer.Instance != null)
        {
            TradeSessionSynchronizer.Instance.SessionStateChanged -= OnSessionStateChanged;
        }

        if (ReferenceEquals(_activeInstance, this))
        {
            _activeInstance = null;
        }
    }

    /// <inheritdoc />
    public void AfterOverlayOpened()
    {
    }

    /// <inheritdoc />
    public void AfterOverlayClosed()
    {
        NHoverTipSet.Clear();
        if (ReferenceEquals(_activeInstance, this))
        {
            _activeInstance = null;
        }
        this.QueueFreeSafely();
    }

    /// <inheritdoc />
    public void AfterOverlayShown()
    {
        Visible = true;
        RefreshUi();
    }

    /// <inheritdoc />
    public void AfterOverlayHidden()
    {
        NHoverTipSet.Clear();
        Visible = false;
    }

    private void OnSessionStateChanged(TradeSessionState? session)
    {
        if (!IsInsideTree())
        {
            return;
        }

        if (session == null && !_isSelectingDeckCards)
        {
            CloseModalFromOverlay();
            return;
        }

        RefreshUi();
    }

    private void CloseModalFromOverlay()
    {
        if (NOverlayStack.Instance != null && IsInsideTree())
        {
            NOverlayStack.Instance.Remove(this);
        }
        else
        {
            this.QueueFreeSafely();
        }
    }

    /// <summary>
    /// Rebuilds the active view (Partner Selection, Incoming Invite Prompt, or Two-Way Barter Window)
    /// from the current <see cref="TradeSessionSynchronizer.ActiveSession"/> state.
    /// </summary>
    public void RefreshUi()
    {
        if (!IsNodeReady())
        {
            return;
        }

        ClearChildren(_bodyContainer);
        ClearChildren(_footerBar);

        TradeSessionSynchronizer? sync = TradeSessionSynchronizer.Instance;
        IRunState? runState = RunManager.Instance.DebugOnlyGetState();
        ulong localNetId = LocalContext.NetId ?? 0UL;

        if (sync == null || runState == null)
        {
            CloseModalFromOverlay();
            return;
        }

        TradeSessionState? session = sync.ActiveSession;
        if (session == null)
        {
            BuildPartnerSelectionView(sync, runState, localNetId);
            return;
        }

        if (session.Phase == TradeSessionPhase.PendingInvite && session.PartnerNetId == localNetId)
        {
            BuildIncomingInviteView(sync, session);
            return;
        }

        BuildTwoWayBarterView(sync, runState, session, localNetId);
    }

    private void BuildPartnerSelectionView(TradeSessionSynchronizer sync, IRunState runState, ulong localNetId)
    {
        _titleLabel.Text = "SELECT A TEAMMATE TO TRADE WITH";
        _statusLabel.Text = "Choose a player in your party to open a two-way card barter window (up to 5-for-0 cards).";

        var listBox = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            CustomMinimumSize = new Vector2(680f, 0f)
        };
        listBox.AddThemeConstantOverride("separation", 14);
        _bodyContainer.AddChildSafely(listBox);

        foreach (Player teammate in runState.Players.Where(p => p.NetId != localNetId))
        {
            int tradableCount = TradeEligibility.GetTradableDeckCards(teammate).Count;
            string displayName = sync.GetPlayerDisplayName(teammate.NetId);
            Button partnerBtn = CreateStyledButton(
                $"Trade with {displayName}  —  {tradableCount} Tradable Deck Cards",
                new Vector2(640f, 58f),
                new Color(0.16f, 0.27f, 0.42f),
                new Color(0.45f, 0.72f, 0.98f));

            ulong targetNetId = teammate.NetId;
            partnerBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(delegate
            {
                sync.StartTradeSession(targetNetId);
                RefreshUi();
            }));
            listBox.AddChildSafely(partnerBtn);
        }

        Button closeBtn = CreateStyledButton(
            "Close",
            new Vector2(200f, 48f),
            new Color(0.32f, 0.16f, 0.16f),
            new Color(0.85f, 0.42f, 0.42f));
        closeBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(CloseModalFromOverlay));
        _footerBar.AddChildSafely(closeBtn);
    }

    private void BuildIncomingInviteView(TradeSessionSynchronizer sync, TradeSessionState session)
    {
        string initiatorName = sync.GetPlayerDisplayName(session.InitiatorNetId);
        _titleLabel.Text = "INCOMING CARD TRADE REQUEST";
        _statusLabel.Text = $"{initiatorName} wants to trade cards with you!";

        var promptPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            CustomMinimumSize = new Vector2(760f, 260f)
        };
        promptPanel.AddThemeStyleboxOverride("panel", CreatePanelStyle(
            new Color(0.12f, 0.15f, 0.21f, 0.95f),
            new Color(0.45f, 0.78f, 0.95f, 0.9f),
            borderWidth: 2,
            cornerRadius: 12,
            contentMargin: 28));
        _bodyContainer.AddChildSafely(promptPanel);

        var promptVBox = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        promptVBox.AddThemeConstantOverride("separation", 22);
        promptPanel.AddChildSafely(promptVBox);

        var descLabel = new Label
        {
            Text = $"{initiatorName} has invited you to a two-way card barter.\n" +
                   "• Offer 0 to 5 removable deck cards on either side (5-for-0 gifting supported).\n" +
                   "• Cards you receive will trigger your card-acquisition relics (e.g. +15 Gold, Egg upgrades)!",
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        descLabel.AddThemeFontSizeOverride("font_size", 18);
        descLabel.AddThemeColorOverride("font_color", new Color(0.92f, 0.94f, 0.98f));
        promptVBox.AddChildSafely(descLabel);

        var buttonRow = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        buttonRow.AddThemeConstantOverride("separation", 24);
        promptVBox.AddChildSafely(buttonRow);

        Button acceptBtn = CreateStyledButton(
            "Accept Trade",
            new Vector2(240f, 54f),
            new Color(0.14f, 0.36f, 0.22f),
            new Color(0.38f, 0.88f, 0.54f));
        acceptBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(delegate
        {
            sync.RespondToInvite(accepted: true);
        }));
        buttonRow.AddChildSafely(acceptBtn);

        Button declineBtn = CreateStyledButton(
            "Decline",
            new Vector2(200f, 54f),
            new Color(0.38f, 0.16f, 0.16f),
            new Color(0.90f, 0.42f, 0.42f));
        declineBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(delegate
        {
            sync.RespondToInvite(accepted: false, "Declined");
        }));
        buttonRow.AddChildSafely(declineBtn);
    }

    private void BuildTwoWayBarterView(
        TradeSessionSynchronizer sync,
        IRunState runState,
        TradeSessionState session,
        ulong localNetId)
    {
        bool isInitiator = localNetId == session.InitiatorNetId;
        ulong partnerNetId = isInitiator ? session.PartnerNetId : session.InitiatorNetId;

        List<TradeOfferEntry> localOffer = isInitiator ? session.InitiatorOffer : session.PartnerOffer;
        List<TradeOfferEntry> remoteOffer = isInitiator ? session.PartnerOffer : session.InitiatorOffer;

        bool localLocked = isInitiator ? session.InitiatorLocked : session.PartnerLocked;
        bool remoteLocked = isInitiator ? session.PartnerLocked : session.InitiatorLocked;

        bool localConfirmed = isInitiator ? session.InitiatorConfirmed : session.PartnerConfirmed;
        bool remoteConfirmed = isInitiator ? session.PartnerConfirmed : session.InitiatorConfirmed;

        string localName = sync.GetPlayerDisplayName(localNetId);
        string partnerName = sync.GetPlayerDisplayName(partnerNetId);

        _titleLabel.Text = $"CARD TRADE  —  {localName}  ⇄  {partnerName}";
        _statusLabel.Text = string.IsNullOrWhiteSpace(session.StatusMessage)
            ? "Select 0 to 5 cards on each side, Lock Offer, and Confirm Trade."
            : session.StatusMessage;

        var columnsHBox = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        columnsHBox.AddThemeConstantOverride("separation", 20);
        _bodyContainer.AddChildSafely(columnsHBox);

        // Left Column: Local Player Offer
        Control localColumn = BuildOfferColumn(
            title: $"YOUR OFFER  ({localOffer.Count} / {TradeEligibility.MaxCardsPerPlayer} Cards)",
            subtitle: localName,
            entries: localOffer,
            isLocked: localLocked,
            isConfirmed: localConfirmed,
            isLocalEditable: session.Phase == TradeSessionPhase.Negotiating,
            runState: runState,
            sync: sync);
        columnsHBox.AddChildSafely(localColumn);

        // Right Column: Partner Player Offer
        Control partnerColumn = BuildOfferColumn(
            title: $"PARTNER OFFER  ({remoteOffer.Count} / {TradeEligibility.MaxCardsPerPlayer} Cards)",
            subtitle: partnerName,
            entries: remoteOffer,
            isLocked: remoteLocked,
            isConfirmed: remoteConfirmed,
            isLocalEditable: false,
            runState: runState,
            sync: sync);
        columnsHBox.AddChildSafely(partnerColumn);

        // Footer Controls
        bool isNegotiating = session.Phase == TradeSessionPhase.Negotiating;
        bool validTotalCards = TradeEligibility.IsValidOfferCount(localOffer.Count, remoteOffer.Count);

        Button lockBtn = CreateStyledButton(
            localLocked ? "Unlock Offer" : "Lock Offer",
            new Vector2(230f, 50f),
            localLocked ? new Color(0.36f, 0.28f, 0.12f) : new Color(0.16f, 0.30f, 0.44f),
            localLocked ? new Color(0.95f, 0.76f, 0.32f) : new Color(0.45f, 0.78f, 0.98f));
        lockBtn.Disabled = !isNegotiating;
        lockBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(delegate
        {
            sync.ToggleLocalLock();
        }));
        _footerBar.AddChildSafely(lockBtn);

        bool canConfirm = isNegotiating && localLocked && remoteLocked && validTotalCards && !localConfirmed;
        string confirmText = localConfirmed
            ? "Waiting for Partner Confirm..."
            : (validTotalCards ? "Confirm Trade" : "Offer ≥1 Card to Confirm");
        Button confirmBtn = CreateStyledButton(
            confirmText,
            new Vector2(280f, 50f),
            canConfirm ? new Color(0.14f, 0.38f, 0.22f) : new Color(0.18f, 0.20f, 0.24f),
            canConfirm ? new Color(0.40f, 0.92f, 0.56f) : new Color(0.42f, 0.45f, 0.50f));
        confirmBtn.Disabled = !canConfirm;
        confirmBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(delegate
        {
            sync.ConfirmLockedTrade();
        }));
        _footerBar.AddChildSafely(confirmBtn);

        Button cancelBtn = CreateStyledButton(
            "Cancel Trade",
            new Vector2(200f, 50f),
            new Color(0.36f, 0.15f, 0.15f),
            new Color(0.88f, 0.40f, 0.40f));
        cancelBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(delegate
        {
            sync.CancelActiveSession("Trade cancelled.");
            CloseModalFromOverlay();
        }));
        _footerBar.AddChildSafely(cancelBtn);
    }

    private Control BuildOfferColumn(
        string title,
        string subtitle,
        List<TradeOfferEntry> entries,
        bool isLocked,
        bool isConfirmed,
        bool isLocalEditable,
        IRunState runState,
        TradeSessionSynchronizer sync)
    {
        Color borderColor = isConfirmed
            ? new Color(0.32f, 0.95f, 0.88f, 0.95f)
            : (isLocked
                ? new Color(0.38f, 0.88f, 0.52f, 0.95f)
                : new Color(0.42f, 0.48f, 0.58f, 0.85f));

        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        panel.AddThemeStyleboxOverride("panel", CreatePanelStyle(
            new Color(0.11f, 0.14f, 0.19f, 0.95f),
            borderColor,
            borderWidth: 2,
            cornerRadius: 10,
            contentMargin: 16));

        var colVBox = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        colVBox.AddThemeConstantOverride("separation", 12);
        panel.AddChildSafely(colVBox);

        // Top row: Column Title + Lock Badge
        var topRow = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        colVBox.AddChildSafely(topRow);

        var titleVBox = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        topRow.AddChildSafely(titleVBox);

        var colTitle = new Label
        {
            Text = title
        };
        colTitle.AddThemeFontSizeOverride("font_size", 20);
        colTitle.AddThemeColorOverride("font_color", new Color(0.95f, 0.90f, 0.72f));
        titleVBox.AddChildSafely(colTitle);

        var subLabel = new Label
        {
            Text = subtitle
        };
        subLabel.AddThemeFontSizeOverride("font_size", 15);
        subLabel.AddThemeColorOverride("font_color", new Color(0.70f, 0.78f, 0.88f));
        titleVBox.AddChildSafely(subLabel);

        string badgeText = isConfirmed
            ? "CONFIRMED ✓✓"
            : (isLocked ? "LOCKED ✓" : "UNLOCKED");
        Color badgeColor = isConfirmed
            ? new Color(0.35f, 0.96f, 0.90f)
            : (isLocked ? new Color(0.42f, 0.92f, 0.56f) : new Color(0.88f, 0.74f, 0.38f));

        var badgeLabel = new Label
        {
            Text = badgeText,
            VerticalAlignment = VerticalAlignment.Center
        };
        badgeLabel.AddThemeFontSizeOverride("font_size", 18);
        badgeLabel.AddThemeColorOverride("font_color", badgeColor);
        topRow.AddChildSafely(badgeLabel);

        // Card slots area
        var cardsScroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
            VerticalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        colVBox.AddChildSafely(cardsScroll);

        var cardsRow = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        cardsRow.AddThemeConstantOverride("separation", 10);
        cardsScroll.AddChildSafely(cardsRow);

        if (entries.Count == 0)
        {
            var emptyLabel = new Label
            {
                Text = "0 Cards Offered\n(Supports 5-for-0 Gifting / Asymmetric Swaps)",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill
            };
            emptyLabel.AddThemeFontSizeOverride("font_size", 17);
            emptyLabel.AddThemeColorOverride("font_color", new Color(0.60f, 0.66f, 0.74f));
            cardsRow.AddChildSafely(emptyLabel);
        }
        else
        {
            for (int i = 0; i < entries.Count; i++)
            {
                int slotIndex = i;
                TradeOfferEntry entry = entries[i];
                Control cardSlot = BuildOfferedCardVisualSlot(entry, slotIndex, isLocalEditable, sync);
                cardsRow.AddChildSafely(cardSlot);
            }
        }

        // Action row for the local player's offer
        if (isLocalEditable)
        {
            var actionsRow = new HBoxContainer
            {
                Alignment = BoxContainer.AlignmentMode.Center
            };
            actionsRow.AddThemeConstantOverride("separation", 14);
            colVBox.AddChildSafely(actionsRow);

            Player? localPlayer = LocalContext.GetMe(runState);
            int eligibleCount = TradeEligibility.GetTradableDeckCards(localPlayer).Count;

            Button selectCardsBtn = CreateStyledButton(
                $"Select Deck Cards (1–{Math.Min(TradeEligibility.MaxCardsPerPlayer, Math.Max(1, eligibleCount))})",
                new Vector2(290f, 44f),
                new Color(0.18f, 0.32f, 0.48f),
                new Color(0.48f, 0.80f, 0.98f));
            selectCardsBtn.Disabled = eligibleCount == 0;
            selectCardsBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(delegate
            {
                TaskHelper.RunSafely(OpenLocalDeckCardPickerAsync(runState, sync));
            }));
            actionsRow.AddChildSafely(selectCardsBtn);

            Button clearOfferBtn = CreateStyledButton(
                "Clear (Offer 0 Cards)",
                new Vector2(220f, 44f),
                new Color(0.28f, 0.22f, 0.16f),
                new Color(0.82f, 0.64f, 0.38f));
            clearOfferBtn.Disabled = entries.Count == 0;
            clearOfferBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(delegate
            {
                sync.SetLocalOfferedCards(Array.Empty<CardModel>());
            }));
            actionsRow.AddChildSafely(clearOfferBtn);
        }

        return panel;
    }

    private static Control BuildOfferedCardVisualSlot(
        TradeOfferEntry entry,
        int slotIndex,
        bool isLocalEditable,
        TradeSessionSynchronizer sync)
    {
        var slotVBox = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(132f, 270f),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        slotVBox.AddThemeConstantOverride("separation", 6);

        CardModel previewCard = TradeEligibility.CreatePreviewCard(entry.Card);

        var cardHolderWrapper = new Control
        {
            CustomMinimumSize = new Vector2(132f, 196f),
            MouseFilter = MouseFilterEnum.Pass
        };
        slotVBox.AddChildSafely(cardHolderWrapper);

        NCard? nCard = NCard.Create(previewCard);
        if (nCard != null)
        {
            NPreviewCardHolder? previewHolder = NPreviewCardHolder.Create(
                nCard,
                showHoverTips: true,
                scaleOnHover: false);
            if (previewHolder != null)
            {
                previewHolder.SetCardScale(new Vector2(0.42f, 0.42f));
                previewHolder.Position = new Vector2(66f, 98f);
                cardHolderWrapper.AddChildSafely(previewHolder);
                nCard.UpdateVisuals(PileType.Deck, CardPreviewMode.Normal);
            }
            else
            {
                nCard.QueueFreeSafely();
            }
        }

        var cardNameLabel = new Label
        {
            Text = TradeEligibility.FormatCardLabel(previewCard),
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(128f, 36f)
        };
        cardNameLabel.AddThemeFontSizeOverride("font_size", 13);
        cardNameLabel.AddThemeColorOverride("font_color", previewCard.IsUpgraded
            ? new Color(0.48f, 0.96f, 0.56f)
            : new Color(0.92f, 0.94f, 0.98f));
        slotVBox.AddChildSafely(cardNameLabel);

        if (isLocalEditable)
        {
            Button removeBtn = CreateStyledButton(
                "Remove",
                new Vector2(110f, 30f),
                new Color(0.34f, 0.16f, 0.16f),
                new Color(0.84f, 0.42f, 0.42f),
                fontSize: 13);
            removeBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(delegate
            {
                sync.RemoveCardFromLocalOfferAt(slotIndex);
            }));
            slotVBox.AddChildSafely(removeBtn);
        }

        return slotVBox;
    }

    private async Task OpenLocalDeckCardPickerAsync(IRunState runState, TradeSessionSynchronizer sync)
    {
        if (_isSelectingDeckCards || NOverlayStack.Instance == null)
        {
            return;
        }

        Player? localPlayer = LocalContext.GetMe(runState);
        List<CardModel> eligibleCards = TradeEligibility.GetTradableDeckCards(localPlayer);
        if (eligibleCards.Count == 0)
        {
            return;
        }

        int maxSelect = Math.Min(TradeEligibility.MaxCardsPerPlayer, eligibleCards.Count);
        LocString prompt = LocString.Exists("card_selection", "MULTIPLAYER_TRADE_SELECT_PROMPT")
            ? new LocString("card_selection", "MULTIPLAYER_TRADE_SELECT_PROMPT")
            : CardSelectorPrefs.TransformSelectionPrompt;

        // Use minCount: 1 so NDeckCardSelectScreen.CancelSelection never hits an empty _selectedCards.Last()
        // while allowing the player to pick 1..5 cards (or press Close to cancel, or use Clear for 0 cards).
        var prefs = new CardSelectorPrefs(prompt, minCount: 1, maxCount: maxSelect)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };

        _isSelectingDeckCards = true;
        try
        {
            NDeckCardSelectScreen screen = NDeckCardSelectScreen.Create(eligibleCards, prefs);
            NOverlayStack.Instance.Push(screen);
            IEnumerable<CardModel> selected = await screen.CardsSelected();
            List<CardModel> selectedList = selected?.ToList() ?? new List<CardModel>();

            if (selectedList.Count > 0 && sync.ActiveSession != null)
            {
                sync.SetLocalOfferedCards(selectedList);
            }
        }
        catch (TaskCanceledException)
        {
            // Selection screen closed or cancelled.
        }
        finally
        {
            _isSelectingDeckCards = false;
            if (IsInsideTree())
            {
                RefreshUi();
            }
        }
    }

    private static Button CreateStyledButton(
        string text,
        Vector2 minSize,
        Color bgColor,
        Color borderColor,
        int fontSize = 16)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = minSize,
            MouseDefaultCursorShape = CursorShape.PointingHand
        };
        button.AddThemeFontSizeOverride("font_size", fontSize);
        button.AddThemeColorOverride("font_color", new Color(0.96f, 0.96f, 0.98f));
        button.AddThemeColorOverride("font_hover_color", new Color(1f, 0.95f, 0.75f));
        button.AddThemeColorOverride("font_disabled_color", new Color(0.50f, 0.52f, 0.56f));

        button.AddThemeStyleboxOverride("normal", CreatePanelStyle(bgColor, borderColor, 2, 8, 10));
        button.AddThemeStyleboxOverride("hover", CreatePanelStyle(bgColor.Lightened(0.14f), borderColor.Lightened(0.2f), 2, 8, 10));
        button.AddThemeStyleboxOverride("pressed", CreatePanelStyle(bgColor.Darkened(0.15f), borderColor, 2, 8, 10));
        button.AddThemeStyleboxOverride("disabled", CreatePanelStyle(
            new Color(0.14f, 0.15f, 0.18f, 0.7f),
            new Color(0.30f, 0.32f, 0.36f, 0.6f),
            1,
            8,
            10));
        return button;
    }

    private static StyleBoxFlat CreatePanelStyle(
        Color bgColor,
        Color borderColor,
        int borderWidth,
        int cornerRadius,
        int contentMargin)
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
            ContentMarginBottom = contentMargin
        };
    }

    private static void ClearChildren(Node parent)
    {
        for (int i = parent.GetChildCount() - 1; i >= 0; i--)
        {
            Node child = parent.GetChild(i);
            parent.RemoveChild(child);
            child.QueueFreeSafely();
        }
    }
}
