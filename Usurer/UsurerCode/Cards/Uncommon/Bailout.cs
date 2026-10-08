using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Usurer.UsurerCode.Util;

namespace Usurer.UsurerCode.Cards;

/// <summary>
/// Bailout — Uncommon Skill, cost 1, Self, Exhaust.
/// Repay ALL of your Debt. Gain 1 (2) Block for each Debt repaid.
/// </summary>
public sealed class Bailout() : UsurerCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("BlockPerDebt", 1m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        int currentDebt = DebtEngine.GetDebtAmount(Owner.Creature);
        if (currentDebt <= 0)
            return;

        int repaid = await DebtEngine.RepayDebt(choiceContext, Owner.Creature, currentDebt);
        int totalBlock = repaid * DynamicVars["BlockPerDebt"].IntValue;

        if (totalBlock > 0 && Owner.Creature.IsAlive)
        {
            await CreatureCmd.GainBlock(Owner.Creature, totalBlock, ValueProp.Unpowered, play);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["BlockPerDebt"].UpgradeValueBy(1m);
    }
}
