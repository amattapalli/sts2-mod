using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Usurer.UsurerCode.Util;

namespace Usurer.UsurerCode.Powers;

/// <summary>
/// Lien — Financial collateral debuff on an enemy.
/// Whenever the enemy is hit by a powered Attack, it takes <see cref="PowerModel.Amount"/> unpowered damage
/// and the attacker Repays 1 Debt.
/// </summary>
public sealed class LienPower : UsurerPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || !props.IsPoweredAttack() || Amount <= 0)
            return;

        Flash();

        if (Owner.IsAlive)
        {
            await CreatureCmd.Damage(
                choiceContext,
                Owner,
                Amount,
                ValueProp.Unpowered,
                dealer,
                null);
        }

        if (dealer is { IsAlive: true })
        {
            await DebtEngine.RepayDebt(choiceContext, dealer, 1);
        }
    }
}
