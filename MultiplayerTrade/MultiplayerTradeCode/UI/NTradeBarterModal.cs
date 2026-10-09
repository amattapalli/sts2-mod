using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
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
/// Full-screen overlay modal implementing the two-way multiplayer card barter window,
/// styled with <i>Slay the Spire 2</i>'s Kreon outlined typography, <see cref="StsColors"/> palette,
/// hand-inked stone/brass plaques, crimson swallowtail banner, and 5-slot recessed stone card racks.
/// </summary>
public partial class NTradeBarterModal : Control, IOverlayScreen
{
    private static NTradeBarterModal? _activeInstance;

    private PanelContainer _mainPanel = null!;
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

        SfxCmd.Play(TradeUiStyle.HoverSfx);
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

        _mainPanel = new PanelContainer
        {
            Name = "MainTradePanel",
            CustomMinimumSize = new Vector2(1620f, 870f)
        };
        _mainPanel.AddThemeStyleboxOverride("panel", TradeUiStyle.CreateStonePanelStyle(
            TradeUiStyle.StonePanelBg,
            TradeUiStyle.BrassBorder,
            borderWidth: 4,
            cornerRadius: 14,
            contentMargin: 24,
            shadowSize: 28));
        centerContainer.AddChildSafely(_mainPanel);

        _rootLayout = new VBoxContainer
        {
            Name = "RootLayout",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        _rootLayout.AddThemeConstantOverride("separation", 14);
        _mainPanel.AddChildSafely(_rootLayout);

        // Ornate STS2 Crimson & Gold Swallowtail Banner Header
        var headerBox = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        headerBox.AddThemeConstantOverride("separation", 6);
        _rootLayout.AddChildSafely(headerBox);

        var bannerWrapper = new Control
        {
            CustomMinimumSize = new Vector2(960f, 84f),
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter
        };
        headerBox.AddChildSafely(bannerWrapper);

        Texture2D? bannerTex = TradeUiStyle.LoadUiTexture("trade_banner.png");
        if (bannerTex != null)
        {
            var bannerRect = new TextureRect
            {
                Name = "BannerTexture",
                Texture = bannerTex,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = MouseFilterEnum.Ignore
            };
            bannerRect.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            bannerWrapper.AddChildSafely(bannerRect);
        }

        var bannerContentRow = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        bannerContentRow.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        bannerContentRow.AddThemeConstantOverride("separation", 14);
        bannerWrapper.AddChildSafely(bannerContentRow);

        Texture2D? tradeIconTex = TradeUiStyle.LoadUiTexture("trade_icon.png");
        if (tradeIconTex != null)
        {
            var leftIcon = new TextureRect
            {
                Texture = tradeIconTex,
                CustomMinimumSize = new Vector2(48f, 48f),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                MouseFilter = MouseFilterEnum.Ignore
            };
            bannerContentRow.AddChildSafely(leftIcon);
        }

        _titleLabel = TradeUiStyle.CreateStsLabel(
            text: "CARD BARTER",
            fontSize: 30,
            color: StsColors.gold,
            bold: true,
            outlineSize: 10,
            alignment: HorizontalAlignment.Center);
        bannerContentRow.AddChildSafely(_titleLabel);

        _statusLabel = TradeUiStyle.CreateStsLabel(
            text: "Offer 0 to 5 deck cards on either side (5-for-0 gifting supported). Acquired cards trigger relics!",
            fontSize: 19,
            color: StsColors.cream,
            bold: false,
            outlineSize: 7,
            alignment: HorizontalAlignment.Center);
        _statusLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
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
            CustomMinimumSize = new Vector2(0f, 64f)
        };
        _footerBar.AddThemeConstantOverride("separation", 24);
        _rootLayout.AddChildSafely(_footerBar);

        if (TradeSessionSynchronizer.Instance != null)
        {
            TradeSessionSynchronizer.Instance.SessionStateChanged += OnSessionStateChanged;
        }

        RefreshUi();
        PlayEntranceAnimation();
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

    private void PlayEntranceAnimation()
    {
        _mainPanel.PivotOffset = _mainPanel.CustomMinimumSize * 0.5f;
        _mainPanel.Scale = new Vector2(0.94f, 0.94f);
        _mainPanel.Modulate = new Color(1f, 1f, 1f, 0f);

        Tween tween = CreateTween().SetParallel();
        tween.TweenProperty(_mainPanel, "scale", Vector2.One, 0.20)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);
        tween.TweenProperty(_mainPanel, "modulate:a", 1f, 0.16)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
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
        _titleLabel.Text = "CHOOSE A TEAMMATE TO BARTER WITH";
        _statusLabel.Text = "Select an adventurer in your party to open a two-way card barter (up to 5-for-0 cards).";

