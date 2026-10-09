using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
using MultiplayerTrade.MultiplayerTradeCode.Rules;

namespace MultiplayerTrade.MultiplayerTradeCode.Net;

/// <summary>
/// Lifecycle phases of a two-way multiplayer card trade session.
/// </summary>
public enum TradeSessionPhase
{
    /// <summary>
    /// Initiator has sent an invite and is waiting for the partner to accept or decline.
    /// </summary>
    PendingInvite,

    /// <summary>
    /// Both players are in the barter window selecting up to 5 cards, locking, and confirming.
    /// </summary>
    Negotiating,

    /// <summary>
    /// Both players have locked and confirmed; the deterministic trade message is executing.
    /// </summary>
    Executing,

    /// <summary>
    /// The trade completed and cards + relic triggers have been applied.
    /// </summary>
    Completed,

    /// <summary>
    /// The trade session was declined or cancelled.
    /// </summary>
    Cancelled
}

/// <summary>
/// Holds the live state of an active two-way barter session between two players.
/// </summary>
public sealed class TradeSessionState
{
    /// <summary>
    /// Unique session identifier.
    /// </summary>
    public ulong SessionId { get; init; }

    /// <summary>
    /// Network ID of the player who initiated the trade.
    /// </summary>
    public ulong InitiatorNetId { get; init; }

    /// <summary>
    /// Network ID of the invited trade partner.
    /// </summary>
    public ulong PartnerNetId { get; init; }

    /// <summary>
    /// Current lifecycle phase of the session.
    /// </summary>
    public TradeSessionPhase Phase { get; set; } = TradeSessionPhase.PendingInvite;

    /// <summary>
    /// Cards (0 to 5) currently offered by the initiator.
    /// </summary>
    public List<TradeOfferEntry> InitiatorOffer { get; set; } = new();

    /// <summary>
    /// Cards (0 to 5) currently offered by the partner.
    /// </summary>
    public List<TradeOfferEntry> PartnerOffer { get; set; } = new();

    /// <summary>
    /// True if the initiator has locked their current offer.
    /// </summary>
    public bool InitiatorLocked { get; set; }

    /// <summary>
    /// True if the partner has locked their current offer.
    /// </summary>
    public bool PartnerLocked { get; set; }

    /// <summary>
    /// True if the initiator has clicked final Confirm after both offers are locked.
    /// </summary>
    public bool InitiatorConfirmed { get; set; }

    /// <summary>
    /// True if the partner has clicked final Confirm after both offers are locked.
    /// </summary>
    public bool PartnerConfirmed { get; set; }

    /// <summary>
    /// Human-readable status banner text for the barter modal.
    /// </summary>
    public string StatusMessage { get; set; } = string.Empty;

    /// <summary>
    /// Resets both participants' Lock and Confirm flags whenever either offer is modified.
    /// </summary>
    public void ResetLocksAndConfirmations()
    {
        InitiatorLocked = false;
        PartnerLocked = false;
        InitiatorConfirmed = false;
        PartnerConfirmed = false;
    }
}

/// <summary>
/// Coordinates multiplayer card trade invitations, real-time offer updates, lock/confirm handshakes,
/// and deterministic <see cref="RunState"/> deck mutations + relic triggers across all peers.
/// </summary>
public sealed class TradeSessionSynchronizer : IDisposable
{
    private readonly RunLocationTargetedMessageBuffer _messageBuffer;
    private readonly INetGameService _netService;
    private readonly RunState _runState;
    private readonly ulong _localPlayerId;
    private readonly HashSet<ulong> _executedSessionIds = new();

    /// <summary>
    /// Singleton synchronizer instance for the currently active run, or null when no run is active.
    /// </summary>
    public static TradeSessionSynchronizer? Instance { get; private set; }

    /// <summary>
    /// Currently active barter session involving the local player, if any.
    /// </summary>
    public TradeSessionState? ActiveSession { get; private set; }

    /// <summary>
    /// Raised whenever <see cref="ActiveSession"/> state changes, a new session starts, or a session ends.
    /// </summary>
    public event Action<TradeSessionState?>? SessionStateChanged;

    /// <summary>
    /// Raised when an incoming trade invite arrives for the local player so the UI can open the barter modal.
    /// </summary>
    public event Action<TradeSessionState>? IncomingInviteReceived;

