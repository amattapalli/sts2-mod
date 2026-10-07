using MegaCrit.Sts2.Core.Entities.Powers;

namespace Transmuter.TransmuterCode.Powers;

/// <summary>
/// Emerald Tablet — Whenever you apply a Reagent, apply +<see cref="PowerModel.Amount"/> additional stacks.
/// </summary>
public sealed class EmeraldTabletPower : TransmuterPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;
}
