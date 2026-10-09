using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Transmuter.Tests;

public class LocalizationAndSourceAuditTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static string LocDir =>
        Path.Combine(RepoRoot, "Transmuter", "Transmuter", "localization", "eng");

    private static string CodeDir =>
        Path.Combine(RepoRoot, "Transmuter", "TransmuterCode");

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

    public static string PascalToUpperSnake(string pascal)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < pascal.Length; i++)
        {
            char c = pascal[i];
            if (i > 0 && char.IsUpper(c) &&
                (!char.IsUpper(pascal[i - 1]) || (i + 1 < pascal.Length && char.IsLower(pascal[i + 1]))))
            {
                sb.Append('_');
            }
            sb.Append(char.ToUpperInvariant(c));
        }
        return sb.ToString();
    }

    [Fact]
    public void AllLocalizationJsonFiles_AreValidJsonWithZeroDuplicateKeys()
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
    public void AncientsJson_ContainsMandatoryArchitectDialogueForTransmuter()
    {
        var ancients = ParseJsonDisallowDuplicates(Path.Combine(LocDir, "ancients.json"));

        string[] requiredKeys =
        [
            "THE_ARCHITECT.talk.TRANSMUTER-TRANSMUTER.0-0r.char",
            "THE_ARCHITECT.talk.TRANSMUTER-TRANSMUTER.0-0r.next",
            "THE_ARCHITECT.talk.TRANSMUTER-TRANSMUTER.0-1r.ancient",
            "THE_ARCHITECT.talk.TRANSMUTER-TRANSMUTER.0-attack"
        ];

        foreach (string key in requiredKeys)
        {
            Assert.True(ancients.TryGetValue(key, out string? val) && !string.IsNullOrWhiteSpace(val),
                $"Missing or empty mandatory Architect dialogue key '{key}' in ancients.json");
        }
    }

    [Fact]
    public void CharactersJson_ContainsAllRequiredTransmuterKeys()
    {
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
            string key = $"TRANSMUTER-TRANSMUTER.{suffix}";
            Assert.True(chars.TryGetValue(key, out string? val) && !string.IsNullOrWhiteSpace(val),
                $"Missing or empty character localization key '{key}'");
        }
    }

    [Fact]
    public void CardsJson_ContainsAll31ExpectedCards()
    {
        var cards = ParseJsonDisallowDuplicates(Path.Combine(LocDir, "cards.json"));

        string[] expectedSlugs =
        [
            // Basic (4)
            "STRIKE_TRANSMUTER", "DEFEND_TRANSMUTER", "BRIMSTONE_TOSS", "SALINE_SOLUTION",
            // Common (10)
            "CINNABAR_SLASH", "HALITE_BASH", "CALCINATION", "QUICK_PRIMER", "SALT_WARD",
            "FUME_CLOUD", "GLASS_VIAL", "DISTILL", "SPLASH_ACID", "HERMETIC_SEAL",
            // Uncommon (9)
            "CHAIN_DETONATION", "SUBLIMATE", "QUICKSILVER_NEEDLE", "LEAD_TO_GOLD", "VOLATILE_FLASK",
            "ATHANOR_FURNACE", "RESIDUAL_PRECIPITATE", "SYMPATHETIC_TINCTURE", "CRUCIBLE_SHIELD",
            // Rare (5)
            "THE_MAGNUM_OPUS", "PHILOSOPHERS_ENGINE", "UNIVERSAL_SOLVENT", "NIGREDO_ALBEDO_RUBEDO", "EMERALD_TABLET",
            // Token (3)
            "NIGREDO_TOKEN", "ALBEDO_TOKEN", "RUBEDO_TOKEN"
        ];

        Assert.Equal(31, expectedSlugs.Length);

        foreach (string slug in expectedSlugs)
        {
            string titleKey = $"TRANSMUTER-{slug}.title";
            string descKey = $"TRANSMUTER-{slug}.description";

            Assert.True(cards.TryGetValue(titleKey, out string? title) && !string.IsNullOrWhiteSpace(title),
                $"Missing or empty '{titleKey}' in cards.json");
            Assert.True(cards.TryGetValue(descKey, out string? desc) && !string.IsNullOrWhiteSpace(desc),
                $"Missing or empty '{descKey}' in cards.json");
        }
    }

    [Fact]
    public void PowersAndRelicsJson_ContainAllExpectedEntries()
    {
        var powers = ParseJsonDisallowDuplicates(Path.Combine(LocDir, "powers.json"));
        string[] expectedPowers =
        [
            "SULFUR_POWER", "MERCURY_POWER", "SALT_POWER", "STABILIZED_POWER",
            "ATHANOR_FURNACE_POWER", "RESIDUAL_PRECIPITATE_POWER", "PHILOSOPHERS_ENGINE_POWER",
            "EMERALD_TABLET_POWER", "CHAIN_DETONATION_POWER", "CRUCIBLE_SHIELD_POWER"
        ];

        foreach (string power in expectedPowers)
        {
            Assert.True(powers.ContainsKey($"TRANSMUTER-{power}.title"), $"Missing TRANSMUTER-{power}.title");
            Assert.True(powers.ContainsKey($"TRANSMUTER-{power}.description"), $"Missing TRANSMUTER-{power}.description");
            Assert.True(powers.ContainsKey($"TRANSMUTER-{power}.smartDescription"), $"Missing TRANSMUTER-{power}.smartDescription");
        }

        var relics = ParseJsonDisallowDuplicates(Path.Combine(LocDir, "relics.json"));
        string[] expectedRelics = ["CRACKED_ALEMBIC", "OUROBOROS_RING", "PARACELSUS_SCALPEL"];
        foreach (string relic in expectedRelics)
        {
            Assert.True(relics.ContainsKey($"TRANSMUTER-{relic}.title"), $"Missing TRANSMUTER-{relic}.title");
            Assert.True(relics.ContainsKey($"TRANSMUTER-{relic}.description"), $"Missing TRANSMUTER-{relic}.description");
            Assert.True(relics.ContainsKey($"TRANSMUTER-{relic}.flavor"), $"Missing TRANSMUTER-{relic}.flavor");
        }
    }

    [Fact]
    public void StaticHoverTipsAndBaseModels_AreCompleteAndWired()
    {
        var hoverTips = ParseJsonDisallowDuplicates(Path.Combine(LocDir, "static_hover_tips.json"));
        string[] expectedTipKeys =
        [
            "SULFUR", "MERCURY", "SALT", "STABILIZE",
            "REAGENT", "REACTION", "DETONATE", "CALCIFY", "DISSOLVE", "MAGNUM_OPUS"
        ];

        foreach (string tip in expectedTipKeys)
        {
            Assert.True(hoverTips.ContainsKey($"TRANSMUTER-{tip}.title"), $"Missing TRANSMUTER-{tip}.title in static_hover_tips.json");
            Assert.True(hoverTips.ContainsKey($"TRANSMUTER-{tip}.description"), $"Missing TRANSMUTER-{tip}.description in static_hover_tips.json");
        }

        Assert.Contains("ExtraHoverTips", File.ReadAllText(Path.Combine(CodeDir, "Cards", "TransmuterCard.cs")));
        Assert.Contains("BeforeCardPlayed(CardPlay cardPlay)", File.ReadAllText(Path.Combine(CodeDir, "Cards", "TransmuterCard.cs")));
        Assert.Contains("ExtraHoverTips", File.ReadAllText(Path.Combine(CodeDir, "Relics", "TransmuterRelic.cs")));
        Assert.Contains("ExtraHoverTips", File.ReadAllText(Path.Combine(CodeDir, "Powers", "TransmuterPower.cs")));
        Assert.Contains("AttachCombatAnimations", File.ReadAllText(Path.Combine(CodeDir, "Character", "Transmuter.cs")));
        Assert.Contains("ApplyCustomCharacterSelectBackground", File.ReadAllText(Path.Combine(CodeDir, "Character", "TransmuterVisualsAndUiPatch.cs")));

        string charUiDir = Path.Combine(RepoRoot, "Transmuter", "Transmuter", "images", "charui");
        AssertPngDimensions(Path.Combine(charUiDir, "char_select_transmuter.png"), 132, 195);
        AssertPngDimensions(Path.Combine(charUiDir, "char_select_transmuter_locked.png"), 132, 195);
        AssertPngDimensions(Path.Combine(charUiDir, "char_select_bg_transmuter.png"), 1920, 1080);
        AssertPngDimensions(Path.Combine(charUiDir, "transmuter_combat.png"), 260, 320);
    }

    internal static void AssertPngDimensions(string pngPath, int expectedWidth, int expectedHeight)
    {
        Assert.True(File.Exists(pngPath), $"Expected PNG file at {pngPath}");
        byte[] bytes = File.ReadAllBytes(pngPath);
        Assert.True(bytes.Length >= 24, $"Invalid PNG file: {pngPath}");
        int width = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
        int height = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
        Assert.Equal(expectedWidth, width);
        Assert.Equal(expectedHeight, height);
    }

    [Fact]
    public void EveryConcreteCSharpCardPowerAndRelic_HasMatchingLocalizationAndDynamicVars()
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

        // 1. Audit Cards
        int concreteCardCount = 0;
        string cardsFolder = Path.Combine(CodeDir, "Cards");
        Assert.True(Directory.Exists(cardsFolder), $"Missing {cardsFolder}");
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
            string slug = PascalToUpperSnake(className);
            string titleKey = $"TRANSMUTER-{slug}.title";
            string descKey = $"TRANSMUTER-{slug}.description";

            Assert.True(cardsLoc.ContainsKey(titleKey),
                $"Card C# class '{className}' ({Path.GetFileName(csFile)}) is missing '{titleKey}' in cards.json");
            Assert.True(cardsLoc.TryGetValue(descKey, out string? desc),
                $"Card C# class '{className}' ({Path.GetFileName(csFile)}) is missing '{descKey}' in cards.json");

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

        // 2. Audit Powers
        int concretePowerCount = 0;
        string powersFolder = Path.Combine(CodeDir, "Powers");
        Assert.True(Directory.Exists(powersFolder), $"Missing {powersFolder}");
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
            string slug = PascalToUpperSnake(className);
            Assert.True(powersLoc.ContainsKey($"TRANSMUTER-{slug}.title"),
                $"Power C# class '{className}' is missing 'TRANSMUTER-{slug}.title' in powers.json");
            Assert.True(powersLoc.ContainsKey($"TRANSMUTER-{slug}.description"),
                $"Power C# class '{className}' is missing 'TRANSMUTER-{slug}.description' in powers.json");
        }
        Assert.Equal(10, concretePowerCount);

        // 3. Audit Relics
        int concreteRelicCount = 0;
        string relicsFolder = Path.Combine(CodeDir, "Relics");
        Assert.True(Directory.Exists(relicsFolder), $"Missing {relicsFolder}");
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
            string slug = PascalToUpperSnake(className);
            Assert.True(relicsLoc.ContainsKey($"TRANSMUTER-{slug}.title"),
                $"Relic C# class '{className}' is missing 'TRANSMUTER-{slug}.title' in relics.json");
            Assert.True(relicsLoc.ContainsKey($"TRANSMUTER-{slug}.description"),
                $"Relic C# class '{className}' is missing 'TRANSMUTER-{slug}.description' in relics.json");
        }
        Assert.Equal(3, concreteRelicCount);
    }
}