    private TradeSessionSynchronizer(
        RunLocationTargetedMessageBuffer messageBuffer,
        INetGameService netService,
        RunState runState,
        ulong localPlayerId)
    {
        _messageBuffer = messageBuffer;
        _netService = netService;
        _runState = runState;
        _localPlayerId = localPlayerId;

        _netService.RegisterMessageHandler<TradeInviteMessage>(HandleTradeInviteMessage);
        _netService.RegisterMessageHandler<TradeInviteResponseMessage>(HandleTradeInviteResponseMessage);
        _netService.RegisterMessageHandler<TradeOfferUpdatedMessage>(HandleTradeOfferUpdatedMessage);
        _netService.RegisterMessageHandler<TradeLockChangedMessage>(HandleTradeLockChangedMessage);
        _netService.RegisterMessageHandler<TradeCancelMessage>(HandleTradeCancelMessage);
        _messageBuffer.RegisterMessageHandler<TradeExecutedMessage>(HandleTradeExecutedMessage);
    }

    /// <summary>
    /// Initializes or replaces the active <see cref="TradeSessionSynchronizer"/> for a newly started or loaded run.
    /// </summary>
    /// <param name="messageBuffer">Run location targeted message buffer from <see cref="RunManager"/>.</param>
    /// <param name="netService">Network game service from <see cref="RunManager"/>.</param>
    /// <param name="runState">Current run state.</param>
    /// <param name="localPlayerId">Local player's network ID.</param>
    public static void InitializeForRun(
        RunLocationTargetedMessageBuffer messageBuffer,
        INetGameService netService,
        RunState runState,
        ulong localPlayerId)
    {
        Instance?.Dispose();
        Instance = new TradeSessionSynchronizer(messageBuffer, netService, runState, localPlayerId);
        MainFile.Logger.Info($"TradeSessionSynchronizer initialized for localPlayerId={localPlayerId}.");
    }

    /// <summary>
    /// Disposes the current singleton synchronizer when the run cleans up.
    /// </summary>
    public static void Shutdown()
    {
        Instance?.Dispose();
        Instance = null;
    }

    /// <summary>
    /// Unregisters all network and location-targeted message handlers.
    /// </summary>
    public void Dispose()
    {
        _netService.UnregisterMessageHandler<TradeInviteMessage>(HandleTradeInviteMessage);
        _netService.UnregisterMessageHandler<TradeInviteResponseMessage>(HandleTradeInviteResponseMessage);
        _netService.UnregisterMessageHandler<TradeOfferUpdatedMessage>(HandleTradeOfferUpdatedMessage);
        _netService.UnregisterMessageHandler<TradeLockChangedMessage>(HandleTradeLockChangedMessage);
        _netService.UnregisterMessageHandler<TradeCancelMessage>(HandleTradeCancelMessage);
        _messageBuffer.UnregisterMessageHandler<TradeExecutedMessage>(HandleTradeExecutedMessage);
        ActiveSession = null;
    }

    /// <summary>
    /// Returns the human-readable display name and character title for a player in the current run.
    /// </summary>
    /// <param name="netId">The player's network ID.</param>
    /// <returns>Formatted player display name.</returns>
    public string GetPlayerDisplayName(ulong netId)
    {
        Player? player = _runState.GetPlayer(netId);
        string platformName;
        try
        {
            platformName = PlatformUtil.GetPlayerName(_netService.Platform, netId);
        }
        catch
        {
            platformName = $"Player {netId}";
        }

        if (string.IsNullOrWhiteSpace(platformName))
        {
            platformName = $"Player {_runState.GetPlayerSlotIndex(netId) + 1}";
        }

        if (player?.Character?.Title != null)
        {
            return $"{platformName} ({player.Character.Title.GetFormattedText()})";
        }

        return platformName;
    }

