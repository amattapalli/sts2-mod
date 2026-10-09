using System.Collections.Generic;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace MultiplayerTrade.MultiplayerTradeCode.Net;

/// <summary>
/// Represents a single offered deck card in a multiplayer trade session, pairing its master deck index
/// with its full <see cref="SerializableCard"/> state (upgrade level, enchantment, saved properties).
/// </summary>
public struct TradeOfferEntry : IPacketSerializable
{
    /// <summary>
    /// 0-based index of the card inside the offering player's master deck (<c>Player.Deck.Cards</c>).
    /// </summary>
    public int DeckIndex;

    /// <summary>
    /// Full serialized snapshot of the offered card.
    /// </summary>
    public SerializableCard Card;

    /// <summary>
    /// Serializes the offer entry to the network packet writer.
    /// </summary>
    /// <param name="writer">Target packet writer.</param>
    public void Serialize(PacketWriter writer)
    {
        writer.WriteInt(DeckIndex);
        writer.Write(Card);
    }

    /// <summary>
    /// Deserializes the offer entry from the network packet reader.
    /// </summary>
    /// <param name="reader">Source packet reader.</param>
    public void Deserialize(PacketReader reader)
    {
        DeckIndex = reader.ReadInt();
        Card = reader.Read<SerializableCard>();
    }
}

/// <summary>
/// Broadcast message sent when an initiator invites a specific teammate to open a two-way barter session.
/// </summary>
public struct TradeInviteMessage : INetMessage
{
    /// <summary>
    /// Unique identifier for the barter session.
    /// </summary>
    public ulong SessionId;

    /// <summary>
    /// Network ID of the player initiating the trade.
    /// </summary>
    public ulong InitiatorNetId;

    /// <summary>
    /// Network ID of the invited trade partner.
    /// </summary>
    public ulong PartnerNetId;

    /// <inheritdoc />
    public bool ShouldBroadcast => true;

    /// <inheritdoc />
    public NetTransferMode Mode => NetTransferMode.Reliable;

    /// <inheritdoc />
    public LogLevel LogLevel => LogLevel.Info;

    /// <summary>
    /// Serializes the invite message to the packet writer.
    /// </summary>
    /// <param name="writer">Target packet writer.</param>
    public void Serialize(PacketWriter writer)
    {
        writer.WriteULong(SessionId);
        writer.WriteULong(InitiatorNetId);
        writer.WriteULong(PartnerNetId);
    }

    /// <summary>
    /// Deserializes the invite message from the packet reader.
    /// </summary>
    /// <param name="reader">Source packet reader.</param>
    public void Deserialize(PacketReader reader)
    {
        SessionId = reader.ReadULong();
        InitiatorNetId = reader.ReadULong();
        PartnerNetId = reader.ReadULong();
    }
}

/// <summary>
/// Broadcast message sent by the invited partner accepting or declining a trade invitation.
/// </summary>
public struct TradeInviteResponseMessage : INetMessage
{
    /// <summary>
    /// Unique identifier for the barter session.
    /// </summary>
    public ulong SessionId;

    /// <summary>
    /// Network ID of the player who initiated the trade.
    /// </summary>
    public ulong InitiatorNetId;

    /// <summary>
    /// Network ID of the invited trade partner responding to the invite.
    /// </summary>
    public ulong PartnerNetId;

    /// <summary>
    /// True if the partner accepted the trade invitation; false if declined or busy.
    /// </summary>
    public bool Accepted;

    /// <summary>
    /// Optional status/reason message when declining.
    /// </summary>
    public string Reason;

    /// <inheritdoc />
    public bool ShouldBroadcast => true;

    /// <inheritdoc />
    public NetTransferMode Mode => NetTransferMode.Reliable;

    /// <inheritdoc />
    public LogLevel LogLevel => LogLevel.Info;

    /// <summary>
    /// Serializes the invite response message to the packet writer.
    /// </summary>
    /// <param name="writer">Target packet writer.</param>
    public void Serialize(PacketWriter writer)
    {
        writer.WriteULong(SessionId);
        writer.WriteULong(InitiatorNetId);
        writer.WriteULong(PartnerNetId);
        writer.WriteBool(Accepted);
        writer.WriteString(Reason ?? string.Empty);
    }

