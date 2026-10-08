using MegaCrit.Sts2.Core.Entities.Powers;

namespace Usurer.UsurerCode.Powers;

/// <summary>
/// Shadow Banking — Whenever you Repay any Debt, gain <see cref="MegaCrit.Sts2.Core.Models.Powers.PowerModel.Amount"/> Block.
/// Triggered directly by <see cref="Util.DebtEngine.RepayDebt"/>.
/// </summary>
public sealed class ShadowBankingPower : UsurerPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;
}
