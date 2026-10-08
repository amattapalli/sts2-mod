# The Usurer — Character, Mechanics & Monte Carlo Balance Design

## 1. Character Identity & Core Loop
- **Name:** The Usurer (`USURER-USURER`)
- **Theme:** An infernal syndicate moneylender in crimson velvet and gold coin-mail who weaponizes compound interest, collateral liens, and debt foreclosure.
- **Starting HP:** `75`
- **Starting Deck (10 Cards):** `4x Strike`, `4x Defend`, `1x Predatory Loan`, `1x Audit`
- **Starter Relic:** `Infernal Ledger`

### Core Financial Mechanics (`DebtEngine.cs`)
1. **Debt (`DebtPower`):**
   - Neutral financial leverage counter on the player (implemented as `PowerType.Buff` so player `Artifact` is never wasted blocking `Borrow`).
   - **Interest & Installments:** At the end of the player's turn (`BeforeSideTurnEnd`), `Debt` increases by **+20% Interest** (`ceil(Debt * 0.20)`). Then, if you are **Over-Leveraged** (`10+` `Debt`), you pay an **Installment** of `Debt / 5` unpowered damage (absorbable by `Block`).
2. **Borrow & Repay:**
   - **Borrow X:** Gain `X` `Debt` to play undercosted tempo cards or activate **Over-Leveraged** (`10+` `Debt`) payoffs.
   - **Repay X:** Remove up to `X` `Debt`. Many defensive and scaling cards scale with the exact amount of `Debt` repaid.
3. **Lien (`LienPower`) & Foreclose:**
   - **Lien:** Collateral debuff on an enemy. Whenever an enemy with `Lien` is hit by a powered Attack, it takes `Lien` unpowered damage and the attacker **Repays 1 Debt**.
   - **Foreclose:** Consumes all `Lien` stacks on an enemy to deal massive burst damage per stack and **Repay 1 Debt** per stack.
4. **Moratorium (`MoratoriumPower`):**
   - Pauses turn-end `Debt` Interest (`+20%`) and Installment damage for `1` turn per stack.

---

## 2. Relics (3)
| Relic | Rarity | Effect |
| :--- | :--- | :--- |
| **Infernal Ledger** (`InfernalLedger`) | Starter | At the start of combat, **Borrow 5 Debt**. The first time each combat you become **Over-Leveraged** (`10+` `Debt`) or reduce your `Debt` to `0`, gain `1 Energy` and draw `1` card. |
| **Abacus of Greed** (`AbacusOfGreed`) | Uncommon | Whenever an enemy dies while it has **Lien**, transfer its **Lien** to a random living enemy and **Repay 3 Debt**. |
| **Blood Signet** (`BloodSignet`) | Rare | At the start of your turn, if you are **Over-Leveraged** (`10+` `Debt`), gain `3 Block` and apply `2 Lien` to ALL enemies. |

---

## 3. Card Pool (31 Cards)

### Basic (4)
- **Strike** (`StrikeUsurer`, 1E Attack): Deal `6 (9)` damage.
- **Defend** (`DefendUsurer`, 1E Skill): Gain `5 (8)` Block.
- **Predatory Loan** (`PredatoryLoan`, 1E Attack): Deal `7 (10)` damage. Apply `2 (3)` **Lien**. **Borrow** `5` **Debt**.
- **Audit** (`Audit`, 1E Skill): Gain `5 (8)` Block. **Repay** `5` **Debt** and gain `1` additional Block for each **Debt** repaid.

### Common (10)
- **Coin Toss** (`CoinToss`, 0E Attack): Deal `4 (6)` damage. Apply `2 (3)` **Lien**. **Borrow** `2` **Debt**.
- **Easy Credit** (`EasyCredit`, 0E Skill): Gain `1 Energy`. Draw `1 (2)` card(s). **Borrow** `5` **Debt**.
- **Bailiff Strike** (`BailiffStrike`, 1E Attack): Deal `5 (7)` damage twice. Apply `1` **Lien** on each hit.
- **Ledger Slam** (`LedgerSlam`, 1E Attack): Deal `7 (10)` damage. If you are **Over-Leveraged**, deal `6 (8)` additional damage.
- **Collateral Shield** (`CollateralShield`, 1E Skill): Gain `6 (8)` Block, plus additional Block equal to the target's **Lien**. Apply `2 (3)` **Lien**.
- **Debt Collection** (`DebtCollection`, 1E Attack): Deal `8 (11)` damage. **Repay** `4 (6)` **Debt**.
- **Cook the Books** (`CookTheBooks`, 1E Skill): Gain `6 (9)` Block. Apply `1` **Moratorium**.
- **Payday** (`Payday`, 1E Skill): Apply `3 (4)` **Lien**. Draw `2` cards.
- **Amortize** (`Amortize`, 1E Skill): **Repay** `6 (8)` **Debt**. Gain `4 (6)` Block. Draw `1` card.
- **Tax Sweep** (`TaxSweep`, 1E Attack, AoE): Deal `6 (9)` damage to ALL enemies. Apply `2 (3)` **Lien** to ALL enemies.

