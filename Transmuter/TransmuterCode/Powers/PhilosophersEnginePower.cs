using MegaCrit.Sts2.Core.Entities.Powers;

namespace Transmuter.TransmuterCode.Powers;

/// <summary>
/// Philosopher's Engine — Your Reactions trigger <see cref="PowerModel.Amount"/> additional time(s).
/// </summary>
public sealed class PhilosophersEnginePower : TransmuterPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;
}