        var listPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            CustomMinimumSize = new Vector2(740f, 340f)
        };
        listPanel.AddThemeStyleboxOverride("panel", TradeUiStyle.CreateStonePanelStyle(
            TradeUiStyle.RecessedWellBg,
            TradeUiStyle.WeatheredBronzeBorder,
            borderWidth: 3,
            cornerRadius: 12,
            contentMargin: 28));
        _bodyContainer.AddChildSafely(listPanel);

        var listBox = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        listBox.AddThemeConstantOverride("separation", 18);
        listPanel.AddChildSafely(listBox);

        foreach (Player teammate in runState.Players.Where(p => p.NetId != localNetId))
        {
            int tradableCount = TradeEligibility.GetTradableDeckCards(teammate).Count;
            string displayName = sync.GetPlayerDisplayName(teammate.NetId);
            Button partnerBtn = TradeUiStyle.CreatePlaqueButton(
                name: $"TradePartner_{teammate.NetId}",
                text: $"Barter with {displayName}   ({tradableCount} Tradable Deck Cards)",
                minSize: new Vector2(640f, 68f),
                fontSize: 21,
                iconFileName: "trade_icon.png",
                iconSize: new Vector2(40f, 40f));

            ulong targetNetId = teammate.NetId;
            partnerBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(delegate
            {
                sync.StartTradeSession(targetNetId);
                RefreshUi();
            }));
            listBox.AddChildSafely(partnerBtn);
        }

        Button closeBtn = TradeUiStyle.CreatePlaqueButton(
            name: "ClosePartnerSelectButton",
            text: "Return",
            minSize: new Vector2(230f, 58f),
            fontSize: 20,
            tint: new Color(1.0f, 0.78f, 0.75f),
            isBackOrCancel: true);
        closeBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(CloseModalFromOverlay));
        _footerBar.AddChildSafely(closeBtn);
    }

    private void BuildIncomingInviteView(TradeSessionSynchronizer sync, TradeSessionState session)
    {
        string initiatorName = sync.GetPlayerDisplayName(session.InitiatorNetId);
        _titleLabel.Text = "INCOMING CARD BARTER";
        _statusLabel.Text = $"{initiatorName} wishes to barter cards with you!";

        var promptPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            CustomMinimumSize = new Vector2(820f, 320f)
        };
        promptPanel.AddThemeStyleboxOverride("panel", TradeUiStyle.CreateStonePanelStyle(
            TradeUiStyle.RecessedWellBg,
            TradeUiStyle.BrassBorder,
            borderWidth: 3,
            cornerRadius: 12,
            contentMargin: 32));
        _bodyContainer.AddChildSafely(promptPanel);

        var promptVBox = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        promptVBox.AddThemeConstantOverride("separation", 26);
        promptPanel.AddChildSafely(promptVBox);

        Label descLabel = TradeUiStyle.CreateStsLabel(
            text: $"{initiatorName} has invited you to a two-way card barter.\n" +
                  "• Offer 0 to 5 removable deck cards on either side (5-for-0 gifting supported).\n" +
                  "• Cards you acquire through trade trigger your relics (Egg upgrades, +15 Gold, etc.)!",
            fontSize: 20,
            color: StsColors.cream,
            bold: false,
            outlineSize: 7,
            alignment: HorizontalAlignment.Center);
        descLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        promptVBox.AddChildSafely(descLabel);

        var buttonRow = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        buttonRow.AddThemeConstantOverride("separation", 28);
        promptVBox.AddChildSafely(buttonRow);

        Button acceptBtn = TradeUiStyle.CreatePlaqueButton(
            name: "AcceptTradeInviteButton",
            text: "Accept Barter",
            minSize: new Vector2(260f, 64f),
            fontSize: 21,
            tint: new Color(0.84f, 1.05f, 0.88f),
            iconFileName: "trade_icon.png",
            iconSize: new Vector2(38f, 38f));
        acceptBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(delegate
        {
            sync.RespondToInvite(accepted: true);
        }));
        buttonRow.AddChildSafely(acceptBtn);

        Button declineBtn = TradeUiStyle.CreatePlaqueButton(
            name: "DeclineTradeInviteButton",
            text: "Decline",
            minSize: new Vector2(220f, 64f),
            fontSize: 21,
            tint: new Color(1.06f, 0.76f, 0.74f),
            isBackOrCancel: true);
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

        _titleLabel.Text = $"{localName.ToUpperInvariant()}   ⇄   {partnerName.ToUpperInvariant()}";
        _statusLabel.Text = string.IsNullOrWhiteSpace(session.StatusMessage)
            ? "Select 0 to 5 cards on either side, Lock Offer, and Confirm Trade."
            : session.StatusMessage;

        var columnsHBox = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        columnsHBox.AddThemeConstantOverride("separation", 18);
        _bodyContainer.AddChildSafely(columnsHBox);

        // Left Column: Local Player Offer
        Control localColumn = BuildOfferColumn(
            title: $"YOUR OFFER   ({localOffer.Count} / {TradeEligibility.MaxCardsPerPlayer})",
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
            title: $"TEAMMATE OFFER   ({remoteOffer.Count} / {TradeEligibility.MaxCardsPerPlayer})",
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

        Button lockBtn = TradeUiStyle.CreatePlaqueButton(
            name: "ToggleLockOfferButton",
            text: localLocked ? "Unlock Offer" : "Lock Offer",
            minSize: new Vector2(250f, 58f),
            fontSize: 20,
            tint: localLocked ? new Color(1.06f, 0.96f, 0.72f) : Colors.White,
            iconFileName: localLocked ? "lock_closed.png" : "lock_open.png",
            iconSize: new Vector2(30f, 30f));
        lockBtn.Disabled = !isNegotiating;
        lockBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(delegate
        {
            sync.ToggleLocalLock();
        }));
        _footerBar.AddChildSafely(lockBtn);

        bool canConfirm = isNegotiating && localLocked && remoteLocked && validTotalCards && !localConfirmed;
        string confirmText = localConfirmed
            ? "Awaiting Teammate..."
            : (validTotalCards ? "Confirm Trade" : "Offer ≥1 Card");
        Button confirmBtn = TradeUiStyle.CreatePlaqueButton(
            name: "ConfirmTradeButton",
            text: confirmText,
            minSize: new Vector2(290f, 58f),
            fontSize: 20,
            tint: canConfirm ? new Color(0.84f, 1.08f, 0.88f) : new Color(0.75f, 0.74f, 0.72f),
            iconFileName: "trade_icon.png",
            iconSize: new Vector2(34f, 34f));
        confirmBtn.Disabled = !canConfirm;
        confirmBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(delegate
        {
            sync.ConfirmLockedTrade();
        }));
        _footerBar.AddChildSafely(confirmBtn);

        Button cancelBtn = TradeUiStyle.CreatePlaqueButton(
            name: "CancelTradeButton",
            text: "Cancel Trade",
            minSize: new Vector2(230f, 58f),
            fontSize: 20,
            tint: new Color(1.06f, 0.76f, 0.74f),
            isBackOrCancel: true);
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
            ? StsColors.aqua
            : (isLocked ? StsColors.gold : TradeUiStyle.WeatheredBronzeBorder);

        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        panel.AddThemeStyleboxOverride("panel", TradeUiStyle.CreateStonePanelStyle(
            TradeUiStyle.RecessedWellBg,
            borderColor,
            borderWidth: isLocked || isConfirmed ? 3 : 2,
            cornerRadius: 12,
            contentMargin: 16,
            shadowSize: 12));

        var colVBox = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        colVBox.AddThemeConstantOverride("separation", 12);
        panel.AddChildSafely(colVBox);

        // Top row: Column Title + Padlock Status Badge
        var topRow = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        colVBox.AddChildSafely(topRow);

        var titleVBox = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        titleVBox.AddThemeConstantOverride("separation", 2);
        topRow.AddChildSafely(titleVBox);

        Label colTitle = TradeUiStyle.CreateStsLabel(
            text: title,
            fontSize: 21,
            color: StsColors.gold,
            bold: true,
            outlineSize: 8);
        titleVBox.AddChildSafely(colTitle);

        Label subLabel = TradeUiStyle.CreateStsLabel(
            text: subtitle,
            fontSize: 16,
            color: StsColors.halfTransparentCream,
            bold: false,
            outlineSize: 6);
        titleVBox.AddChildSafely(subLabel);

        var statusBadgeRow = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.End,
            SizeFlagsVertical = SizeFlags.ShrinkCenter
        };
        statusBadgeRow.AddThemeConstantOverride("separation", 8);
        topRow.AddChildSafely(statusBadgeRow);

        Texture2D? lockTex = TradeUiStyle.LoadUiTexture(isLocked || isConfirmed ? "lock_closed.png" : "lock_open.png");
        if (lockTex != null)
        {
            var lockIcon = new TextureRect
            {
                Texture = lockTex,
                CustomMinimumSize = new Vector2(28f, 28f),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                MouseFilter = MouseFilterEnum.Ignore
            };
            statusBadgeRow.AddChildSafely(lockIcon);
        }

        string badgeText = isConfirmed
            ? "CONFIRMED"
            : (isLocked ? "LOCKED" : "UNLOCKED");
        Color badgeColor = isConfirmed
            ? StsColors.aqua
            : (isLocked ? StsColors.gold : StsColors.halfTransparentCream);

        Label badgeLabel = TradeUiStyle.CreateStsLabel(
            text: badgeText,
            fontSize: 18,
            color: badgeColor,
            bold: true,
            outlineSize: 8);
        statusBadgeRow.AddChildSafely(badgeLabel);

        // Fixed 5-Slot Recessed Stone Card Rack
        var cardsRow = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        cardsRow.AddThemeConstantOverride("separation", 8);
        colVBox.AddChildSafely(cardsRow);

        for (int i = 0; i < TradeEligibility.MaxCardsPerPlayer; i++)
        {
            int slotIndex = i;
            if (slotIndex < entries.Count)
            {
                Control filledSlot = BuildFilledCardSocket(entries[slotIndex], slotIndex, isLocalEditable && !isLocked, sync);
                cardsRow.AddChildSafely(filledSlot);
            }
            else
            {
                Control emptySlot = BuildEmptyCardSocket(slotIndex, isLocalEditable && !isLocked, runState, sync);
                cardsRow.AddChildSafely(emptySlot);
            }
        }

        // Action row for the local player's offer (or symmetric spacer for partner column)
        var actionsRow = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            CustomMinimumSize = new Vector2(0f, 52f)
        };
        actionsRow.AddThemeConstantOverride("separation", 16);
        colVBox.AddChildSafely(actionsRow);

        if (isLocalEditable)
        {
            Player? localPlayer = LocalContext.GetMe(runState);
            int eligibleCount = TradeEligibility.GetTradableDeckCards(localPlayer).Count;

            Button selectCardsBtn = TradeUiStyle.CreatePlaqueButton(
                name: "SelectDeckCardsButton",
                text: $"Choose Deck Cards (1–{Math.Min(TradeEligibility.MaxCardsPerPlayer, Math.Max(1, eligibleCount))})",
                minSize: new Vector2(310f, 50f),
                fontSize: 18);
            selectCardsBtn.Disabled = eligibleCount == 0 || isLocked;
            selectCardsBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(delegate
            {
                TaskHelper.RunSafely(OpenLocalDeckCardPickerAsync(runState, sync));
            }));
            actionsRow.AddChildSafely(selectCardsBtn);

            Button clearOfferBtn = TradeUiStyle.CreatePlaqueButton(
                name: "ClearOfferButton",
                text: "Clear Offer (0 Cards)",
                minSize: new Vector2(220f, 50f),
                fontSize: 18,
                tint: new Color(0.95f, 0.86f, 0.78f),
                isBackOrCancel: true);
            clearOfferBtn.Disabled = entries.Count == 0 || isLocked;
            clearOfferBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(delegate
            {
                sync.SetLocalOfferedCards(Array.Empty<CardModel>());
            }));
            actionsRow.AddChildSafely(clearOfferBtn);
        }
        else
        {
            Label hintLabel = TradeUiStyle.CreateStsLabel(
                text: entries.Count == 0
                    ? "0 Cards Offered (5-for-0 Gifting Supported)"
                    : "Hover any offered card above to inspect its full text & upgrades",
                fontSize: 16,
                color: StsColors.halfTransparentCream,
                bold: false,
                outlineSize: 6,
                alignment: HorizontalAlignment.Center);
            actionsRow.AddChildSafely(hintLabel);
        }

        return panel;
    }

    private static Control BuildFilledCardSocket(
        TradeOfferEntry entry,
        int slotIndex,
        bool canRemove,
        TradeSessionSynchronizer sync)
    {
        var slotVBox = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(142f, 286f),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        slotVBox.AddThemeConstantOverride("separation", 6);

        var socketFrame = new Control
        {
            CustomMinimumSize = new Vector2(142f, 202f),
            MouseFilter = MouseFilterEnum.Pass
        };
        slotVBox.AddChildSafely(socketFrame);

        Texture2D? socketTex = TradeUiStyle.LoadUiTexture("card_slot_empty.png");
        if (socketTex != null)
        {
            var socketBg = new TextureRect
            {
                Texture = socketTex,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = MouseFilterEnum.Ignore
            };
            socketBg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            socketFrame.AddChildSafely(socketBg);
        }

        CardModel previewCard = TradeEligibility.CreatePreviewCard(entry.Card);
        NCard? nCard = NCard.Create(previewCard);
        if (nCard != null)
        {
            NPreviewCardHolder? previewHolder = NPreviewCardHolder.Create(
                nCard,
                showHoverTips: true,
                scaleOnHover: true);
            if (previewHolder != null)
            {
                previewHolder.SetCardScale(new Vector2(0.44f, 0.44f));
                previewHolder.Position = new Vector2(71f, 101f);
                socketFrame.AddChildSafely(previewHolder);
                nCard.UpdateVisuals(PileType.Deck, CardPreviewMode.Normal);
            }
            else
            {
                nCard.QueueFreeSafely();
            }
        }

        Label cardNameLabel = TradeUiStyle.CreateStsLabel(
            text: TradeEligibility.FormatCardLabel(previewCard),
            fontSize: 14,
            color: previewCard.IsUpgraded ? StsColors.green : StsColors.cream,
            bold: true,
            outlineSize: 6,
            alignment: HorizontalAlignment.Center);
        cardNameLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        cardNameLabel.CustomMinimumSize = new Vector2(138f, 38f);
        slotVBox.AddChildSafely(cardNameLabel);

        if (canRemove)
        {
            Button removeBtn = TradeUiStyle.CreatePlaqueButton(
                name: $"RemoveSlot_{slotIndex}",
                text: "Remove",
                minSize: new Vector2(118f, 34f),
                fontSize: 14,
                tint: new Color(1.06f, 0.76f, 0.74f),
                isBackOrCancel: true);
            removeBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(delegate
            {
                sync.RemoveCardFromLocalOfferAt(slotIndex);
            }));
            slotVBox.AddChildSafely(removeBtn);
        }
        else
        {
            var spacer = new Control
            {
                CustomMinimumSize = new Vector2(118f, 34f)
            };
            slotVBox.AddChildSafely(spacer);
        }

        return slotVBox;
    }

    private Control BuildEmptyCardSocket(
        int slotIndex,
        bool isClickableToSelect,
        IRunState runState,
        TradeSessionSynchronizer sync)
    {
        var slotVBox = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(142f, 286f),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        slotVBox.AddThemeConstantOverride("separation", 6);

        var socketFrame = new Control
        {
            CustomMinimumSize = new Vector2(142f, 202f),
            MouseFilter = isClickableToSelect ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore,
            MouseDefaultCursorShape = isClickableToSelect ? CursorShape.PointingHand : CursorShape.Arrow
        };
        slotVBox.AddChildSafely(socketFrame);

        Texture2D? socketTex = TradeUiStyle.LoadUiTexture("card_slot_empty.png");
        if (socketTex != null)
        {
            var socketBg = new TextureRect
            {
                Texture = socketTex,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                Modulate = new Color(1f, 1f, 1f, 0.88f),
                MouseFilter = MouseFilterEnum.Ignore
            };
            socketBg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            socketFrame.AddChildSafely(socketBg);

            if (isClickableToSelect)
            {
                socketFrame.MouseEntered += () =>
                {
                    SfxCmd.Play(TradeUiStyle.HoverSfx);
                    socketBg.Modulate = new Color(1.18f, 1.12f, 0.95f, 1f);
                };
                socketFrame.MouseExited += () =>
                {
                    socketBg.Modulate = new Color(1f, 1f, 1f, 0.88f);
                };
                socketFrame.GuiInput += (@event) =>
                {
                    if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                    {
                        SfxCmd.Play(TradeUiStyle.ClickSfx);
                        TaskHelper.RunSafely(OpenLocalDeckCardPickerAsync(runState, sync));
                    }
                };
            }
        }

        Label emptySlotLabel = TradeUiStyle.CreateStsLabel(
            text: isClickableToSelect ? $"Slot {slotIndex + 1}\n(+ Click to Offer)" : $"Empty Slot {slotIndex + 1}",
            fontSize: 13,
            color: StsColors.halfTransparentCream,
            bold: false,
            outlineSize: 5,
            alignment: HorizontalAlignment.Center);
        emptySlotLabel.CustomMinimumSize = new Vector2(138f, 38f);
        slotVBox.AddChildSafely(emptySlotLabel);

        var bottomSpacer = new Control
        {
            CustomMinimumSize = new Vector2(118f, 34f)
        };
        slotVBox.AddChildSafely(bottomSpacer);

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