### Uncommon (9)
- **Foreclose** (`Foreclose`, 1E Attack): Deal `8 (11)` damage. **Foreclose**: Consume all **Lien** on the enemy to deal `3 (4)` damage and **Repay** `1` **Debt** per stack.
- **Debt Restructuring** (`DebtRestructuring`, 1E Skill): **Repay** `7 (10)` **Debt**. Apply `1 (2)` **Moratorium**.
- **Double Entry** (`DoubleEntry`, 1E Skill): Double (`Triple`) the target's **Lien**. **Borrow** `4` **Debt**.
- **Bailout** (`Bailout`, 1E Skill, Exhaust): **Repay** ALL of your **Debt**. Gain `1 (2)` Block for each **Debt** repaid.
- **Hostile Takeover** (`HostileTakeover`, 2E Attack): Destroy the target's Block. Deal `12 (16)` damage. Apply `4 (6)` **Lien** and **Borrow** `4` **Debt**.
- **Compound Interest** (`CompoundInterest`, 1E Power): At the start of your turn, if you have any **Debt**, apply `2 (3)` **Lien** to ALL enemies.
- **Shadow Banking** (`ShadowBanking`, 1E Power): Whenever you **Repay** any **Debt**, gain `3 (4)` Block.
- **Garnish Wages** (`GarnishWages`, 1E Skill): Gain `6 (8)` Block. Whenever you apply **Lien** this turn, gain `3 (4)` Block.
- **Debtor's Prison** (`DebtorsPrison`, 1E Power): At the start of your turn, apply `1 (2)` **Weak** to ALL enemies that have **Lien**.

### Rare (5)
- **Leveraged Buyout** (`LeveragedBuyout`, 2E Attack): Deal `14 (18)` damage, plus `2 (3)` additional damage for each **Debt** you have.
- **Sovereign Default** (`SovereignDefault`, `2 (1)`E Power): Prevent turn-end **Debt** Installment self-damage. Whenever you are **Over-Leveraged** at the end of your turn, deal `ceil(Debt / 2)` damage to ALL enemies.
- **Syndicated Loan** (`SyndicatedLoan`, 2E Attack, AoE): Deal `12 (16)` damage to ALL enemies. Remove all **Artifact** and **Block** from ALL enemies, then apply `4 (6)` **Lien** to ALL enemies. **Borrow** `6` **Debt**.
- **Infernal Contract** (`InfernalContract`, 1E Skill): Choose 1 of 3 generated 0-cost cards to add to your Hand: **Blood Clause**, **Gold Clause**, or **Soul Clause** (upgraded if this card is upgraded).
- **Golden Scales** (`GoldenScales`, 1E Power): Whenever you **Borrow** **Debt**, apply `2 (3)` **Lien** to a random enemy.

### Tokens (3)
- **Blood Clause** (`BloodClauseToken`, 0E Skill, Exhaust): Gain `2 (3) Energy`, draw `2` cards, and **Borrow** `6` **Debt**.
- **Gold Clause** (`GoldClauseToken`, 0E Skill, Exhaust): **Repay** `10 (14)` **Debt**, gain `10 (14)` Block, and gain `10 (15)` Gold.
- **Soul Clause** (`SoulClauseToken`, 0E Skill, Exhaust): Apply `6 (9)` **Lien** and `2 (3)` **Vulnerable**.

---

## 4. Monte Carlo Combat Simulation Results (10,000 Trials / Scenario)

| Scenario | T1 Relic Trigger % | T1–T2 Relic Trigger % | Avg Damage / Turn | Avg Block / Turn | Value / Energy (DPE+BPE) | Avg Peak Debt | Avg Unblocked Self-Dmg |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| **A1: Starter Deck (Naive 0 Starting Debt)** | `22.39%` | `72.45%` | `20.41` | `5.58` | `7.92` | `5.38` | `0.00` |
| **A2: Starter Deck (Tuned 5 Starting Debt via `InfernalLedger`)** | **`77.90%`** | **`100.00%`** | `19.04` | **`7.23`** | `7.88` | `9.07` | `0.01` |
| **B1: End of Act 1 Deck (15c, 1 Enemy)** | `75.79%` | `94.09%` | `25.26` | `8.32` | `10.09` | `12.28` | `0.26` |
| **B2: End of Act 1 Deck (15c, 2 Enemies)** | `82.73%` | `97.94%` | `28.50` | `7.23` | `10.72` | `16.47` | `0.83` |
| **C1: Late-Game Scaling Deck (20c, 5 Turns)** | `60.45%` | `88.27%` | `29.28` | `8.65` | `11.85` | `13.61` | `0.04` |

### Key Balance Takeaways
- **Starter Relic Tuning (`InfernalLedger`):** Starting combat with `5 Debt` raises Turn 1 `InfernalLedger` activation from `22.39%` to **`77.90%`** (`100.00%` by Turn 2) because drawing *either* `Predatory Loan` (`5 + 5 = 10 Debt` -> Over-Leveraged) *or* `Audit` (`5 - 5 = 0 Debt` -> Settled) immediately triggers `+1 Energy` and `Draw 1`, while boosting Turn 1 `Audit` to `10 Block` (`5 base + 5 repaid`).
- **Act 1 & Late-Game Scaling:** Value per Energy scales smoothly from `7.88` (Starter) to `10.09–10.72` (Act 1) and `11.85` (Late-Game with `GoldenScales`, `CompoundInterest`, `ShadowBanking`, and `SovereignDefault`), matching *Slay the Spire 2* base-character curves while keeping unblocked self-damage below `1 HP` per combat.