    /// <summary>
    /// Starts a new trade session as the initiator and sends a <see cref="TradeInviteMessage"/> to the partner.
    /// </summary>
    /// <param name="partnerNetId">Network ID of the teammate to trade with.</param>
    /// <returns>The newly created <see cref="TradeSessionState"/>, or null if trading is not currently allowed.</returns>
    public TradeSessionState? StartTradeSession(ulong partnerNetId)
    {
        if (!TradeEligibility.CanTradeInCurrentRoom(_runState))
        {
            return null;
        }

        if (partnerNetId == _localPlayerId || _runState.GetPlayer(partnerNetId) == null)
        {
            return null;
        }

        if (ActiveSession != null &&
            (ActiveSession.Phase == TradeSessionPhase.PendingInvite ||
             ActiveSession.Phase == TradeSessionPhase.Negotiating ||
             ActiveSession.Phase == TradeSessionPhase.Executing))
        {
            return ActiveSession;
        }

        ulong timestampPart = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        ulong sessionId = (timestampPart << 16) ^ (_localPlayerId & 0xFFFFUL);

        var session = new TradeSessionState
        {
            SessionId = sessionId,
            InitiatorNetId = _localPlayerId,
            PartnerNetId = partnerNetId,
            Phase = TradeSessionPhase.PendingInvite,
            StatusMessage = $"Waiting for {GetPlayerDisplayName(partnerNetId)} to accept trade invite..."
        };

        ActiveSession = session;
        _netService.SendMessage(new TradeInviteMessage
        {
            SessionId = sessionId,
            InitiatorNetId = _localPlayerId,
            PartnerNetId = partnerNetId
        });

        SessionStateChanged?.Invoke(ActiveSession);
        return session;
    }

    /// <summary>
    /// Called by the invited partner to accept or decline the pending trade invitation.
    /// </summary>
    /// <param name="accepted">True to accept and begin negotiating; false to decline.</param>
    /// <param name="reason">Optional reason string when declining.</param>
    public void RespondToInvite(bool accepted, string reason = "")
    {
        if (ActiveSession == null || ActiveSession.Phase != TradeSessionPhase.PendingInvite)
        {
            return;
        }

        if (ActiveSession.PartnerNetId != _localPlayerId)
        {
            return;
        }

        var session = ActiveSession;
        _netService.SendMessage(new TradeInviteResponseMessage
        {
            SessionId = session.SessionId,
            InitiatorNetId = session.InitiatorNetId,
            PartnerNetId = session.PartnerNetId,
            Accepted = accepted,
            Reason = reason
        });

        if (accepted)
        {
            session.Phase = TradeSessionPhase.Negotiating;
            session.StatusMessage = "Select up to 5 cards to offer (or 0 cards for a gift), then Lock Offer.";
            SessionStateChanged?.Invoke(session);
        }
        else
        {
            session.Phase = TradeSessionPhase.Cancelled;
            session.StatusMessage = "Trade invitation declined.";
            ActiveSession = null;
            SessionStateChanged?.Invoke(null);
        }
    }

    /// <summary>
    /// Updates the local player's offered cards (0 to 5 cards), resets both players' Lock/Confirm states,
    /// and broadcasts <see cref="TradeOfferUpdatedMessage"/> to the partner.
    /// </summary>
    /// <param name="selectedCards">The 0 to 5 deck cards selected by the local player.</param>
    public void SetLocalOfferedCards(IReadOnlyList<CardModel> selectedCards)
    {
        if (ActiveSession == null || ActiveSession.Phase != TradeSessionPhase.Negotiating)
        {
            return;
        }

        Player? localPlayer = _runState.GetPlayer(_localPlayerId);
        if (localPlayer?.Deck?.Cards == null)
        {
            return;
        }

        IReadOnlyList<CardModel> deckCards = localPlayer.Deck.Cards;
        var entries = new List<TradeOfferEntry>();
        var usedIndices = new HashSet<int>();

        foreach (CardModel card in selectedCards.Take(TradeEligibility.MaxCardsPerPlayer))
        {
            if (!TradeEligibility.CanTradeCard(card))
            {
                continue;
            }

            int foundIndex = -1;
            for (int i = 0; i < deckCards.Count; i++)
            {
                if (!usedIndices.Contains(i) && ReferenceEquals(deckCards[i], card))
                {
                    foundIndex = i;
                    usedIndices.Add(i);
                    break;
                }
            }

            if (foundIndex < 0)
            {
                continue;
            }

            entries.Add(new TradeOfferEntry
            {
                DeckIndex = foundIndex,
                Card = card.ToSerializable()
            });
        }

        if (_localPlayerId == ActiveSession.InitiatorNetId)
        {
            ActiveSession.InitiatorOffer = entries;
        }
        else if (_localPlayerId == ActiveSession.PartnerNetId)
        {
            ActiveSession.PartnerOffer = entries;
        }
        else
        {
            return;
        }

        ActiveSession.ResetLocksAndConfirmations();
        ActiveSession.StatusMessage = "Offer updated. Both players must Lock Offer before confirming.";

        _netService.SendMessage(new TradeOfferUpdatedMessage
        {
            SessionId = ActiveSession.SessionId,
            InitiatorNetId = ActiveSession.InitiatorNetId,
            PartnerNetId = ActiveSession.PartnerNetId,
            OfferedCards = entries
        });

        SessionStateChanged?.Invoke(ActiveSession);
    }

