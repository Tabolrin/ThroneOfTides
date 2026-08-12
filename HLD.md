# Throne of Tides — High-Level Design (Living Snapshot)

> **What this file is:** a local, code-verified snapshot of the game as it currently exists in
> the Unity project — every number below was read directly from the live `CardSO`/`CaptainSO`/
> `GameConfigSO` assets, not copied from a spec. It supersedes the README's numbers where they
> disagree, and exists to track balance changes and new mechanics as they land, in between
> updates to the canonical [HLD v8 Google Doc](https://docs.google.com/document/d/133qzpSa4RO27y_zbFs6qLWrUmpmaZXtODrBtMcZaaso/edit?usp=sharing).
>
> Last verified: 2026-08-11.

---

## 1. Core Concept

A single-player, turn-based pirate ship duel. Two ships (Player vs. an AI Captain) trade cards
across turns until one hull reaches 0 HP. Hand and deck are a single shared resource — running
both dry sinks the ship regardless of remaining HP.

## 2. Turn Structure

```
Draw → Main Phase → Defense Window → End Phase   (mirrored for the AI)
```

- The player must play at least one card per turn — passing is not an option.
- The **Defense Window** is where charged Reaction cards (see §4.4) can trigger against an
  incoming attack, before damage lands.
- Reactions are never played from hand manually — they're **charged automatically on draw** and
  auto-consumed (by the owning side's AI-equivalent choice logic) when the opponent attacks.

## 3. Match Economy

| Resource | Starting Value | Notes |
|---|---|---|
| HP | 30 (base) | Per-captain HP overrides the base for the AI side — see §6 |
| Max Mana | 3 (base) | Fully resets every turn, no carry-over |
| Hand Size | 4 max | |
| Deck Storage Capacity | 20 base slots | Every card has a mana cost (play cost) **and** a separate storage/slot cost (deck-build cost) |

Mana, HP, and storage capacity are all independently upgradeable meta-progression tracks (§7).

## 4. Card Types & Mechanics

### 4.1 Weapon
Deals flat `Damage` to the opponent. No special resolution — see the catalog in §5 for current
numbers.

### 4.2 Action
No direct damage; utility effects (heal, draw, mana steal, hand disruption, etc.). Costs mana
like any other card and can be paired with a Weapon card in the same turn for a bigger turn.

### 4.3 DOT (Damage over Time)
Applies a ticking effect (`DamageAndDuration`) that fires at the start of the affected ship's own
turn. Two DOT cards currently exist (Hail Storm, Whirlpool) — the design supports up to 2
simultaneous DOT effects on one ship with no duplicate types.

### 4.4 Reaction
Never played manually. Charges automatically the moment it's **drawn**, then sits as a stored
charge (`PlayerDeadMansTurnCharges` / `PlayerCounterGaleCharges`, mirrored for the enemy) until
the owning side takes an incoming attack. At that point, the reaction is auto-evaluated (weighted
AI choice on the enemy side) to either fully negate the hit (**Dead Man's Turn**) or reflect half
the incoming damage, rounded down (**Counter Gale**). Charges are consumed on use.

### 4.5 Combo — Gunpowder / Torch
The signature two-card combo:

1. **Gunpowder Barrel** primes the *target* ship (not the caster's own ship) with a Gunpowder
   stack, incrementing `ComboStackCount` on that ship each time it's played again. The stack is
   visually represented by a world-space status indicator + a one-shot particle puff on the
   ship the instant the first stack goes active.
2. **Torch**, played by the same attacker, checks whether the target ship currently has an
   active Gunpowder stack. If so, it ignites it: total damage = `8 + ((stack - 1) × 2)`, the
   stack resets to 0, and a **medium explosion + Level 4 screen shake** plays (see §8.2). If the
   target has no active stack, Torch just deals its own flat 1 damage with no explosion.
3. Interruption resets the stack — e.g. **Tidal Wave** can be played on your *own* ship to
   deliberately break an active Gunpowder stack before the enemy can ignite it.

### 4.6 Other status mechanics
- **Siren Song** — the next attack the caster plays this turn becomes unblockable (bypasses
  Reaction negation/reflection entirely).
- **High Spirits** — permanent in-match mana gain, stacks across a single match.
- **Treasure Chest** — returns cards from the original deck + draws 1, with a 50% chance of also
  granting +1 max mana for the rest of the match.

## 5. Current Card Catalog

*(Ground truth as of this snapshot — pulled directly from the `CardSO` assets.)*

| Card | Type | Mana | Damage | Combo Dmg | HP Cost | Notes |
|---|---|---|---|---|---|---|
| Pistol | Weapon | 0 | 1 | — | — | Cheapest attack in the game |
| Cannonball | Weapon | 1 | 2 | — | — | |
| Tidal Wave | Weapon | 2 | 3 | — | — | Requires target selection; clears Gunpowder from the hit ship |
| Chain Shot | Weapon | 2 | 4 | — | 2 | Sacrifice 2 HP for extra damage |
| Whale Ram | Weapon | 3 | 6 | — | — | |
| Lightning | Weapon | 4 | 10 | — | — | **New: Level 4 screen shake on strike** |
| **The Kraken** | Weapon | 5 | **20** | — | 4 | Unblockable. Also costs 33% of current materials. **Balance change: was 15 → now 20.** **New: Level 5 screen shake (strongest in the game) on attack.** Max 1 copy per deck |
| Gunpowder Barrel | Combo | 1 | — | 8 (+2/stack) | — | Primes the *target* ship; combo partner: Torch |
| Torch | Combo | 3 | 1 | 8 (+2/stack) | — | Ignites an active Gunpowder stack on the target for combo damage. **New: medium explosion + Level 4 screen shake on ignition** (previously had no impact VFX at all) |
| Hail Storm | DOT | 3 | — | — | — | 2 dmg/turn × 3 turns |
| Whirlpool | DOT | 4 | — | — | — | 3 dmg/turn × 3 turns |
| Rum | Action | 1 | — | — | — | Restore 5 HP |
| High Spirits | Action | 3 | — | — | — | Restore 5 HP + permanent mana stack |
| Recon Parrot | Action | 1 | — | — | — | Inspect 3 cards in enemy hand |
| Monkey Grab | Action | 2 | — | — | — | Steal 1 random card from enemy hand |
| Treasure Chest | Action | 2 | — | — | — | Return 3 cards from original deck + draw 1; 50% chance +1 max mana |
| Locker's Return | Action | 3 | — | — | — | Recover 3 random cards from discard (cannot recover The Kraken) |
| Siren Song | Action | 3 | — | — | — | This turn's attack becomes unblockable |
| Essence Plunder | Action | 0 | — | — | 3 | Steal 2 mana from the enemy this turn |
| Dead Man's Turn | Reaction | 0 | — | — | — | Charges on draw; negates the next incoming attack |
| Counter Gale | Reaction | 0 | — | — | — | Charges on draw; reflects 50% of incoming damage (rounded down) |

## 6. AI Captains

Three captains, each a distinct weighted-decision archetype (`CaptainSO` exposes ~18 individual
weight sliders — high/low damage weapon preference, combo initiation, DOT preference, healing,
reaction play-around, etc. — evaluated against live game state and mana budget each AI turn).

| Captain | HP | Max Mana | Archetype |
|---|---|---|---|
| Captain Rumboat | 40 | 4 | Aggressive |
| Captain Loreley | 50 | 4 | Action-heavy sustain — spams Siren Song, chips away with DOT and medium attacks to prolong the fight |
| Captain Nurgle | 60 | 5 | Aggressive burst — spams Kraken even at HP cost, builds mana with Essence Plunder/High Spirits then bursts |

> Note: these HP/mana values and names are read live from the current `CaptainSO` assets and
> differ from the README's older "Siren Captain Schrei / Silas Deepbound, 30/40/50 HP" text —
> that copy predates this snapshot and should be treated as stale until reconciled with the
> canonical HLD v8 doc.

## 7. Meta-Progression

Three permanent upgrade tracks, each with a max level of 3, purchased with currency between
matches: **Health**, **Mana**, **Storage** (deck slot capacity). Reward drops are
performance-tiered by remaining HP; a loss still grants a partial (50%) material drop rather than
nothing.

## 8. Presentation & Feel Systems

### 8.1 Card VFX architecture
Two parallel, deliberately-separate systems handle a card's on-screen presentation:

- **`CardPresentationPlayer`** — data-driven per-card visuals. Each `CardSO` carries a list of
  `CardPresentationEntry` (spawn prefab, anchor point, caster filter, SFX cue). Prefabs that need
  multi-beat sequencing (travel, impact, explosion) implement `ICardPlayEffect` and drive
  themselves via a `CardEffectSpawnContext` handed to them once at spawn.
- **`CardVFXHandler`** — generic, system-wide combat feedback that fires for *every* card
  regardless of which one was played: damage numbers, light/medium/heavy hit feedback (sound +
  shake), win/loss sequences, mana/reaction pulses.

Migrating a card from the old inline switch-statement VFX (`CardVFXHandler.PlayCardVFX`) to the
new per-card system means populating its `PresentationEntries` and deleting its now-redundant
case from that switch so it doesn't double-fire. Cannonball, Pistol, and Torch were all migrated
this way this cycle — Cannonball/Pistol previously had a broken, disconnected legacy VFX path
that never actually played; Torch had a fully-written controller that had simply never been wired
up to its `CardSO` at all.

### 8.2 Screen Shake — standardized 5-level scale
Introduced `ScreenShake.Trigger(ScreenShakeLevel)` (`Systems/VFX/ScreenShake.cs`) so every card's
"how strong should this hit feel" decision maps to one of five tuned presets instead of each
controller inventing its own duration/amplitude/frequency numbers:

| Level | Duration | Amplitude | Frequency | Used by |
|---|---|---|---|---|
| 1 | 0.15s | 0.3 | 25 | (reserved for future light hits) |
| 2 | 0.20s | 0.6 | 28 | (reserved) |
| 3 | 0.25s | 1.0 | 30 | (reserved) |
| 4 | 0.30s | 1.6 | 35 | **Torch** (Gunpowder ignition), **Lightning** (strike) |
| 5 | 0.40s | 2.4 | 40 | **The Kraken** (attack peak) — strongest shake in the game |

This scale was introduced alongside a fix for a **project-wide screen-shake bug**: the Main
Camera's `MMWiggle` component had `PositionActive` disabled, which silently no-op'd every
`MMCameraShakeEvent` in the game (used by Whale Ram, Tidal Wave, and now everything above).
Enabling it initially re-exposed a *second*, pre-existing latent bug — a leftover `WigglePermitted
= true` with no time limit caused the camera to jitter constantly at idle instead of only during
an actual triggered shake. Both are now fixed: the camera sits dead-still at rest and shakes
correctly (and only) on a real trigger. Separately, the *generic* per-hit feedback
(`CardVFXHandler`'s light/medium/heavy hit) previously only shook a UI canvas container, not the
camera/world — an `MMF_CameraShake` feedback was added to all three so ship hits actually read as
impacts, not just UI wobble.

### 8.3 Cannonball / Pistol VFX (new this cycle)
Both share `CannonballVFXController`: spawn next to the caster's ship, fire immediately (no
wind-up delay), arc to the target's actual hit point (not wherever they spawned), then explode +
play a hit SFX + a small shake. Pistol reuses the same controller at a smaller scale, faster
travel, and quieter SFX than Cannonball.

### 8.4 Explosion sizing (`SpriteBoundsRecenter`)
The shared Explosion spritesheet's frames were trimmed to wildly inconsistent pixel sizes with
inconsistent pivots, which previously made explosions render oversized and off-center.
`SpriteBoundsRecenter` (`Systems/VFX/SpriteBoundsRecenter.cs`) re-centers the sprite on its spawn
point and normalizes it to a fixed max on-screen size every frame, regardless of which animation
frame is showing. Two size tiers exist: `Explosion.prefab` (0.9 units, used by Cannonball/Pistol)
and `Explosion Medium.prefab` (1.4 units, used by Torch's Gunpowder ignition).

### 8.5 Match Log
A scrolling, capped (60-entry) history panel of card plays, damage/heal events, and misc notes
(e.g. Locker's Return recovering cards), with hover-to-preview a card's description when the log
entry was a card play. Fed by a new `GameEventBus.OnMatchNote` event for one-off flavor lines that
don't already have a dedicated event to hang a log line off of.

### 8.6 Enemy card dismiss setting
The enemy's played card can either require a click to dismiss or auto-dismiss after a configurable
duration — a persisted `GameplaySettingsSO` toggle exposed in the Options panel.

### 8.7 UI layering fix
UI "layers" in this project are enforced via separate root Canvases with ascending `sortingOrder`
(`GameCanvas` → `MidUICanvas` → `DragCanvas` → `TopUICanvas`), **not** Sorting Layers/
`SortingGroup` — those only affect world-space Renderers and have no effect on
`CanvasRenderer`-based UI, which draws strictly by sibling order within a single Canvas. Prompts/
panels/cheats live on `MidUICanvas`; the hover tooltip and full-screen flash live on
`TopUICanvas` so they're never covered by a dragged card or panel.

## 9. Known Latent-Bug Fixes Worth Noting

- **`ShipStatusIndicator.IsVisible`** was gated on an optional decorative icon reference
  (`_icon != null && _icon.color.a > 0.01f`). Since neither ship's Gunpowder indicator has that
  icon wired, `IsVisible` always returned `false` — which would have silently prevented Torch's
  "was Gunpowder active" check from ever passing, i.e. the combo-ignition explosion added this
  cycle would never have fired. Fixed to key off the tracked status count instead
  (`_previousCount > 0`), independent of whether a decorative icon happens to be wired.

---

*This file is maintained alongside the codebase — update it when a change affects balance
numbers, adds a new mechanic, or fixes a bug significant enough that a future contributor would
otherwise waste time rediscovering it.*
