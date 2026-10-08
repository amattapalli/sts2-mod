using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using Transmuter.TransmuterCode.Cards;
using Transmuter.TransmuterCode.Powers;

namespace Transmuter.TransmuterCode.Util;

/// <summary>
/// Centralized factory for building hover tooltips across Transmuter cards, relics, and powers
/// by inspecting their localized descriptions for highlighted alchemical keywords.
/// </summary>
public static class TransmuterHoverTips
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
        BuildTipsForLocTable("cards", entryKey, excludePowerType: null, includeReactionSummaryOnReagents: true);

    /// <summary>
    /// Returns all hover tips referenced in a relic's localized description.
    /// </summary>
    public static IEnumerable<IHoverTip> ForRelic(string entryKey) =>
        BuildTipsForLocTable("relics", entryKey, excludePowerType: null, includeReactionSummaryOnReagents: true);

    /// <summary>
    /// Returns all hover tips referenced in a power's localized description, excluding the power itself.
    /// </summary>
    public static IEnumerable<IHoverTip> ForPower(string entryKey, Type selfPowerType) =>
        BuildTipsForLocTable("powers", entryKey, excludePowerType: selfPowerType, includeReactionSummaryOnReagents: false);

    private static IEnumerable<IHoverTip> BuildTipsForLocTable(
        string locTable,
        string entryKey,
        Type? excludePowerType,
        bool includeReactionSummaryOnReagents)
    {
        string raw = LocString.GetIfExists(locTable, $"{entryKey}.description")?.GetRawText() ?? string.Empty;
        if (string.IsNullOrEmpty(raw))
            yield break;

        bool hasReagentWord = ContainsKeyword(raw, "Reagent") || ContainsKeyword(raw, "Reagents");
        bool hasSalt = ContainsKeyword(raw, "Salt");
        bool hasSulfur = ContainsKeyword(raw, "Sulfur");
        bool hasMercury = ContainsKeyword(raw, "Mercury");
        bool hasStabilize = ContainsKeyword(raw, "Stabilize") || ContainsKeyword(raw, "Stabilized");
        bool hasReactionWord = ContainsKeyword(raw, "Reaction") || ContainsKeyword(raw, "Reactions");
        bool hasDetonate = ContainsKeyword(raw, "Detonate");
        bool hasCalcify = ContainsKeyword(raw, "Calcify");
        bool hasDissolve = ContainsKeyword(raw, "Dissolve");
        bool hasMagnumOpus = ContainsKeyword(raw, "Magnum Opus");

        if (hasReagentWord)
            yield return GetStaticHoverTip("TRANSMUTER-REAGENT");

        if (hasSalt && excludePowerType != typeof(SaltPower))
            yield return HoverTipFactory.FromPower<SaltPower>();

        if (hasSulfur && excludePowerType != typeof(SulfurPower))
            yield return HoverTipFactory.FromPower<SulfurPower>();

        if (hasMercury && excludePowerType != typeof(MercuryPower))
            yield return HoverTipFactory.FromPower<MercuryPower>();

        if (hasStabilize && excludePowerType != typeof(StabilizedPower))
            yield return HoverTipFactory.FromPower<StabilizedPower>();

        bool hasSpecificReaction = hasDetonate || hasCalcify || hasDissolve || hasMagnumOpus;
        bool hasAnyReagentReference = hasReagentWord || hasSalt || hasSulfur || hasMercury || hasStabilize;

        if (hasReactionWord || (includeReactionSummaryOnReagents && hasAnyReagentReference && !hasSpecificReaction))
            yield return GetStaticHoverTip("TRANSMUTER-REACTION");

        if (hasDetonate)
            yield return GetStaticHoverTip("TRANSMUTER-DETONATE");

        if (hasCalcify)
            yield return GetStaticHoverTip("TRANSMUTER-CALCIFY");

        if (hasDissolve)
            yield return GetStaticHoverTip("TRANSMUTER-DISSOLVE");

        if (hasMagnumOpus)
            yield return GetStaticHoverTip("TRANSMUTER-MAGNUM_OPUS");

        if (ContainsKeyword(raw, "Artifact"))
            yield return HoverTipFactory.FromPower<ArtifactPower>();

        if (ContainsKeyword(raw, "Weak"))
            yield return HoverTipFactory.FromPower<WeakPower>();

        if (ContainsKeyword(raw, "Vulnerable"))
            yield return HoverTipFactory.FromPower<VulnerablePower>();

        if (ContainsKeyword(raw, "Nigredo"))
            yield return HoverTipFactory.FromCard<NigredoToken>();

        if (ContainsKeyword(raw, "Albedo"))
            yield return HoverTipFactory.FromCard<AlbedoToken>();

        if (ContainsKeyword(raw, "Rubedo"))
            yield return HoverTipFactory.FromCard<RubedoToken>();
    }

    private static bool ContainsKeyword(string rawText, string keyword) =>
        rawText.Contains($"[gold]{keyword}[/gold]", StringComparison.Ordinal);
}
