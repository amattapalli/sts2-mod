# STS2 Character Brainstorm: Seed Concepts

> [!NOTE]
> These are starting points to react to; nothing is decided. You can mix and match, for example the Conductor's look with the Transmuter's mechanic. The prior-art notes come from quick web searches on 2026-10-06. Card numbers are rough placeholders, not balanced.

## At a glance

| # | Concept (look) | Gimmick in one line | Feel | Closest prior art | Build cost |
|---|---|---|---|---|---|
| 1 | **Conductor** (undead maestro) | Every card plays a beat in a 4-beat measure. Cards get a bonus when they land on the right beat. | Ordering puzzle, combo bursts | STS1 *The Bard*: notes combine into Melodies by recipe. Partial overlap (recipe vs. position). | Low–Med |
| 2 | **Tidecaller** (drowned lighthouse keeper) | A two-way Tide meter from 0 to 10. Flood cards push it up and Ebb cards push it down, with a payoff at each end. | Rhythmic swing | STS1 *Skadi* (an Arknights crossover) has a one-way 0–3 Tide charge that bursts. Ours swings both ways. | Low–Med |
| 3 | **Gardener** (moss golem) | Plant Seeds in a Garden pile. They grow each turn, and you Harvest them for big effects. | Slow-burn scaling | None found | Med |
| 4 | **Transmuter** (plague-doctor alchemist) | Apply Salt, Sulfur, or Mercury to enemies. Mixing two different ones triggers a Reaction. | Set up, then detonate; co-op combos | STS1 *The Alchemist* is built on potions and self-poison, with no reactions. That name is taken, hence "Transmuter". | Low–Med |
| 5 | **Usurer** (gilded devil) | Borrow power now and take on Debt that grows with Interest. Repay it with gold or HP. | Push-your-luck | STS1 *The Hierophant* already spends gold in combat. The Debt/Interest loop is the new part. | Med |
| 6 | **Warden** (chain jailer) | Tether creatures together so damage and debuffs spread along the chains. In co-op, tether allies. | Area control, co-op tank | None found | Med–High |

**Cut:** *Glyphwright* (combine 3 glyphs into one spell). It's essentially Downfall's *Automaton*, which Encodes 3 cards and compiles them into a Function card.

---

## 1. Conductor: Tempo

