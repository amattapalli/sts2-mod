using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Transmuter.Tests;

public class UsurerLocalizationAndSourceAuditTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static string LocDir =>
        Path.Combine(RepoRoot, "Usurer", "Usurer", "localization", "eng");

    private static string ImagesDir =>
        Path.Combine(RepoRoot, "Usurer", "Usurer", "images");

    private static string CodeDir =>
        Path.Combine(RepoRoot, "Usurer", "UsurerCode");

    private static Dictionary<string, string> ParseJsonDisallowDuplicates(string filePath)
    {
        Assert.True(File.Exists(filePath), $"Expected localization file at {filePath}");
        byte[] bytes = File.ReadAllBytes(filePath);
        var reader = new Utf8JsonReader(bytes, new JsonReaderOptions
        {
            CommentHandling = JsonCommentHandling.Disallow,
            AllowTrailingCommas = false
        });

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        string? currentKey = null;

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    currentKey = reader.GetString()!;
                    Assert.False(
                        result.ContainsKey(currentKey),
                        $"Duplicate JSON key '{currentKey}' in {Path.GetFileName(filePath)}");
                    break;
                case JsonTokenType.String:
                    if (currentKey != null)
                    {
                        result[currentKey] = reader.GetString()!;
                        currentKey = null;
                    }
                    break;
            }
        }

        return result;
    }

    [Fact]
    public void AllUsurerLocalizationJsonFiles_AreValidJsonWithZeroDuplicateKeys()
    {
        string[] expectedFiles =
        [
            "ancients.json",
            "card_keywords.json",
            "cards.json",
            "characters.json",
            "powers.json",
            "relics.json",
            "static_hover_tips.json"
        ];

        foreach (string fileName in expectedFiles)
        {
            string fullPath = Path.Combine(LocDir, fileName);
            var dict = ParseJsonDisallowDuplicates(fullPath);
            Assert.NotEmpty(dict);
        }
    }

    [Fact]
    public void AncientsAndCharactersJson_ContainAllRequiredUsurerKeys()
    {
        var ancients = ParseJsonDisallowDuplicates(Path.Combine(LocDir, "ancients.json"));
        string[] requiredAncientKeys =
        [
            "THE_ARCHITECT.talk.USURER-USURER.0-0r.char",
            "THE_ARCHITECT.talk.USURER-USURER.0-0r.next",
            "THE_ARCHITECT.talk.USURER-USURER.0-1r.ancient",
            "THE_ARCHITECT.talk.USURER-USURER.0-attack"
        ];

        foreach (string key in requiredAncientKeys)
        {
            Assert.True(ancients.TryGetValue(key, out string? val) && !string.IsNullOrWhiteSpace(val),
                $"Missing or empty mandatory Architect dialogue key '{key}' in Usurer ancients.json");
        }

        var chars = ParseJsonDisallowDuplicates(Path.Combine(LocDir, "characters.json"));
        string[] requiredSuffixes =
        [
            "title",
            "titleObject",
            "description",
            "pronounObject",
            "pronounSubject",
            "pronounPossessive",
            "possessiveAdjective",
            "goldMonologue",
            "eventDeathPrevention",
            "aromaPrinciple",
            "cardsModifierTitle",
            "cardsModifierDescription",
            "banter.alive.endTurnPing",
            "banter.dead.endTurnPing"
        ];

        foreach (string suffix in requiredSuffixes)
        {
            string key = $"USURER-USURER.{suffix}";
            Assert.True(chars.TryGetValue(key, out string? val) && !string.IsNullOrWhiteSpace(val),
                $"Missing or empty character localization key '{key}'");
        }
    }

    [Fact]
    public void UsurerCardsJson_ContainsAll31ExpectedCards()
    {
        var cards = ParseJsonDisallowDuplicates(Path.Combine(LocDir, "cards.json"));

        string[] expectedSlugs =
        [
            // Basic (4)
            "STRIKE_USURER", "DEFEND_USURER", "PREDATORY_LOAN", "AUDIT",
            // Common (10)
            "COIN_TOSS", "EASY_CREDIT", "BAILIFF_STRIKE", "LEDGER_SLAM", "COLLATERAL_SHIELD",
            "DEBT_COLLECTION", "COOK_THE_BOOKS", "PAYDAY", "AMORTIZE", "TAX_SWEEP",
            // Uncommon (9)
            "FORECLOSE", "DEBT_RESTRUCTURING", "DOUBLE_ENTRY", "BAILOUT", "HOSTILE_TAKEOVER",
            "COMPOUND_INTEREST", "SHADOW_BANKING", "GARNISH_WAGES", "DEBTORS_PRISON",
            // Rare (5)
            "LEVERAGED_BUYOUT", "SOVEREIGN_DEFAULT", "SYNDICATED_LOAN", "INFERNAL_CONTRACT", "GOLDEN_SCALES",
            // Token (3)
            "BLOOD_CLAUSE_TOKEN", "GOLD_CLAUSE_TOKEN", "SOUL_CLAUSE_TOKEN"
        ];

        Assert.Equal(31, expectedSlugs.Length);

        foreach (string slug in expectedSlugs)
        {
            string titleKey = $"USURER-{slug}.title";
            string descKey = $"USURER-{slug}.description";

            Assert.True(cards.TryGetValue(titleKey, out string? title) && !string.IsNullOrWhiteSpace(title),
                $"Missing or empty '{titleKey}' in Usurer cards.json");
            Assert.True(cards.TryGetValue(descKey, out string? desc) && !string.IsNullOrWhiteSpace(desc),
                $"Missing or empty '{descKey}' in Usurer cards.json");
        }
    }

    [Fact]
    public void UsurerPowersRelicsAndHoverTips_AreCompleteAndWired()
    {
        var powers = ParseJsonDisallowDuplicates(Path.Combine(LocDir, "powers.json"));
        string[] expectedPowers =
        [
            "DEBT_POWER", "LIEN_POWER", "MORATORIUM_POWER",
            "COMPOUND_INTEREST_POWER", "SHADOW_BANKING_POWER", "GARNISH_WAGES_POWER",
            "DEBTORS_PRISON_POWER", "SOVEREIGN_DEFAULT_POWER", "GOLDEN_SCALES_POWER"
        ];

        foreach (string power in expectedPowers)
        {
            Assert.True(powers.ContainsKey($"USURER-{power}.title"), $"Missing USURER-{power}.title");
            Assert.True(powers.ContainsKey($"USURER-{power}.description"), $"Missing USURER-{power}.description");
            Assert.True(powers.ContainsKey($"USURER-{power}.smartDescription"), $"Missing USURER-{power}.smartDescription");
        }

        var relics = ParseJsonDisallowDuplicates(Path.Combine(LocDir, "relics.json"));
        string[] expectedRelics = ["INFERNAL_LEDGER", "ABACUS_OF_GREED", "BLOOD_SIGNET"];
        foreach (string relic in expectedRelics)
        {
            Assert.True(relics.ContainsKey($"USURER-{relic}.title"), $"Missing USURER-{relic}.title");
            Assert.True(relics.ContainsKey($"USURER-{relic}.description"), $"Missing USURER-{relic}.description");
            Assert.True(relics.ContainsKey($"USURER-{relic}.flavor"), $"Missing USURER-{relic}.flavor");
        }

        var hoverTips = ParseJsonDisallowDuplicates(Path.Combine(LocDir, "static_hover_tips.json"));
        string[] expectedTips =
        [
            "DEBT", "BORROW", "REPAY", "OVER_LEVERAGED", "LIEN", "FORECLOSE", "MORATORIUM"
        ];
        foreach (string tip in expectedTips)
        {
            Assert.True(hoverTips.ContainsKey($"USURER-{tip}.title"), $"Missing USURER-{tip}.title");
            Assert.True(hoverTips.ContainsKey($"USURER-{tip}.description"), $"Missing USURER-{tip}.description");
        }

        Assert.Contains("ExtraHoverTips", File.ReadAllText(Path.Combine(CodeDir, "Cards", "UsurerCard.cs")));
        Assert.Contains("ExtraHoverTips", File.ReadAllText(Path.Combine(CodeDir, "Relics", "UsurerRelic.cs")));
        Assert.Contains("ExtraHoverTips", File.ReadAllText(Path.Combine(CodeDir, "Powers", "UsurerPower.cs")));
        Assert.Contains("AfterCombatEnd(CombatRoom room)", File.ReadAllText(Path.Combine(CodeDir, "Relics", "InfernalLedger.cs")));
    }

    [Fact]
    public void EveryConcreteUsurerCSharpClass_HasMatchingLocalizationDynamicVarsAndArtwork()
    {
        var cardsLoc = ParseJsonDisallowDuplicates(Path.Combine(LocDir, "cards.json"));
        var powersLoc = ParseJsonDisallowDuplicates(Path.Combine(LocDir, "powers.json"));
        var relicsLoc = ParseJsonDisallowDuplicates(Path.Combine(LocDir, "relics.json"));

        var classDeclRegex = new Regex(@"public\s+(?:sealed\s+)?class\s+(\w+)\b", RegexOptions.Compiled);
        var placeholderRegex = new Regex(@"\{([A-Za-z0-9_]+)(?::[^{}]*)?\}", RegexOptions.Compiled);
        HashSet<string> builtInPlaceholders = new(StringComparer.Ordinal)
        {
            "IfUpgraded", "InCombat", "energyPrefix", "Amount"
        };

        // Verify character UI artwork exists
        string[] charUiFiles =
        [
            "usurer_combat.png",
            "char_select_usurer.png",
            "char_select_usurer_locked.png",
            "character_icon_usurer.png",
            "map_marker_usurer.png",
            "big_energy.png",
            "text_energy.png"
        ];
        foreach (string uiFile in charUiFiles)
        {
            string uiPath = Path.Combine(ImagesDir, "charui", uiFile);
            Assert.True(File.Exists(uiPath), $"Missing character UI art file: {uiPath}");
        }

        // 1. Audit Cards + Card Portraits
        int concreteCardCount = 0;
        string cardsFolder = Path.Combine(CodeDir, "Cards");
        foreach (string csFile in Directory.EnumerateFiles(cardsFolder, "*.cs", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(csFile);
            if (source.Contains("abstract class"))
                continue;

            var match = classDeclRegex.Match(source);
            if (!match.Success)
                continue;

            concreteCardCount++;
            string className = match.Groups[1].Value;
            string slug = LocalizationAndSourceAuditTests.PascalToUpperSnake(className);
            string lowerSlug = slug.ToLowerInvariant();
            string titleKey = $"USURER-{slug}.title";
            string descKey = $"USURER-{slug}.description";

            Assert.True(cardsLoc.ContainsKey(titleKey),
                $"Card C# class '{className}' is missing '{titleKey}' in cards.json");
            Assert.True(cardsLoc.TryGetValue(descKey, out string? desc),
                $"Card C# class '{className}' is missing '{descKey}' in cards.json");

            Assert.True(File.Exists(Path.Combine(ImagesDir, "card_portraits", $"{lowerSlug}.png")),
                $"Missing small card portrait for '{lowerSlug}.png'");
            Assert.True(File.Exists(Path.Combine(ImagesDir, "card_portraits", "big", $"{lowerSlug}.png")),
                $"Missing big card portrait for '{lowerSlug}.png'");

            foreach (Match ph in placeholderRegex.Matches(desc!))
            {
                string varName = ph.Groups[1].Value;
                if (builtInPlaceholders.Contains(varName))
                    continue;

                bool definedInCode = varName switch
                {
                    "Damage" => source.Contains("DamageVar"),
                    "Block" => source.Contains("BlockVar"),
                    "Cards" => source.Contains("CardsVar") || source.Contains($"\"{varName}\""),
                    "Gold" => source.Contains("GoldVar") || source.Contains($"\"{varName}\""),
                    "Energy" => source.Contains("EnergyVar") || source.Contains($"\"{varName}\""),
                    "Repeat" => source.Contains("RepeatVar") || source.Contains($"\"{varName}\""),
                    _ => source.Contains($"\"{varName}\"") || source.Contains($"PowerVar<{varName}>")
                };

                Assert.True(definedInCode,
                    $"Card '{className}' description references placeholder '{{{varName}}}' in cards.json, but '{Path.GetFileName(csFile)}' does not define a matching DynamicVar.");
            }
        }
        Assert.Equal(31, concreteCardCount);

        // 2. Audit Powers + Power Icons
        int concretePowerCount = 0;
        string powersFolder = Path.Combine(CodeDir, "Powers");
        foreach (string csFile in Directory.EnumerateFiles(powersFolder, "*.cs", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(csFile);
            if (source.Contains("abstract class"))
                continue;

            var match = classDeclRegex.Match(source);
            if (!match.Success)
                continue;

            concretePowerCount++;
            string className = match.Groups[1].Value;
            string slug = LocalizationAndSourceAuditTests.PascalToUpperSnake(className);
            string lowerSlug = slug.ToLowerInvariant();

            Assert.True(powersLoc.ContainsKey($"USURER-{slug}.title"),
                $"Power C# class '{className}' is missing 'USURER-{slug}.title'");
            Assert.True(powersLoc.ContainsKey($"USURER-{slug}.description"),
                $"Power C# class '{className}' is missing 'USURER-{slug}.description'");
            Assert.True(File.Exists(Path.Combine(ImagesDir, "powers", $"{lowerSlug}.png")),
                $"Missing small power icon '{lowerSlug}.png'");
            Assert.True(File.Exists(Path.Combine(ImagesDir, "powers", "big", $"{lowerSlug}.png")),
                $"Missing big power icon '{lowerSlug}.png'");
        }
        Assert.Equal(9, concretePowerCount);

        // 3. Audit Relics + Relic Icons
        int concreteRelicCount = 0;
        string relicsFolder = Path.Combine(CodeDir, "Relics");
        foreach (string csFile in Directory.EnumerateFiles(relicsFolder, "*.cs", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(csFile);
            if (source.Contains("abstract class"))
                continue;

            var match = classDeclRegex.Match(source);
            if (!match.Success)
                continue;

            concreteRelicCount++;
            string className = match.Groups[1].Value;
            string slug = LocalizationAndSourceAuditTests.PascalToUpperSnake(className);
            string lowerSlug = slug.ToLowerInvariant();

            Assert.True(relicsLoc.ContainsKey($"USURER-{slug}.title"),
                $"Relic C# class '{className}' is missing 'USURER-{slug}.title'");
            Assert.True(relicsLoc.ContainsKey($"USURER-{slug}.description"),
                $"Relic C# class '{className}' is missing 'USURER-{slug}.description'");
            Assert.True(File.Exists(Path.Combine(ImagesDir, "relics", $"{lowerSlug}.png")),
                $"Missing small relic icon '{lowerSlug}.png'");
            Assert.True(File.Exists(Path.Combine(ImagesDir, "relics", $"{lowerSlug}_outline.png")),
                $"Missing relic outline icon '{lowerSlug}_outline.png'");
            Assert.True(File.Exists(Path.Combine(ImagesDir, "relics", "big", $"{lowerSlug}.png")),
                $"Missing big relic icon '{lowerSlug}.png'");
        }
        Assert.Equal(3, concreteRelicCount);
    }
}