    /// <summary>
    /// Removes a single card at the given offer slot index from the local player's current offer.
    /// </summary>
    /// <param name="offerSlotIndex">0-based index within the local player's current offer list.</param>
    public void RemoveCardFromLocalOfferAt(int offerSlotIndex)
    {
        if (ActiveSession == null || ActiveSession.Phase != TradeSessionPhase.Negotiating)
        {
            return;
        }

        List<TradeOfferEntry> currentOffer = _localPlayerId == ActiveSession.InitiatorNetId
            ? ActiveSession.InitiatorOffer
            : ActiveSession.PartnerOffer;

        if (offerSlotIndex < 0 || offerSlotIndex >= currentOffer.Count)
        {
            return;
        }

        currentOffer.RemoveAt(offerSlotIndex);
        ActiveSession.ResetLocksAndConfirmations();
        ActiveSession.StatusMessage = "Offer updated. Both players must Lock Offer before confirming.";

        _netService.SendMessage(new TradeOfferUpdatedMessage
        {
            SessionId = ActiveSession.SessionId,
            InitiatorNetId = ActiveSession.InitiatorNetId,
            PartnerNetId = ActiveSession.PartnerNetId,
            OfferedCards = new List<TradeOfferEntry>(currentOffer)
        });

        SessionStateChanged?.Invoke(ActiveSession);
    }

    /// <summary>
    /// Toggles the local player's Lock state on their current offer.
    /// Unlocking automatically clears both players' Confirm flags.
    /// </summary>
    public void ToggleLocalLock()
    {
        if (ActiveSession == null || ActiveSession.Phase != TradeSessionPhase.Negotiating)
        {
            return;
        }

        bool newLockState;
        if (_localPlayerId == ActiveSession.InitiatorNetId)
        {
            ActiveSession.InitiatorLocked = !ActiveSession.InitiatorLocked;
            newLockState = ActiveSession.InitiatorLocked;
        }
        else if (_localPlayerId == ActiveSession.PartnerNetId)
        {
            ActiveSession.PartnerLocked = !ActiveSession.PartnerLocked;
            newLockState = ActiveSession.PartnerLocked;
        }
        else
        {
            return;
        }

        ActiveSession.InitiatorConfirmed = false;
        ActiveSession.PartnerConfirmed = false;
        UpdateNegotiationStatusMessage(ActiveSession);

        _netService.SendMessage(new TradeLockChangedMessage
        {
            SessionId = ActiveSession.SessionId,
            InitiatorNetId = ActiveSession.InitiatorNetId,
            PartnerNetId = ActiveSession.PartnerNetId,
            IsLocked = newLockState,
            IsConfirmed = false
        });

        SessionStateChanged?.Invoke(ActiveSession);
    }