    /// <summary>
    /// Deserializes the invite response message from the packet reader.
    /// </summary>
    /// <param name="reader">Source packet reader.</param>
    public void Deserialize(PacketReader reader)
    {
        SessionId = reader.ReadULong();
        InitiatorNetId = reader.ReadULong();
        PartnerNetId = reader.ReadULong();
        Accepted = reader.ReadBool();
        Reason = reader.ReadString();
    }
}

/// <summary>
/// Broadcast message sent whenever a participant updates their offered cards (0 to 5 cards) in the barter window.
/// Receiving this message automatically resets both participants' Lock and Confirm states to prevent bait-and-switch.
/// </summary>
public struct TradeOfferUpdatedMessage : INetMessage
{
    /// <summary>
    /// Unique identifier for the barter session.
    /// </summary>
    public ulong SessionId;

    /// <summary>
    /// Network ID of the trade initiator.
    /// </summary>
    public ulong InitiatorNetId;

    /// <summary>
    /// Network ID of the trade partner.
    /// </summary>
    public ulong PartnerNetId;

    /// <summary>
    /// List of 0 to 5 cards offered by the sender.
    /// </summary>
    public List<TradeOfferEntry> OfferedCards;

    /// <inheritdoc />
    public bool ShouldBroadcast => true;

    /// <inheritdoc />
    public NetTransferMode Mode => NetTransferMode.Reliable;

    /// <inheritdoc />
    public LogLevel LogLevel => LogLevel.Info;

    /// <summary>
    /// Serializes the offer update message to the packet writer.
    /// </summary>
    /// <param name="writer">Target packet writer.</param>
    public void Serialize(PacketWriter writer)
    {
        writer.WriteULong(SessionId);
        writer.WriteULong(InitiatorNetId);
        writer.WriteULong(PartnerNetId);
        writer.WriteList(OfferedCards ?? new List<TradeOfferEntry>(), 8);
    }

    /// <summary>
    /// Deserializes the offer update message from the packet reader.
    /// </summary>
    /// <param name="reader">Source packet reader.</param>
    public void Deserialize(PacketReader reader)
    {
        SessionId = reader.ReadULong();
        InitiatorNetId = reader.ReadULong();
        PartnerNetId = reader.ReadULong();
        OfferedCards = reader.ReadList<TradeOfferEntry>(8);
    }
}

/// <summary>
/// Broadcast message sent when a participant locks, unlocks, or confirms the current trade offer.
/// </summary>
public struct TradeLockChangedMessage : INetMessage
{
    /// <summary>
    /// Unique identifier for the barter session.
    /// </summary>
    public ulong SessionId;

    /// <summary>
    /// Network ID of the trade initiator.
    /// </summary>
    public ulong InitiatorNetId;

    /// <summary>
    /// Network ID of the trade partner.
    /// </summary>
    public ulong PartnerNetId;

    /// <summary>
    /// Whether the sender has locked their current offer.
    /// </summary>
    public bool IsLocked;

    /// <summary>
    /// Whether the sender has clicked final Confirm after both offers are locked.
    /// </summary>
    public bool IsConfirmed;

    /// <inheritdoc />
    public bool ShouldBroadcast => true;

    /// <inheritdoc />
    public NetTransferMode Mode => NetTransferMode.Reliable;

    /// <inheritdoc />
    public LogLevel LogLevel => LogLevel.Info;

    /// <summary>
    /// Serializes the lock/confirm state message to the packet writer.
    /// </summary>
    /// <param name="writer">Target packet writer.</param>
    public void Serialize(PacketWriter writer)
    {
        writer.WriteULong(SessionId);
        writer.WriteULong(InitiatorNetId);
        writer.WriteULong(PartnerNetId);
        writer.WriteBool(IsLocked);
        writer.WriteBool(IsConfirmed);
    }

    /// <summary>
    /// Deserializes the lock/confirm state message from the packet reader.
    /// </summary>
    /// <param name="reader">Source packet reader.</param>
    public void Deserialize(PacketReader reader)
    {
        SessionId = reader.ReadULong();
        InitiatorNetId = reader.ReadULong();
        PartnerNetId = reader.ReadULong();
        IsLocked = reader.ReadBool();
        IsConfirmed = reader.ReadBool();
    }
}

