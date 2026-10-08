using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using Usurer.UsurerCode.Cards;
using Usurer.UsurerCode.Powers;

namespace Usurer.UsurerCode.Util;

/// <summary>
/// Centralized factory for building hover tooltips across Usurer cards, relics, and powers
/// by inspecting their localized descriptions for highlighted financial keywords.
/// </summary>
public static class UsurerHoverTips
{
    private const string StaticHoverTipsTable = "static_hover_tips";

    /// <summary>
    /// Creates a static <see cref="IHoverTip"/> backed by <c>static_hover_tips.json</c>.
    /// </summary>
    public static IHoverTip GetStaticHoverTip(string locEntry)
    {
        return new HoverTip(
            new LocString(StaticHoverTipsTable, $"{locEntry}.title"),
            new LocString(StaticHoverTipsTable, $"{locEntry}.description"));
    }

    /// <summary>
    /// Returns all hover tips referenced in a card's localized description.
    /// </summary>
    public static IEnumerable<IHoverTip> ForCard(string entryKey) =>
        BuildTipsForLocTable("cards", entryKey, excludePowerType: null);

    /// <summary>
    /// Returns all hover tips referenced in a relic's localized description.
    /// </summary>
    public static IEnumerable<IHoverTip> ForRelic(string entryKey) =>
        BuildTipsForLocTable("relics", entryKey, excludePowerType: null);

    /// <summary>
    /// Returns all hover tips referenced in a power's localized description, excluding the power itself.
    /// </summary>
    public static IEnumerable<IHoverTip> ForPower(string entryKey, Type selfPowerType) =>
        BuildTipsForLocTable("powers", entryKey, excludePowerType: selfPowerType);

    private static IEnumerable<IHoverTip> BuildTipsForLocTable(
        string locTable,
        string entryKey,
        Type? excludePowerType)
    {
        string raw = LocString.GetIfExists(locTable, $"{entryKey}.description")?.GetRawText() ?? string.Empty;
        if (string.IsNullOrEmpty(raw))
            yield break;

        bool hasBorrow = ContainsKeyword(raw, "Borrow") || ContainsKeyword(raw, "Borrows");
        bool hasRepay = ContainsKeyword(raw, "Repay") || ContainsKeyword(raw, "Repays") || ContainsKeyword(raw, "Repaid");
        bool hasDebt = ContainsKeyword(raw, "Debt");
        bool hasOverLeveraged = ContainsKeyword(raw, "Over-Leveraged");
        bool hasLien = ContainsKeyword(raw, "Lien") || ContainsKeyword(raw, "Liens");
        bool hasForeclose = ContainsKeyword(raw, "Foreclose") || ContainsKeyword(raw, "Forecloses");
        bool hasMoratorium = ContainsKeyword(raw, "Moratorium");

        if (hasBorrow)
            yield return GetStaticHoverTip("USURER-BORROW");

        if (hasRepay)
            yield return GetStaticHoverTip("USURER-REPAY");

        if ((hasDebt || hasBorrow || hasRepay || hasOverLeveraged) && excludePowerType != typeof(DebtPower))
            yield return HoverTipFactory.FromPower<DebtPower>();

        if (hasOverLeveraged)
            yield return GetStaticHoverTip("USURER-OVER_LEVERAGED");

        if (hasForeclose)
            yield return GetStaticHoverTip("USURER-FORECLOSE");

        if ((hasLien || hasForeclose) && excludePowerType != typeof(LienPower))
            yield return HoverTipFactory.FromPower<LienPower>();

        if (hasMoratorium && excludePowerType != typeof(MoratoriumPower))
            yield return HoverTipFactory.FromPower<MoratoriumPower>();

        if (ContainsKeyword(raw, "Artifact"))
            yield return HoverTipFactory.FromPower<ArtifactPower>();

        if (ContainsKeyword(raw, "Weak"))
            yield return HoverTipFactory.FromPower<WeakPower>();

        if (ContainsKeyword(raw, "Vulnerable"))
            yield return HoverTipFactory.FromPower<VulnerablePower>();

        if (ContainsKeyword(raw, "Blood Clause"))
            yield return HoverTipFactory.FromCard<BloodClauseToken>();

        if (ContainsKeyword(raw, "Gold Clause"))
            yield return HoverTipFactory.FromCard<GoldClauseToken>();

        if (ContainsKeyword(raw, "Soul Clause"))
            yield return HoverTipFactory.FromCard<SoulClauseToken>();
    }

    private static bool ContainsKeyword(string rawText, string keyword) =>
        rawText.Contains($"[gold]{keyword}[/gold]", StringComparison.Ordinal);
}
