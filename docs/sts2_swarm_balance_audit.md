# The Transmuter — Swarm Card Design, Simulation & Balance Audit

Three specialist subagents audited the entire 31-card, 10-power, 3-relic implementation in [`Transmuter/`](file:///usr/local/google/home/amattapalli/PersonalProjects/sts2-mod/Transmuter):
- **`balance-earlygame`**: Audited [`ReactionEngine.cs`](file:///usr/local/google/home/amattapalli/PersonalProjects/sts2-mod/Transmuter/TransmuterCode/Util/ReactionEngine.cs), hypergeometric Turn 1–2 starter deck probabilities, [`CrackedAlembic.cs`](file:///usr/local/google/home/amattapalli/PersonalProjects/sts2-mod/Transmuter/TransmuterCode/Relics/CrackedAlembic.cs), and all 14 Basic & Common cards.
- **`balance-midlategame`**: Audited all 17 Uncommon, Rare, and Token cards, 7 engine powers, and Uncommon/Rare relics for infinite loops, degenerate scaling, and `Stabilize` / `Magnum Opus` math.
- **`balance-sim`**: Built and ran a 10,000-trial Monte Carlo 3-turn combat simulator ([`simulate_transmuter.py`](file:///usr/local/google/home/amattapalli/.gemini/jetski/brain/ff4b6a5d-1b99-435a-8377-89190aa94fec/scratch/sim/simulate_transmuter.py)) and cross-checked every C# card/power/relic against the localization JSONs.

---

## 1. 🚨 P0 Bugs & Infinite Loops Found in Code (Must Fix Before Playtesting)

Before tuning numbers, the swarm caught **6 mechanical bugs / infinite loops** in the current C# implementation that would either crash the game or break combat rules:

| # | Location | What Happens Right Now | Proposed Fix |
| :--- | :--- | :--- | :--- |
| **1A** | [`ReactionEngine.cs:L381-L418`](file:///usr/local/google/home/amattapalli/PersonalProjects/sts2-mod/Transmuter/TransmuterCode/Util/ReactionEngine.cs#L381-L418) + [`ParacelsusScalpel.cs`](file:///usr/local/google/home/amattapalli/PersonalProjects/sts2-mod/Transmuter/TransmuterCode/Relics/ParacelsusScalpel.cs) | **Deterministic Game Freeze (`StackOverflowException`):** `ParacelsusScalpel` leaves `1` stack of **every** reacting Reagent behind (`distinct == 2` after a reaction). When `ResidualPrecipitatePower` reapplies a Reagent in `ExecutePostReactionHooks` (`L536`), it calls `ResolveReactions`, sees `distinct == 2`, and loops infinitely! Even alone, `ParacelsusScalpel` leaves all 3 Reagents on an enemy permanently (`distinct == 3`), firing un-stabilized `Magnum Opus` on every card play and bricking `Distill` / `LeadToGold`. | Change `ParacelsusScalpel` so `ConsumeAllReagents` leaves `1` (or `2`) stack of **only the first Reagent consumed** (`distinct == 1` after a reaction), and pass `resolveReactions: false` when `ResidualPrecipitatePower` applies post-reaction residue. |
| **1B** | [`StabilizedPower.cs:L23-L49`](file:///usr/local/google/home/amattapalli/PersonalProjects/sts2-mod/Transmuter/TransmuterCode/Powers/StabilizedPower.cs#L23-L49) | **Wasted Block Timing + Artifact Collision:** `StabilizedPower` is applied to the enemy (`Owner = enemy`) and checks `if (side != Owner.Side) return;`. When you fail to reach 3 Reagents, the paused 2-Reagent reaction (`Calcify` Block/Weak) fires at the **end of the enemy's turn** (after the enemy already attacked you), and the Block immediately vanishes when your turn starts! Also, `PowerType.Debuff` means enemy `Artifact` blocks `Stabilize`. | Resolve `StabilizedPower` at the **end of the Player's turn** (`CombatSide.Player`, before the enemy attacks) and change `StabilizedPower` to `PowerType.Buff` (neutral state marker). |
| **1C** | [`SalineSolution.cs:L32-L44`](file:///usr/local/google/home/amattapalli/PersonalProjects/sts2-mod/Transmuter/TransmuterCode/Cards/Basic/SalineSolution.cs#L32-L44) & [`CrackedAlembic.cs:L24`](file:///usr/local/google/home/amattapalli/PersonalProjects/sts2-mod/Transmuter/TransmuterCode/Relics/CrackedAlembic.cs#L24) | **Starter Card Self-Reacts + Starter Relic Collides:** `SalineSolution` applies `2 Salt` **and** `1 Mercury` in one card—meaning on an empty enemy, a 1-cost Basic card self-triggers `Dissolve` (`X=3`: strip Block + 2 Vuln + Draw 1)! Meanwhile, `CrackedAlembic` hardcodes `2 Sulfur`, which collides 100% with `BrimstoneToss` (`2 Sulfur`), resulting in only a **50.0% Turn 1 Reaction rate** with the starter deck. | Make `SalineSolution` apply pure `2 (3) Salt` (`5 [7] Block`), and change `CrackedAlembic` to apply **`2 Mercury`** at combat start so **both** `BrimstoneToss` (`Sulfur`) and `SalineSolution` (`Salt`) react with `CrackedAlembic` on Turn 1 (**77.8% Turn 1 Reaction rate**!). |
| **1D** | [`QuickPrimer.cs`](file:///usr/local/google/home/amattapalli/PersonalProjects/sts2-mod/Transmuter/TransmuterCode/Cards/Common/QuickPrimer.cs) + [`GlassVial.cs`](file:///usr/local/google/home/amattapalli/PersonalProjects/sts2-mod/Transmuter/TransmuterCode/Cards/Common/GlassVial.cs) | **2-Common-Card 0-Energy Infinite Loop:** `QuickPrimer` (`0E`: `1 [2]` Mercury + Draw 1) + `GlassVial` (`0E`: `3 [5]` Dmg + `1 [2]` Salt) triggers `Dissolve` (`Salt + Mercury`), which **also draws 1–2 cards**. In a slimmed deck, these two Commons draw each other infinitely for 0 Energy! | Change `QuickPrimer` to **1 Energy: Apply 2 (3) Mercury, Draw 2 cards** (or keep at 0 Energy and add `Exhaust`). |
| **1E** | [`TheMagnumOpus.cs:L29-L34`](file:///usr/local/google/home/amattapalli/PersonalProjects/sts2-mod/Transmuter/TransmuterCode/Cards/Rare/TheMagnumOpus.cs#L29-L34) & [`ReactionEngine.cs:L163`](file:///usr/local/google/home/amattapalli/PersonalProjects/sts2-mod/Transmuter/TransmuterCode/Util/ReactionEngine.cs#L163) | **Double Magnum Opus Trigger:** Line 32 (`ApplyReagent(Mercury)`) already auto-triggers `TriggerMagnumOpus` when all 3 Reagents are present. Line 33 then calls `TriggerMagnumOpus` a **second** time, which doesn't verify `distinct >= 3` and fires a second Magnum Opus off any `ResidualPrecipitate` stacks! | Require `GetDistinctReagentTypes(target) >= 3` inside `TriggerMagnumOpus` and remove the redundant explicit call if `ResolveReactions` already triggered it. |
| **1F** | [`SympatheticTincture.cs:L34-L45`](file:///usr/local/google/home/amattapalli/PersonalProjects/sts2-mod/Transmuter/TransmuterCode/Cards/Uncommon/SympatheticTincture.cs#L34-L45) & [`LeadToGold.cs:L29-L30`](file:///usr/local/google/home/amattapalli/PersonalProjects/sts2-mod/Transmuter/TransmuterCode/Cards/Uncommon/LeadToGold.cs#L29-L30) | **Loop Premature Reaction & Free Gold:** `SympatheticTincture` calls `ApplyReagent` inside a `for` loop `N` times (triggering a weak reaction on `i=0` and multiplying `EmeraldTablet` `N` times). `LeadToGold` grants Gold even when `SelfReactSingleReagent` fails (`0` Reagents on target). | Multiply `distinctDebuffs * Sulfur` and call `ApplyReagent` **once** in `SympatheticTincture`; have `SelfReactSingleReagent` return `Task<bool>` and only grant Gold in `LeadToGold` when `true`. |

---

## 2. Quantitative Monte Carlo Simulation Results (10,000 Combats)

`balance-sim` simulated 10,000 3-turn combat openings across the Floor 1 Starter Deck (`A1` current code vs. `A2` non-colliding starter relic) and an End-of-Act-1 15-card deck (`B1` single target, `B2` 2-enemy fight):

| Metric (10,000 3-Turn Trials) | A1: Starter Deck (Current `2 Sulfur` Alembic) | A2: Starter Deck (Fixed `2 Mercury` / Non-Colliding Alembic) | B1: End of Act 1 Deck (15c, 1 Enemy) | B2: End of Act 1 Deck (15c, 2 Enemies) |
| :--- | :--- | :--- | :--- | :--- |
| **Turn 1 Reaction %** | **`49.3%`** *(exact: `50.0%`)* | **`77.8%`** *(with pure Salt `SalineSolution`)* | **`91.6%`** | **`91.6%`** |
| **Turn 1 or Turn 2 Reaction %** | **`100.0%`** | **`100.0%`** | **`99.99%`** | **`99.99%`** |
| **Avg 3-Turn Total Damage** | `60.6 Dmg` (`20.2/turn`) | `67.0 Dmg` (`22.3/turn`) | `85.0 Dmg` (`28.3/turn`) | `108.3 Dmg` (`36.1/turn`) |
| **Avg 3-Turn Total Block** | `23.5 Blk` (`7.8/turn`) | `19.4 Blk` (`6.5/turn`) | `23.1 Blk` (`7.7/turn`) | `23.1 Blk` (`7.7/turn`) |
| **Value per Energy Spent (DPE + BPE)** | **`8.40 Value / E`** | **`8.64 Value / E`** | **`10.80 Value / E`** | **`13.14 Value / E`** |

---

## 3. Core Reaction Engine Balance (`ReactionEngine.cs`)

Currently in [`ReactionEngine.cs`](file:///usr/local/google/home/amattapalli/PersonalProjects/sts2-mod/Transmuter/TransmuterCode/Util/ReactionEngine.cs), Potency $X = \text{sum of reacting Reagent stacks}$:

| Reaction | Current Formula (`ReactionEngine.cs`) | Why It Needs Tuning | Recommended Formula |
| :--- | :--- | :--- | :--- |
| **Detonate** (`Sulfur + Mercury`) | **`3X` Target Dmg + `2X` AoE Dmg** | Two unupgraded 1E Commons (`Calcination` + `CinnabarSlash`, $X=5$) deal **`30` single-target damage + `10` AoE damage** for 2 Energy (+50% above vanilla 2E benchmarks) while solving both single-target and AoE fights simultaneously. | **`2X` Target Dmg + `1X` AoE Dmg** (at $X=5$, deals `25` target Dmg + `5` AoE—still very strong and rewarding, without trivializing Act 1). |
| **Calcify** (`Salt + Sulfur`) | **`2X` Block + `ceil(X / 3)` Weak** | At $X=4$ (1 card + starter relic), `ceil(4/3) = 2` turns of Weak **plus** `8` Block makes permanent Weak lock trivial from Floor 1. | **`2X` Block + `1` Weak (`2` Weak if `X >= 6`)** (matches `Dissolve`'s `X >= 6` breakpoint reward!). |
| **Dissolve** (`Salt + Mercury`) | **Strip Target Block + `ceil(X / 2)` Vulnerable + Draw `1` (`2` if `X >= 6`)** | Balanced once `SalineSolution` and `QuickPrimer` are fixed so it isn't triggered for 0 Energy in a 2-card infinite. | **Keep as is** (fix Block strip to remove Block directly rather than dealing unpowered damage equal to Block). |

---

## 4. Card-by-Card Balance Recommendations (`Before -> After`)

### A. Basic & Common Cards (Top Adjustments)
1. **[`SalineSolution.cs`](file:///usr/local/google/home/amattapalli/PersonalProjects/sts2-mod/Transmuter/TransmuterCode/Cards/Basic/SalineSolution.cs) (Basic, 1E Skill):**
   - **Before:** `Gain 5 (7) Block. Apply 2 (3) Salt and 1 (2) Mercury.` (Self-triggers `Dissolve` on an empty target!)
   - **After:** **`Gain 5 (7) Block. Apply 2 (3) Salt.`** (Pair with changing [`CrackedAlembic.cs`](file:///usr/local/google/home/amattapalli/PersonalProjects/sts2-mod/Transmuter/TransmuterCode/Relics/CrackedAlembic.cs) from `2 Sulfur` to **`2 Mercury`** so both `BrimstoneToss` and `SalineSolution` trigger a Reaction on Turn 1!).
2. **[`CinnabarSlash.cs`](file:///usr/local/google/home/amattapalli/PersonalProjects/sts2-mod/Transmuter/TransmuterCode/Cards/Common/CinnabarSlash.cs) vs. [`Calcination.cs`](file:///usr/local/google/home/amattapalli/PersonalProjects/sts2-mod/Transmuter/TransmuterCode/Cards/Common/Calcination.cs) (Common, 1E Attacks):**
   - **Before:** `CinnabarSlash` is `7 (9) Dmg + 2 (3) Mercury` (strictly worse than `Calcination`'s `8 (11) Dmg + 3 (4) Sulfur`).
   - **After:** Make both parity 1E Common applicators:
     - **`CinnabarSlash`:** **`7 (10) Dmg, Apply 3 (4) Mercury`**
     - **`Calcination`:** **`7 (10) Dmg, Apply 3 (4) Sulfur`**
3. **[`HaliteBash.cs`](file:///usr/local/google/home/amattapalli/PersonalProjects/sts2-mod/Transmuter/TransmuterCode/Cards/Common/HaliteBash.cs) (Common, 1E Attack):**
   - **Before:** `Deal 6 (8) Dmg. Gain Block equal to target's Salt. Apply 3 (4) Salt.` (Grants `0` Block on an empty target because Salt is applied *after* the attack).
   - **After:** **`Apply 2 (3) Salt, then deal 6 (9) Dmg.`** (Because `SaltPower` grants Block equal to Salt whenever you attack the enemy, applying Salt *first* means `HaliteBash` automatically grants `2 (3)` Block on an empty enemy, or triggers `Calcify`/`Dissolve` *before* the hit!).
4. **[`QuickPrimer.cs`](file:///usr/local/google/home/amattapalli/PersonalProjects/sts2-mod/Transmuter/TransmuterCode/Cards/Common/QuickPrimer.cs) (Common, 0E Skill):**
   - **Before:** `Cost 0: Apply 1 (2) Mercury. Draw 1 card.` (Creates a 0-cost infinite with `GlassVial`).
   - **After:** **`Cost 1: Apply 2 (3) Mercury. Draw 2 cards.`** (Clean 1-cost card-draw + Mercury bridge, or keep at `Cost 0` and add `Exhaust`).
5. **[`FumeCloud.cs`](file:///usr/local/google/home/amattapalli/PersonalProjects/sts2-mod/Transmuter/TransmuterCode/Cards/Common/FumeCloud.cs), [`Distill.cs`](file:///usr/local/google/home/amattapalli/PersonalProjects/sts2-mod/Transmuter/TransmuterCode/Cards/Common/Distill.cs), [`SplashAcid.cs`](file:///usr/local/google/home/amattapalli/PersonalProjects/sts2-mod/Transmuter/TransmuterCode/Cards/Common/SplashAcid.cs), [`HermeticSeal.cs`](file:///usr/local/google/home/amattapalli/PersonalProjects/sts2-mod/Transmuter/TransmuterCode/Cards/Common/HermeticSeal.cs) (Common Floors):**
   - **`FumeCloud`:** `Apply 2 (3) Sulfur to ALL enemies` $\rightarrow$ **`Gain 4 (6) Block. Apply 2 (3) Sulfur to ALL enemies.`**
   - **`Distill`:** `Double (Triple) a single Reagent` (`0` floor on empty target) $\rightarrow$ **`Gain 4 (6) Block. Double (Triple) a single Reagent on an enemy.`**
   - **`SplashAcid`:** `Deal 5 (7) Dmg (x2 if enemy has Reagent)` $\rightarrow$ **`Deal 6 (8) Dmg (x2 if enemy has Reagent)`** (so its floor is never worse than a basic Strike).
   - **`HermeticSeal`:** `Gain 6 (9) Block. Apply 1 Stabilize.` $\rightarrow$ **`Gain 7 (10) Block. Apply 1 Stabilize and 1 (2) Salt to an enemy.`** (Contributes 1 of the 3 Reagents toward `Magnum Opus` so `Stabilize` isn't a dead 4-card requirement in Act 1).

### B. Uncommon & Rare Cards (Top Adjustments)
6. **[`CrucibleShield.cs`](file:///usr/local/google/home/amattapalli/PersonalProjects/sts2-mod/Transmuter/TransmuterCode/Cards/Uncommon/CrucibleShield.cs) (Uncommon Skill):**
   - **Before:** `Cost 2: Gain 12 (16) Block. Gain 4 (6) Block per Reaction this turn.` (At 2 Energy, you only have 1 Energy left to trigger a Reaction).
   - **After:** **`Cost 1: Gain 6 (8) Block. Gain 4 (6) Block per Reaction this turn.`** (Leaves 2 Energy to fire multiple Reactions on the same turn).
7. **[`ResidualPrecipitate.cs`](file:///usr/local/google/home/amattapalli/PersonalProjects/sts2-mod/Transmuter/TransmuterCode/Cards/Uncommon/ResidualPrecipitate.cs) (Uncommon Power):**
   - **Before:** `Cost 1: Whenever a Reaction triggers, reapply 2 (3) stacks of the first Reagent consumed.`
   - **After:** **`Cost 1: Whenever a Reaction triggers, reapply 1 (2) stack(s) of the first Reagent consumed`** (and do not recursively call `ResolveReactions` on the reapplied residue).
8. **[`LeadToGold.cs`](file:///usr/local/google/home/amattapalli/PersonalProjects/sts2-mod/Transmuter/TransmuterCode/Cards/Uncommon/LeadToGold.cs) (Uncommon Skill, Exhaust):**
   - **Before:** `Cost 1 (0): Self-react 2+ stacks of a single Reagent. Gain 5 (8) Gold. Exhaust.`
   - **After:** **`Cost 1 (0): Self-react 2+ stacks of a single Reagent. If a Reaction triggered, gain 10 (15) Gold. Exhaust.`** (Also add `"Exhaust."` to `cards.json`).
9. **[`TheMagnumOpus.cs`](file:///usr/local/google/home/amattapalli/PersonalProjects/sts2-mod/Transmuter/TransmuterCode/Cards/Rare/TheMagnumOpus.cs) (Rare Skill, Exhaust):**
   - **Before:** `Cost 2 (1): Apply Stabilize, then 3 (5) Salt, Sulfur, and Mercury, then trigger Magnum Opus.` (Upgraded outputs **`45 Dmg + 30 AoE + 30 Block + 5 Weak + 8 Vuln + Draw 2` for 1 Energy**!).
   - **After:** **`Cost 2 (1): Apply Stabilize, then 2 (3) Salt, Sulfur, and Mercury, then trigger Magnum Opus.`** (At $X=6$ base / $X=9$ upgraded + tuned `Detonate`, deals `12 (18)` target Dmg + `6 (9)` AoE + `12 (18)` Block + Weak/Vuln + Draw 2—still a massive 1-card turn swing without invalidating the entire rest of the card pool).
10. **[`UniversalSolvent.cs`](file:///usr/local/google/home/amattapalli/PersonalProjects/sts2-mod/Transmuter/TransmuterCode/Cards/Rare/UniversalSolvent.cs) (Rare Attack, AllEnemies):**
    - **Before:** `Cost 2 (non-Exhaust): Deal 12 (16) AoE Dmg, then remove Block & Artifact, then apply 3 (4) Salt and 3 (4) Mercury to ALL enemies` (draws up to 6 cards in a 3-enemy fight and hits *before* stripping Block!).
    - **After:** **`Cost 2 (Exhaust): Remove all Artifact and Block from ALL enemies, deal 12 (16) AoE Dmg, then apply 2 (3) Salt and 2 (3) Mercury to ALL enemies. Exhaust.`**
11. **[`EmeraldTablet.cs`](file:///usr/local/google/home/amattapalli/PersonalProjects/sts2-mod/Transmuter/TransmuterCode/Cards/Rare/EmeraldTablet.cs) (Rare Power):**
    - **Before:** `Cost 2 (2): Apply +1 (+2) additional stacks whenever you apply a Reagent.`
    - **After:** **`Cost 2 (1): Apply +1 additional stack whenever you apply a Reagent.`** (Upgrading cost `2 -> 1` is much cleaner than `+2` per application, which scales quadratically on multi-hit/multi-reagent cards).
