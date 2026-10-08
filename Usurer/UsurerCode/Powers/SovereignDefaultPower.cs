using MegaCrit.Sts2.Core.Entities.Powers;

namespace Usurer.UsurerCode.Powers;

/// <summary>
/// Sovereign Default — Prevents turn-end Debt Installment self-damage and instead deals
/// ceil(Debt / 2) * <see cref="MegaCrit.Sts2.Core.Models.Powers.PowerModel.Amount"/> unpowered damage to ALL enemies
/// when Over-Leveraged (10+ Debt) at the end of your turn.
/// </summary>
public sealed class SovereignDefaultPower : UsurerPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;
}