    /// <summary>
    /// Marks the local player as having confirmed the locked trade.
    /// Once both participants have locked and confirmed a valid offer (at least 1 card total, up to 5 per side),
    /// the initiator broadcasts <see cref="TradeExecutedMessage"/> and executes the swap.
    /// </summary>
    public void ConfirmLockedTrade()
    {
        if (ActiveSession == null || ActiveSession.Phase != TradeSessionPhase.Negotiating)
        {
            return;
        }

        if (!ActiveSession.InitiatorLocked || !ActiveSession.PartnerLocked)
        {
            ActiveSession.StatusMessage = "Both players must Lock Offer before confirming!";
            SessionStateChanged?.Invoke(ActiveSession);
            return;
        }

        if (!TradeEligibility.IsValidOfferCount(ActiveSession.InitiatorOffer.Count, ActiveSession.PartnerOffer.Count))
        {
            ActiveSession.StatusMessage = "At least 1 card must be offered across both sides to confirm a trade.";
            SessionStateChanged?.Invoke(ActiveSession);
            return;
        }

        if (_localPlayerId == ActiveSession.InitiatorNetId)
        {
            ActiveSession.InitiatorConfirmed = true;
        }
        else if (_localPlayerId == ActiveSession.PartnerNetId)
        {
            ActiveSession.PartnerConfirmed = true;
        }
        else
        {
            return;
        }

        UpdateNegotiationStatusMessage(ActiveSession);

        _netService.SendMessage(new TradeLockChangedMessage
        {
            SessionId = ActiveSession.SessionId,
            InitiatorNetId = ActiveSession.InitiatorNetId,
            PartnerNetId = ActiveSession.PartnerNetId,
            IsLocked = true,
            IsConfirmed = true
        });

        SessionStateChanged?.Invoke(ActiveSession);
        TryDispatchTradeExecutionIfBothConfirmed();
    }

    /// <summary>
    /// Cancels the active trade session and notifies the partner.
    /// </summary>
    /// <param name="reason">Human-readable cancellation reason.</param>
    public void CancelActiveSession(string reason = "Cancelled by player.")
    {
        if (ActiveSession == null)
        {
            return;
        }

        var session = ActiveSession;
        ActiveSession = null;

        if (session.Phase == TradeSessionPhase.PendingInvite || session.Phase == TradeSessionPhase.Negotiating)
        {
            _netService.SendMessage(new TradeCancelMessage
            {
                SessionId = session.SessionId,
                InitiatorNetId = session.InitiatorNetId,
                PartnerNetId = session.PartnerNetId,
                Reason = reason
            });
        }

        session.Phase = TradeSessionPhase.Cancelled;
        session.StatusMessage = reason;
        SessionStateChanged?.Invoke(null);
    }

    /// <summary>
    /// Automatically cancels any in-progress negotiation when the players leave the Shop or Rest Site room.
    /// </summary>
    public void CancelActiveSessionOnRoomExit()
    {
        if (ActiveSession != null &&
            (ActiveSession.Phase == TradeSessionPhase.PendingInvite ||
             ActiveSession.Phase == TradeSessionPhase.Negotiating))
        {
            CancelActiveSession("Left room.");
        }
    }

    private void TryDispatchTradeExecutionIfBothConfirmed()
    {
        if (ActiveSession == null || ActiveSession.Phase != TradeSessionPhase.Negotiating)
        {
            return;
        }

        if (!ActiveSession.InitiatorLocked || !ActiveSession.PartnerLocked ||
            !ActiveSession.InitiatorConfirmed || !ActiveSession.PartnerConfirmed)
        {
            return;
        }

        if (!TradeEligibility.IsValidOfferCount(ActiveSession.InitiatorOffer.Count, ActiveSession.PartnerOffer.Count))
        {
            return;
        }

        // Only the initiator dispatches TradeExecutedMessage so exactly one authoritative message is broadcast.
        if (_localPlayerId != ActiveSession.InitiatorNetId)
        {
            ActiveSession.StatusMessage = "Finalizing trade...";
            SessionStateChanged?.Invoke(ActiveSession);
            return;
        }

        ActiveSession.Phase = TradeSessionPhase.Executing;
        ActiveSession.StatusMessage = "Executing trade...";
        SessionStateChanged?.Invoke(ActiveSession);

        var executedMessage = new TradeExecutedMessage
        {
            Location = _runState.CurrentLocation,
            SessionId = ActiveSession.SessionId,
            InitiatorNetId = ActiveSession.InitiatorNetId,
            PartnerNetId = ActiveSession.PartnerNetId,
            InitiatorOffer = new List<TradeOfferEntry>(ActiveSession.InitiatorOffer),
            PartnerOffer = new List<TradeOfferEntry>(ActiveSession.PartnerOffer)
        };

        _netService.SendMessage(executedMessage);
        TaskHelper.RunSafely(ExecuteTradeDeterministicallyAsync(executedMessage));
    }

