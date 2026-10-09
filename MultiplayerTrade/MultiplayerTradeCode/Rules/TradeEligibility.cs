using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace MultiplayerTrade.MultiplayerTradeCode.Rules;

/// <summary>
/// Pure validation rules governing when players can trade and which deck cards are eligible.
/// Supports asymmetric trades up to <see cref="MaxCardsPerPlayer"/> cards per side (including 5-for-0 gifting).
/// </summary>
public static class TradeEligibility
{
    /// <summary>
    /// Maximum number of cards a single player may offer in one trade session.
    /// </summary>
    public const int MaxCardsPerPlayer = 5;

    /// <summary>
    /// Returns true if the current run state is a multiplayer co-op run currently inside a Shop or Rest Site room
    /// and not in active combat.
    /// </summary>
    /// <param name="runState">The active run state to inspect.</param>
    /// <returns>True if card trading is currently permitted in the active room.</returns>
    public static bool CanTradeInCurrentRoom(IRunState? runState)
    {
        if (runState == null || runState.Players.Count <= 1)
        {
            return false;
        }

        if (CombatManager.Instance.IsInProgress)
        {
            return false;
        }

        AbstractRoom? currentRoom = runState.CurrentRoom;
        if (currentRoom == null)
        {
            return false;
        }

        return currentRoom.RoomType == RoomType.Shop || currentRoom.RoomType == RoomType.RestSite;
    }

    /// <summary>
    /// Returns true if the specified card is in a player's master deck and is eligible to be traded.
    /// Excludes Curses, Statuses, Quests, and unremovable (<c>Eternal</c>) cards.
    /// </summary>
    /// <param name="card">The card model to check.</param>
    /// <returns>True if the card can be offered in a trade.</returns>
    public static bool CanTradeCard(CardModel? card)
    {
        if (card == null)
        {
            return false;
        }

        if (card.Pile?.Type != PileType.Deck)
        {
            return false;
        }

        if (!card.IsRemovable)
        {
            return false;
        }

        if (card.Type == CardType.Curse || card.Type == CardType.Status || card.Type == CardType.Quest)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Returns all tradable cards currently in the specified player's master deck.
    /// </summary>
    /// <param name="player">The player whose deck should be filtered.</param>
    /// <returns>A list of eligible deck cards.</returns>
    public static List<CardModel> GetTradableDeckCards(Player? player)
    {
        if (player?.Deck?.Cards == null)
        {
            return new List<CardModel>();
        }

        return player.Deck.Cards.Where(CanTradeCard).ToList();
    }

    /// <summary>
    /// Validates whether the offered card counts from both participants form a legal trade.
    /// Players may trade up to <see cref="MaxCardsPerPlayer"/> cards per side (including 5-for-0 or 0-for-5),
    /// provided at least one card is offered across both sides.
    /// </summary>
    /// <param name="initiatorCardCount">Number of cards offered by the trade initiator.</param>
    /// <param name="partnerCardCount">Number of cards offered by the trade partner.</param>
    /// <returns>True if the card counts are within [0, 5] and at least one card is being traded.</returns>
    public static bool IsValidOfferCount(int initiatorCardCount, int partnerCardCount)
    {
        if (initiatorCardCount < 0 || initiatorCardCount > MaxCardsPerPlayer)
        {
            return false;
        }

        if (partnerCardCount < 0 || partnerCardCount > MaxCardsPerPlayer)
        {
            return false;
        }

        return initiatorCardCount + partnerCardCount > 0;
    }

    /// <summary>
    /// Formats a human-readable display title for a <see cref="CardModel"/>, including upgrade and enchantment info.
    /// </summary>
    /// <param name="card">The card model to format.</param>
    /// <returns>A formatted card title string.</returns>
    public static string FormatCardLabel(CardModel card)
    {
        string title = card.Title;
        if (card.Enchantment != null)
        {
            return $"{title} [{card.Enchantment.Title.GetFormattedText()}]";
        }

        return title;
    }

    /// <summary>
    /// Materializes a temporary preview <see cref="CardModel"/> from a <see cref="SerializableCard"/>
    /// without registering it into <see cref="RunState"/>.
    /// </summary>
    /// <param name="serializableCard">The serialized card payload.</param>
    /// <returns>A mutable preview card model reflecting upgrades, enchantments, and saved properties.</returns>
    public static CardModel CreatePreviewCard(SerializableCard serializableCard)
    {
        return CardModel.FromSerializable(serializableCard);
    }
}
