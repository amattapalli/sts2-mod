using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using Usurer.UsurerCode.Character;
using Usurer.UsurerCode.Powers;
using Usurer.UsurerCode.Util;

namespace Usurer.UsurerCode.Relics;

/// <summary>
/// Infernal Ledger — Starter Relic for The Usurer.
/// At the start of combat, Borrow 5 Debt (+5 Gold). The first time each combat you reach 10+ Debt
/// (Over-Leveraged) or reduce your Debt to 0, gain 1 Energy and draw 1 card.
/// At the end of combat, any unpaid Debt is settled from your Gold.
/// </summary>
[Pool(typeof(UsurerRelicPool))]
public sealed class InfernalLedger : UsurerRelic
{
    private const int InitialDebtStacks = 5;
    private bool _triggeredThisCombat;

    public override RelicRarity Rarity => RelicRarity.Starter;

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != Owner.Creature.Side || combatState.RoundNumber > 1)
            return;

        _triggeredThisCombat = false;
        DebtEngine.ResetCombatLedger();
        Flash();
        await DebtEngine.BorrowDebt(choiceContext, Owner.Creature, InitialDebtStacks);
    }

    public override async Task AfterCombatEnd(IRunState runState, CombatState? combatState, CombatRoom room)
    {
        await DebtEngine.SettleCombatEndDebt(Owner);
    }

    public async Task OnThresholdOrSettledReached(PlayerChoiceContext choiceContext)
    {
        if (_triggeredThisCombat)
            return;

        _triggeredThisCombat = true;
        Flash();

        await PlayerCmd.GainEnergy(1, Owner);
        await CardPileCmd.Draw(choiceContext, 1, Owner);
    }
}
