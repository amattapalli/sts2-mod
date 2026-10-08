using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Usurer.UsurerCode.Util;

namespace Usurer.UsurerCode.Cards;

/// <summary>
/// Syndicated Loan — Rare Attack, cost 2, AllEnemies.
/// Deal 12 (16) damage to ALL enemies. Remove all Artifact and Block from ALL enemies,
/// then apply 4 (6) Lien to ALL enemies. Borrow 6 Debt.
/// </summary>
public sealed class SyndicatedLoan() : UsurerCard(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(12m, ValueProp.Move),
        new DynamicVar("Lien", 4m),
        new DynamicVar("Borrow", 6m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);

        if (CombatState != null)
        {
            int lienAmount = DynamicVars["Lien"].IntValue;

            foreach (Creature enemy in CombatState.HittableEnemies.ToList())
            {
                if (!enemy.IsAlive)
                    continue;

                if (enemy.Block > 0)
                {
                    await CreatureCmd.Damage(
                        choiceContext,
                        enemy,
                        enemy.Block,
                        ValueProp.Unpowered,
                        Owner.Creature,
                        this);
                }

                ArtifactPower? artifact = enemy.GetPower<ArtifactPower>();
                if (artifact != null)
                {
                    await PowerCmd.Remove(artifact);
                }

                if (enemy.IsAlive)
                {
                    await DebtEngine.ApplyLien(
                        choiceContext,
                        enemy,
                        Owner.Creature,
                        lienAmount);
                }
            }
        }

        await DebtEngine.BorrowDebt(choiceContext, Owner.Creature, DynamicVars["Borrow"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
        DynamicVars["Lien"].UpgradeValueBy(2m);
    }
}
