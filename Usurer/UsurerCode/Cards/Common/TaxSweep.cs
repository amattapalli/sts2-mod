using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Usurer.UsurerCode.Util;

namespace Usurer.UsurerCode.Cards;

/// <summary>
/// Tax Sweep — Common Attack, cost 1, AllEnemies.
/// Deal 6 (9) damage to ALL enemies. Apply 2 (3) Lien to ALL enemies.
/// </summary>
public sealed class TaxSweep() : UsurerCard(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move),
        new DynamicVar("Lien", 2m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);

        if (CombatState == null)
            return;

        foreach (Creature enemy in CombatState.HittableEnemies.ToList())
        {
            if (enemy.IsAlive)
            {
                await DebtEngine.ApplyLien(
                    choiceContext,
                    enemy,
                    Owner.Creature,
                    DynamicVars["Lien"].IntValue);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars["Lien"].UpgradeValueBy(1m);
    }
}
