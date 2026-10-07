using MegaCrit.Sts2.Core.Entities.Powers;

namespace Transmuter.TransmuterCode.Powers;

/// <summary>
/// Residual Precipitate — Whenever a Reaction triggers on an enemy, reapply
/// <see cref="PowerModel.Amount"/> stacks of the first Reagent consumed.
/// </summary>
public sealed class ResidualPrecipitatePower : TransmuterPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;
}