    private void HandleTradeInviteMessage(TradeInviteMessage message, ulong senderId)
    {
        if (message.PartnerNetId != _localPlayerId)
        {
            return;
        }

        if (!TradeEligibility.CanTradeInCurrentRoom(_runState) ||
            (ActiveSession != null &&
             (ActiveSession.Phase == TradeSessionPhase.PendingInvite ||
              ActiveSession.Phase == TradeSessionPhase.Negotiating ||
              ActiveSession.Phase == TradeSessionPhase.Executing)))
        {
            _netService.SendMessage(new TradeInviteResponseMessage
            {
                SessionId = message.SessionId,
                InitiatorNetId = message.InitiatorNetId,
                PartnerNetId = message.PartnerNetId,
                Accepted = false,
                Reason = "Player is currently busy."
            });
            return;
        }

        var session = new TradeSessionState
        {
            SessionId = message.SessionId,
            InitiatorNetId = message.InitiatorNetId,
            PartnerNetId = message.PartnerNetId,
            Phase = TradeSessionPhase.PendingInvite,
            StatusMessage = $"{GetPlayerDisplayName(message.InitiatorNetId)} wants to trade cards with you!"
        };

        ActiveSession = session;
        IncomingInviteReceived?.Invoke(session);
        SessionStateChanged?.Invoke(session);
    }

    private void HandleTradeInviteResponseMessage(TradeInviteResponseMessage message, ulong senderId)
    {
        if (ActiveSession == null || ActiveSession.SessionId != message.SessionId)
        {
            return;
        }

        if (message.Accepted)
        {
            ActiveSession.Phase = TradeSessionPhase.Negotiating;
            ActiveSession.StatusMessage = "Select up to 5 cards to offer (or 0 cards for a gift), then Lock Offer.";
            SessionStateChanged?.Invoke(ActiveSession);
        }
        else
        {
            string partnerName = GetPlayerDisplayName(message.PartnerNetId);
            ActiveSession.Phase = TradeSessionPhase.Cancelled;
            ActiveSession.StatusMessage = string.IsNullOrWhiteSpace(message.Reason)
                ? $"{partnerName} declined the trade request."
                : $"{partnerName} declined: {message.Reason}";
            ActiveSession = null;
            SessionStateChanged?.Invoke(null);
        }
    }

    private void HandleTradeOfferUpdatedMessage(TradeOfferUpdatedMessage message, ulong senderId)
    {
        if (ActiveSession == null || ActiveSession.SessionId != message.SessionId ||
            ActiveSession.Phase != TradeSessionPhase.Negotiating)
        {
            return;
        }

        List<TradeOfferEntry> sanitized = (message.OfferedCards ?? new List<TradeOfferEntry>())
            .Take(TradeEligibility.MaxCardsPerPlayer)
            .ToList();

        if (senderId == ActiveSession.InitiatorNetId)
        {
            ActiveSession.InitiatorOffer = sanitized;
        }
        else if (senderId == ActiveSession.PartnerNetId)
        {
            ActiveSession.PartnerOffer = sanitized;
        }
        else
        {
            return;
        }

        ActiveSession.ResetLocksAndConfirmations();
        ActiveSession.StatusMessage = $"{GetPlayerDisplayName(senderId)} updated their offer. Lock Offer when ready.";
        SessionStateChanged?.Invoke(ActiveSession);
    }

    private void HandleTradeLockChangedMessage(TradeLockChangedMessage message, ulong senderId)
    {
        if (ActiveSession == null || ActiveSession.SessionId != message.SessionId ||
            ActiveSession.Phase != TradeSessionPhase.Negotiating)
        {
            return;
        }

        if (senderId == ActiveSession.InitiatorNetId)
        {
            ActiveSession.InitiatorLocked = message.IsLocked;
            ActiveSession.InitiatorConfirmed = message.IsLocked && message.IsConfirmed;
        }
        else if (senderId == ActiveSession.PartnerNetId)
        {
            ActiveSession.PartnerLocked = message.IsLocked;
            ActiveSession.PartnerConfirmed = message.IsLocked && message.IsConfirmed;
        }
        else
        {
            return;
        }

        if (!ActiveSession.InitiatorLocked || !ActiveSession.PartnerLocked)
        {
            ActiveSession.InitiatorConfirmed = false;
            ActiveSession.PartnerConfirmed = false;
        }

        UpdateNegotiationStatusMessage(ActiveSession);
        SessionStateChanged?.Invoke(ActiveSession);
        TryDispatchTradeExecutionIfBothConfirmed();
    }