/// <summary>
/// Broadcast message sent when either participant cancels or closes the active barter session.
/// </summary>
public struct TradeCancelMessage : INetMessage
{
    /// <summary>
    /// Unique identifier for the barter session.
    /// </summary>
    public ulong SessionId;

    /// <summary>
    /// Network ID of the trade initiator.
    /// </summary>
    public ulong InitiatorNetId;

    /// <summary>
    /// Network ID of the trade partner.
    /// </summary>
    public ulong PartnerNetId;

    /// <summary>
    /// Optional human-readable cancellation reason.
    /// </summary>
    public string Reason;

    /// <inheritdoc />
    public bool ShouldBroadcast => true;

    /// <inheritdoc />
    public NetTransferMode Mode => NetTransferMode.Reliable;

    /// <inheritdoc />
    public LogLevel LogLevel => LogLevel.Info;

    /// <summary>
    /// Serializes the cancel message to the packet writer.
    /// </summary>
    /// <param name="writer">Target packet writer.</param>
    public void Serialize(PacketWriter writer)
    {
        writer.WriteULong(SessionId);
        writer.WriteULong(InitiatorNetId);
        writer.WriteULong(PartnerNetId);
        writer.WriteString(Reason ?? string.Empty);
    }

    /// <summary>
    /// Deserializes the cancel message from the packet reader.
    /// </summary>
    /// <param name="reader">Source packet reader.</param>
    public void Deserialize(PacketReader reader)
    {
        SessionId = reader.ReadULong();
        InitiatorNetId = reader.ReadULong();
        PartnerNetId = reader.ReadULong();
        Reason = reader.ReadString();
    }
}

/// <summary>
/// Location-targeted broadcast message sent by the trade initiator once both participants have locked
/// and confirmed their offers. Processed deterministically by every peer in the lobby via
/// <see cref="RunLocationTargetedMessageBuffer"/> so all players' <c>RunState</c> decks and relic triggers
/// stay 100% synchronized for <c>ChecksumTracker</c>.
/// </summary>
public struct TradeExecutedMessage : INetMessage, IRunLocationTargetedMessage
{
    /// <summary>
    /// Map location where the trade occurred.
    /// </summary>
    public RunLocation Location { get; set; }

    /// <summary>
    /// Unique identifier for the barter session.
    /// </summary>
    public ulong SessionId;

    /// <summary>
    /// Network ID of the trade initiator.
    /// </summary>
    public ulong InitiatorNetId;

    /// <summary>
    /// Network ID of the trade partner.
    /// </summary>
    public ulong PartnerNetId;

    /// <summary>
    /// Cards (0 to 5) given by the initiator to the partner.
    /// </summary>
    public List<TradeOfferEntry> InitiatorOffer;

    /// <summary>
    /// Cards (0 to 5) given by the partner to the initiator.
    /// </summary>
    public List<TradeOfferEntry> PartnerOffer;

    /// <inheritdoc />
    public bool ShouldBroadcast => true;

    /// <inheritdoc />
    public NetTransferMode Mode => NetTransferMode.Reliable;

    /// <inheritdoc />
    public LogLevel LogLevel => LogLevel.Info;

    /// <summary>
    /// Serializes the executed trade message to the packet writer.
    /// </summary>
    /// <param name="writer">Target packet writer.</param>
    public void Serialize(PacketWriter writer)
    {
        writer.Write(Location);
        writer.WriteULong(SessionId);
        writer.WriteULong(InitiatorNetId);
        writer.WriteULong(PartnerNetId);
        writer.WriteList(InitiatorOffer ?? new List<TradeOfferEntry>(), 8);
        writer.WriteList(PartnerOffer ?? new List<TradeOfferEntry>(), 8);
    }

    /// <summary>
    /// Deserializes the executed trade message from the packet reader.
    /// </summary>
    /// <param name="reader">Source packet reader.</param>
    public void Deserialize(PacketReader reader)
    {
        Location = reader.Read<RunLocation>();
        SessionId = reader.ReadULong();
        InitiatorNetId = reader.ReadULong();
        PartnerNetId = reader.ReadULong();
        InitiatorOffer = reader.ReadList<TradeOfferEntry>(8);
        PartnerOffer = reader.ReadList<TradeOfferEntry>(8);
    }
}
