using MegaCrit.Sts2.Core.Entities.Powers;

namespace Usurer.UsurerCode.Powers;

/// <summary>
/// Golden Scales — Whenever you Borrow Debt, apply <see cref="MegaCrit.Sts2.Core.Models.Powers.PowerModel.Amount"/>
/// Lien to a random living enemy. Triggered directly by <see cref="Util.DebtEngine.BorrowDebt"/>.
/// </summary>
public sealed class GoldenScalesPower : UsurerPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;
}
