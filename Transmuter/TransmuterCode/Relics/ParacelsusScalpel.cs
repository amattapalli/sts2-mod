using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;
using Transmuter.TransmuterCode.Character;

namespace Transmuter.TransmuterCode.Relics;

/// <summary>
/// Paracelsus's Scalpel — Rare Relic for The Transmuter.
/// Reactions leave 1 stack of each reacting Reagent behind instead of consuming all stacks.
/// </summary>
[Pool(typeof(TransmuterRelicPool))]
public sealed class ParacelsusScalpel : TransmuterRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;
}