- **Look:** A skeletal maestro in a tattered tailcoat, with a baton that doubles as a rapier. A phantom orchestra behind him plays louder as the fight goes on.
- **Rules:**
  - Each card you play advances the **Beat**: 1 → 2 → 3 → 4 → 1. One full cycle is a **Measure**.
  - Cards can have beat triggers: **Downbeat** (played on beat 1), **Backbeat** (played on beat 2 or 4), and **Finale** (played on beat 4).
  - Completing a Measure gives you 1 **Crescendo**: your Attacks deal +1 damage per stack for the rest of combat.
  - Two ways to bend the Beat: **Rest** (this card doesn't advance it) and **Syncopate** (advance 2 beats).
- **Sample cards:**
  - **Downbeat Strike** (1, Attack, Common): Deal 7 damage. *Downbeat:* deal it again.
  - **Grace Note** (0, Skill, Common): Syncopate. Draw 1 card.
  - **Coda** (2, Attack, Rare): Deal 6 damage to ALL enemies. *Finale:* +3 damage per Crescendo.
- **Starter relic idea:** *Metronome*. Your first Downbeat each turn draws 1 card.
- **Why it's fun:** Every turn is an ordering puzzle, and Rest and Syncopate keep bad draws from ruining it.
- **Watch-outs:** It can turn into bookkeeping. It needs a clear 4-pip beat indicator next to the character.
- **Co-op angle:** "Ensemble" cards give allies Crescendo.
- **Tech:** A Beat counter (a power or `CustomResource`), an `AfterCardPlayed` hook, keyword enums, and pip UI through `AddedNode`.
- **Key design fork:** Should the Beat carry over between turns (flexible, lets you plan ahead) or reset every turn (tighter, rewards playing exactly 4 cards)?

## 2. Tidecaller: Tide

- **Look:** A drowned lighthouse keeper in a barnacled coat, swinging an anchor flail. His lantern still burns underwater.
- **Rules:**
  - The **Tide** meter runs from 0 to 10, starts at 5, and carries over between turns. **Flood X** raises it and **Ebb X** lowers it.
  - High-tide cards hit harder when Tide is high. Low-tide cards block or draw more when Tide is low.
  - Reaching **10 (High Tide)** triggers a free Tsunami that damages ALL enemies, then Tide resets to 5. Reaching **0 (Low Tide)** draws 2 cards and gives 1 energy, then Tide resets to 5.
- **Sample cards:**
  - **Undertow** (1, Attack, Common): Deal 6 damage. Ebb 2.
  - **Swell** (1, Skill, Common): Gain 6 Block. Flood 3.
  - **Leviathan's Call** (3, Power, Rare): Whenever you reach High or Low Tide, deal 8 damage to ALL enemies.
- **Starter relic idea:** *Drowned Lantern*. The first time you reach High or Low Tide each combat, gain 1 energy.
- **Why it's fun:** There's a readable push-pull decision every turn, with two different payoffs to aim for.
- **Watch-outs:** If the middle of the meter is dull, it plays like STS1 Watcher's stance swapping. Give exactly-5 ("Slack Water") cards a reason to exist.
- **Co-op angle:** High Tide also gives allies Block ("a rising tide lifts all ships").
- **Tech:** A bounded `CustomResource`, threshold hooks, and a meter UI.
- **Key design fork:** Does Tide drift 1 toward 5 each turn (a "current" you push against), or stay where you leave it?

## 3. Gardener: Seeds

- **Look:** A moss-covered stone golem with a garden growing on its back. Slow and patient, and enormous by late game.
- **Rules:**
  - **Plant** puts a Seed card into your **Garden**, a new pile shown next to the character.
  - At the start of each turn your plants **Grow**: Seed → Sprout → Bloom.
  - **Harvest** uses up plants for their effect. Blooms give the full effect. A Bloom you don't harvest wilts after one turn, so use it or lose it.
  - Seed types: Thornseed (damage), Ironbark (Block), Sunpetal (energy), Nightshade (weakens enemies).
- **Sample cards:**
  - **Sow** (1, Skill, Common): Plant 2 random Seeds.
  - **Reap** (1, Attack, Common): Deal 5 damage. Harvest all Blooms.
  - **Old Growth** (2, Power, Rare): Your plants Grow twice each turn.
- **Starter relic idea:** *Seed Pouch*. Start each combat with 2 random Seeds planted.
- **Why it's fun:** Turns you invest in pay off later in explosive turns, and drafting is about tuning your seed mix.
- **Watch-outs:** Starts are slow in Act 1 hallway fights, so the deck needs some cards with immediate value. The pile UI and growth stages also mean extra art.
- **Co-op angle:** *Cross-pollinate* lets you hand a Bloom to an ally.
- **Tech:** A `CustomPile` for the Garden, growth hooks, card transforms, and a pile UI.
- **Key design fork:** A Garden made of real cards (flexible, more code) or plants as orb-like slots (reuses `CustomOrbModel`, cheaper, but closer to Defect)?

## 4. Transmuter: Reactions

- **Look:** A plague-doctor alchemist chasing the Magnum Opus, with brass flasks and sigil-etched gloves. Three vials stand for Paracelsus's *tria prima*: white Salt, yellow Sulfur, and silver Mercury.
- **Rules:**
  - Cards apply **Reagents** to enemies: **Salt**, **Sulfur**, or **Mercury**. Each is a stackable debuff.
  - Applying a *different* Reagent to an enemy that already has one triggers a **Reaction**. The Reaction uses up both Reagents and scales with their stacks:
    - Sulfur + Mercury → **Detonate**: damage to the target and the enemies next to it
    - Salt + Sulfur → **Calcify**: the enemy loses Strength
    - Salt + Mercury → **Dissolve**: removes the enemy's Block and applies Vulnerable
  - **Catalyst** cards apply all three at once and trigger **Magnum Opus**, a big finisher.
- **Sample cards:**
  - **Brimstone Flask** (1, Attack, Common): Deal 5 damage. Apply 3 Sulfur.
  - **Quicksilver** (0, Skill, Common): Apply 1 Mercury. Draw 1 card.
  - **Philosopher's Engine** (3, Power, Rare): Reactions trigger twice.
- **Starter relic idea:** *Alembic*. Your first Reaction each turn draws 1 card.
- **Why it's fun:** You set up combinations and then detonate them, which turns multi-enemy fights into puzzles.
- **Watch-outs:** Enemies that are immune to debuffs (for example, ones with Artifact) block Reagents, the same way they block Poison, and that's fine. Reaction strength needs careful tuning. Three Reagents is the most that stays readable.
- **Co-op angle:** Let some Reactions key off base-game debuffs such as Vulnerable and Weak. That way any teammate's cards help set up your detonations.
- **Tech:** Three `PowerModel`s plus a resolver that runs when a power is applied. It reuses the existing debuff UI, so it needs the least new UI of the six.
- **Key design fork:** Should Reactions fire automatically (simpler, more combo-heavy), or only when you play a **Catalyze** card (more control, and Reagents can stack higher first)?

## 5. Usurer: Debt

- **Look:** A gilded devil moneylender in coin-mail, carrying a ledger and quill. His contracts burn when signed.
- **Rules:**
  - **Borrow X** effects give you power now and add X **Debt**.
  - At the end of each turn, Debt grows by **Interest** (+10%, rounded up).
  - **Repay** effects pay Debt with gold or HP. Debt left at the end of combat comes out of your gold. If you can't cover it, a **Collector** curse is added to your deck.
  - Some cards scale with Debt, so the deeper you are in, the harder you hit.
- **Sample cards:**
  - **Easy Credit** (0, Skill, Common): Gain 2 energy. Borrow 15.
  - **Foreclose** (2, Attack, Uncommon): Deal 8 damage, plus 1 for every 5 Debt.
  - **Golden Parachute** (1, Skill, Rare): Repay all Debt with HP instead of gold (1 HP per 5 Debt). Exhaust.
- **Starter relic idea:** *Ledger*. If you end a combat with no Debt, gain 15 gold.
- **Why it's fun:** Push-your-luck tension that reaches shops and events, not just combat.
- **Watch-outs:** Death spirals are a risk. Gold also pays for card removal, so balance is delicate. Keep the identity on *borrowing from the future* so it doesn't read as the Hierophant.
- **Co-op angle:** *Co-sign*. Give an ally energy now and take on the Debt yourself.
- **Tech:** A `CustomResource` for Debt, hooks for gold and end of combat, and a curse card.
- **Key design fork:** Does Debt stay inside one combat, or carry over between combats (bolder, but harder to balance)?

## 6. Warden: Tethers

- **Look:** The Spire's chain-wrapped jailer, with a key-ring flail, manacles, and a lantern full of trapped souls.
- **Rules:**
  - **Tether** links two creatures: enemy to enemy, enemy to you, or ally to ally.
  - When you damage a tethered enemy, everything tethered to it takes 50% of that damage. Debuffs you apply also spread along tethers.
  - When a tethered creature dies, the tether **Snaps** and its overkill damage hits the creature on the other end.
- **Sample cards:**
  - **Chain Lash** (1, Attack, Common): Deal 7 damage. Tether the target to another random enemy.
  - **Iron Maiden** (2, Skill, Uncommon): Gain 10 Block. Tether yourself to an enemy. Whenever it attacks you, it takes 4 damage.
  - **Chain Reaction** (2, Power, Rare): Damage spreads along tethers at 100% instead of 50%.
- **Starter relic idea:** *Key Ring*. At the start of combat, tether the two leftmost enemies.
- **Why it's fun:** Single-target hits become area control, and large fights shine. In co-op you get to play the tank.
- **Watch-outs:**
  - It's weak against a lone boss, so it needs self-tether cards and boss-specific payoffs.
  - Chain loops must be capped, for example so each hit spreads only once.
  - Drawing chains between creatures is real visual-effects work.
- **Co-op angle:** Tether allies to share Block, or to redirect damage onto the Warden.
- **Tech:** Hooks that spread damage, a way to pick 2 targets, and chain visuals through `AddedNode`.
- **Key design fork:** Should spread be a simple percentage, or should tethers come in types (Iron, Silver, Bone chains, each with its own effect)? Types are richer but more work.

---

## Picking heuristics

| If you want... | Look at |
|---|---|
| Something fresh that's also cheap to build | Transmuter |
| The most distinctive turn-by-turn puzzle | Conductor |
| The easiest to read at a glance | Tidecaller |
| A long-game snowball | Gardener |
| Risk and reward across the whole run (shops, events) | Usurer |
| A co-op-first identity | Warden, Transmuter |

**My lean for a first mod: Transmuter.** Nothing like it exists in STS, and it needs no new UI because Reagents are ordinary debuffs. It does well in multi-enemy and co-op fights, and the tria prima gives it an instant gothic identity. The runner-up is Conductor, if you want the most distinctive moment-to-moment play.

## Questions for the next round

1. **Tone:** Gothic-grim (the game's house style), whimsical, cosmic horror, or mythic/folklore? Are you drawing on any games, books, or characters?
2. **Feel:** Big combo turns, slow-scaling control, push-your-luck, or co-op support?
3. **v1 ambition:** One keyword on top of existing systems, a new resource with its own counter, or a new pile or targeting system?
4. **Art:** Drawing it yourself, commissioning it, or placeholders until the mechanics feel right?
