using MegaCrit.Sts2.Core.Entities.Powers;

namespace Usurer.UsurerCode.Powers;

/// <summary>
/// Moratorium — Pauses turn-end Debt Interest and Installment damage for <see cref="MegaCrit.Sts2.Core.Models.Powers.PowerModel.Amount"/> turns.
/// Decrements by 1 at the end of the player's turn via <see cref="Util.DebtEngine.ResolveTurnEndDebt"/>.
/// </summary>
public sealed class MoratoriumPower : UsurerPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;
}