    private void HandleTradeCancelMessage(TradeCancelMessage message, ulong senderId)
    {
        if (ActiveSession == null || ActiveSession.SessionId != message.SessionId)
        {
            return;
        }

        ActiveSession.Phase = TradeSessionPhase.Cancelled;
        ActiveSession.StatusMessage = string.IsNullOrWhiteSpace(message.Reason)
            ? "Trade cancelled by teammate."
            : message.Reason;
        ActiveSession = null;
        SessionStateChanged?.Invoke(null);
    }

    private void HandleTradeExecutedMessage(TradeExecutedMessage message, ulong senderId)
    {
        TaskHelper.RunSafely(ExecuteTradeDeterministicallyAsync(message));
    }

    /// <summary>
    /// Deterministically executes the card trade on all peers in the lobby, removing offered cards from
    /// each giver's deck and adding them as newly owned mutable cards to each receiver's deck via
    /// <see cref="CardPileCmd.Add(IEnumerable{CardModel}, PileType, CardPilePosition, AbstractModel?, bool)"/>
    /// so all deck-addition hooks and relics (e.g. +15 gold, Egg upgrades) trigger identically across peers.
    /// </summary>
    /// <param name="message">The authoritative trade execution message.</param>
    public async Task ExecuteTradeDeterministicallyAsync(TradeExecutedMessage message)
    {
        if (!_executedSessionIds.Add(message.SessionId))
        {
            return;
        }

        Player? initiator = _runState.GetPlayer(message.InitiatorNetId);
        Player? partner = _runState.GetPlayer(message.PartnerNetId);
        if (initiator == null || partner == null)
        {
            MainFile.Logger.Error(
                $"Cannot execute trade session {message.SessionId}: initiator={message.InitiatorNetId} or partner={message.PartnerNetId} not found.");
            return;
        }

        List<TradeOfferEntry> initiatorEntries = (message.InitiatorOffer ?? new List<TradeOfferEntry>())
            .Take(TradeEligibility.MaxCardsPerPlayer)
            .ToList();
        List<TradeOfferEntry> partnerEntries = (message.PartnerOffer ?? new List<TradeOfferEntry>())
            .Take(TradeEligibility.MaxCardsPerPlayer)
            .ToList();

        List<CardModel> initiatorCardsToRemove = ResolveCardsToRemove(initiator, initiatorEntries);
        List<CardModel> partnerCardsToRemove = ResolveCardsToRemove(partner, partnerEntries);

        if (!TradeEligibility.IsValidOfferCount(initiatorCardsToRemove.Count, partnerCardsToRemove.Count))
        {
            MainFile.Logger.Warn(
                $"Trade session {message.SessionId} aborted: resolved card counts ({initiatorCardsToRemove.Count}, {partnerCardsToRemove.Count}) are invalid.");
            if (ActiveSession?.SessionId == message.SessionId)
            {
                ActiveSession = null;
                SessionStateChanged?.Invoke(null);
            }
            return;
        }

        MainFile.Logger.Info(
            $"Executing trade session {message.SessionId}: initiator {initiator.NetId} gives {initiatorCardsToRemove.Count} card(s), partner {partner.NetId} gives {partnerCardsToRemove.Count} card(s).");

        // Close the barter modal on participating clients before playing card previews/relic flashes.
        if (ActiveSession?.SessionId == message.SessionId)
        {
            ActiveSession.Phase = TradeSessionPhase.Completed;
            ActiveSession.StatusMessage = "Trade complete!";
            ActiveSession = null;
            SessionStateChanged?.Invoke(null);
        }

        // 1. Remove offered cards from Initiator, then from Partner, in deterministic order.
        if (initiatorCardsToRemove.Count > 0)
        {
            await CardPileCmd.RemoveFromDeck(initiatorCardsToRemove, showPreview: false);
        }

        if (partnerCardsToRemove.Count > 0)
        {
            await CardPileCmd.RemoveFromDeck(partnerCardsToRemove, showPreview: false);
        }

        // 2. Materialize and add Initiator's offered cards to Partner's deck (triggering Partner's relics).
        if (initiatorEntries.Count > 0)
        {
            await AddTradedCardsToReceiverDeckAsync(partner, initiatorEntries.Take(initiatorCardsToRemove.Count).ToList());
        }

        // 3. Materialize and add Partner's offered cards to Initiator's deck (triggering Initiator's relics).
        if (partnerEntries.Count > 0)
        {
            await AddTradedCardsToReceiverDeckAsync(initiator, partnerEntries.Take(partnerCardsToRemove.Count).ToList());
        }
    }

    private async Task AddTradedCardsToReceiverDeckAsync(Player receiver, List<TradeOfferEntry> receivedEntries)
    {
        if (receivedEntries.Count == 0)
        {
            return;
        }

        var loadedCards = new List<CardModel>(receivedEntries.Count);
        foreach (TradeOfferEntry entry in receivedEntries)
        {
            CardModel newCard = _runState.LoadCard(entry.Card, receiver);
            loadedCards.Add(newCard);
        }

        IReadOnlyList<CardPileAddResult> addResults = await CardPileCmd.Add(loadedCards, PileType.Deck);

        // If a relic (such as MoltenEgg / ToxicEgg / FrozenEgg) cloned & replaced the card during
        // Hook.ModifyCardBeingAddedToDeck, remove the un-added pre-clone instance from RunState.
        for (int i = 0; i < loadedCards.Count && i < addResults.Count; i++)
        {
            CardModel rawCard = loadedCards[i];
            CardModel finalCard = addResults[i].cardAdded;
            if (finalCard != null && !ReferenceEquals(rawCard, finalCard) && rawCard.Pile == null)
            {
                _runState.RemoveCard(rawCard);
            }
        }

        if (LocalContext.IsMe(receiver) && addResults.Any(r => r.success))
        {
            CardPreviewStyle previewStyle = addResults.Count > 3
                ? CardPreviewStyle.MessyLayout
                : CardPreviewStyle.HorizontalLayout;
            CardCmd.PreviewCardPileAdd(addResults, 1.2f, previewStyle);
        }
    }

    private static List<CardModel> ResolveCardsToRemove(Player giver, List<TradeOfferEntry> entries)
    {
        var resolved = new List<CardModel>();
        IReadOnlyList<CardModel> deckCards = giver.Deck.Cards;
        var usedIndices = new HashSet<int>();

        foreach (TradeOfferEntry entry in entries)
        {
            CardModel? matched = null;

            // Primary lookup: exact deck index + matching ModelId.
            if (entry.DeckIndex >= 0 &&
                entry.DeckIndex < deckCards.Count &&
                !usedIndices.Contains(entry.DeckIndex) &&
                deckCards[entry.DeckIndex].Id == entry.Card.Id &&
                TradeEligibility.CanTradeCard(deckCards[entry.DeckIndex]))
            {
                matched = deckCards[entry.DeckIndex];
                usedIndices.Add(entry.DeckIndex);
            }
            else
            {
                // Secondary lookup: match by ModelId, UpgradeLevel, and Enchantment.
                for (int i = 0; i < deckCards.Count; i++)
                {
                    if (usedIndices.Contains(i))
                    {
                        continue;
                    }

                    CardModel candidate = deckCards[i];
                    if (candidate.Id == entry.Card.Id &&
                        candidate.CurrentUpgradeLevel == entry.Card.CurrentUpgradeLevel &&
                        candidate.Enchantment?.Id == entry.Card.Enchantment?.Id &&
                        TradeEligibility.CanTradeCard(candidate))
                    {
                        matched = candidate;
                        usedIndices.Add(i);
                        break;
                    }
                }
            }

            if (matched != null)
            {
                resolved.Add(matched);
            }
        }

        return resolved;
    }

    private static void UpdateNegotiationStatusMessage(TradeSessionState session)
    {
        int totalCards = session.InitiatorOffer.Count + session.PartnerOffer.Count;
        if (totalCards == 0)
        {
            session.StatusMessage = "Select 1 to 5 cards on either side (up to 5-for-0 supported), then Lock Offer.";
            return;
        }

        if (!session.InitiatorLocked || !session.PartnerLocked)
        {
            session.StatusMessage = "Waiting for both players to Lock Offer...";
            return;
        }

        if (!session.InitiatorConfirmed || !session.PartnerConfirmed)
        {
            session.StatusMessage = "Both offers locked! Click Confirm Trade to execute the swap.";
            return;
        }

        session.StatusMessage = "Executing trade...";
    }
}
