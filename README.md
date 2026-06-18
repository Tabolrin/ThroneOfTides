# Throne of Tides
**A strategic turn-based pirate card battler - single player, PC**
> Built in Unity 2D URP · C# · In Development (Vertical Slice)

---

## About
Throne of Tides is a single-player turn-based card battler set in a world 
of mythic pirate naval warfare. You command a ship against a roster of 
legendary AI captains, managing a fixed deck across tense turn-based 
battles. Between stages, plunder rewards, upgrade your ship, and sharpen 
your arsenal to conquer the seas and claim your throne.

Each turn you spend mana to play cards - a damage card, an action card, 
or both - with at least one card required per turn. Your hand and deck 
are a single shared resource. Let both run dry and your ship sinks.

I am the lead designer and programmer on this project, working alongside 
a producer/co-designer and an artist. Every system was designed and 
documented before implementation, with a full GDD, HLD, and balance 
spreadsheet maintained across multiple versions.

---

## Design Documents
📄 [Game Design Document (GDD) - v6](<https://docs.google.com/document/d/1MkV1CaZWJsu4zOP6ZjAu_O4Zo4t17NGckKYAUY4Jhxk/edit?usp=sharing>)  
📄 [High-Level Design Document (HLD) - v8](<https://docs.google.com/document/d/133qzpSa4RO27y_zbFs6qLWrUmpmaZXtODrBtMcZaaso/edit?usp=sharing>)  
📊 [Balance Spreadsheet](<https://docs.google.com/spreadsheets/d/1qG3uNH2hfbM_wV13tYVdbw4M_pffa1z3ndoEvEb-3go/edit?usp=sharing>)  
📋 [Master Asset List](<https://miro.com/app/board/uXjVHdt4P7U=/?moveToWidget=3458764669833709512&cot=14>)

---

## Key Design Features

**Card System**
- 40-card starter deck filling all 80 base deck slots - curated spread of 
  weapon, action, reaction, combo, and DOT cards
- Every card carries two independent costs: an in-match **play cost** 
  (mana) and a **deck slot cost** (deck construction) - separating 
  moment-to-moment resource management from deck-building strategy
- Action cards (Siren Song, Recon Parrot, Stolen Wind, etc.) pair with 
  damage cards for amplified or supportive turns
- Reaction cards (Dead Man's Turn, Counter Gale) trigger outside the 
  normal mana economy, resolving only when the enemy attacks
- Combo system: Gunpowder Barrel stacks ComboStackCount each consecutive 
  turn. Torch resolves total damage (8 + ((stack - 1) × 2)). Any 
  interruption resets the stack
- DOT effects (Hail Storm, Whirlpool) tick at the start of the affected 
  player's turn, capped at 2 simultaneous effects with no duplicate types
- High-stakes ultimates: The Kraken deals 15 unblockable damage but costs 
  3 HP + 33% of earned materials

**Mana System**
- Base 3 mana per turn, fully resetting each turn with no carry-over
- No fixed per-turn card limit - mana is the sole constraint on how many 
  cards a player can play
- Multiple stacking mana-growth paths: Port upgrades, High Spirits 
  (permanent in-match mana gain), and Treasure Chest's 50/50 mana proc 
  all compound across a single match
- Stolen Wind introduces short-term mana disruption - stealing from the 
  enemy's current turn at a small HP cost, with a floor to prevent 
  fully locking out enemy archetypes

**Turn Structure**
- Draw → Main → Defense Window → End Phase, mirrored for the AI
- Player must play at least 1 card per turn - passing is not an option
- Reaction cards create a sub-state inside the Defense Window: the game 
  pauses mid-enemy-attack, and the player chooses which reaction card to 
  use (if any) or takes the hit

**AI Architecture**
- Strategy pattern - each archetype implements IAIStrategy
- Three captains, three archetypes, each designed to implicitly teach 
  counterplay through encounter design:

| Captain | Archetype | HP | Mana/Turn | Counter-Play Taught |
|---|---|---|---|---|
| Captain Rumboat | Aggressive | 30 | 2 | Play defensively, outlast the chaos |
| Siren Captain Schrei | Defensive / Reserved | 40 | 2 | Apply pressure, force blocks early |
| Silas Deepbound | Control / Hoarder | 50 | 3 | Disrupt hand size, strike before the Kraken |

- AI decisions driven by per-captain weight tables evaluated against 
  mana availability and live game state
- Enemy decks support illegal card counts by design for unique challenge 
  profiles - Schrei's five Siren Songs and Deepbound's five Krakens both 
  break standard deck-building rules deliberately

**Meta-Progression & Economy**
- Three currencies: Rum (Hull Reinforcement → +5 max HP per tier), 
  Shipwrecks (Expanded Cargo Hold → +10 deck slots per tier), and a 
  third currency for Mana Upgrade (+1 base mana per tier)
- Up to 5 upgrade tiers per axis, cost scaling per level
- Reward drops are performance-tiered (High / Mid / Low HP remaining)
- Win: full material drop + guaranteed card reward
- Loss: 50% material drop (rounded down), no card reward - defeat is 
  never a dead end
- Deck management in The Port: minimum 40 cards, base maximum 80 slots 
  (expandable), free card swaps between stages

> **Scoped out of the current milestone:** Power-ups and the Spent Shot 
> card are designed and documented but cut from this vertical slice 
> build due to the production timeframe. Both remain on the roadmap.

---

## Art Direction

Throne of Tides uses a **flat vector illustration style** - bold, 
uniform outlines, minimal flat-color shading (2-3 tones per object), and 
simplified geometric forms favoring smooth curves over rendered detail. 
The palette leans toward muted, weathered tones - worn browns, slate 
greys, deep teals - reinforcing the naval, aged-leather mood of the 
world. This approach prioritizes fast, at-a-glance legibility (critical 
for a card game) while remaining achievable for a small two-person art 
team without requiring pixel-precise or painterly rendering per asset.

*Note: the project's earlier pixel-art pipeline (Pixel Perfect Camera, 
Point filtering) has been superseded by this vector approach - rendering 
setup has been updated accordingly.*

---

## Technical Architecture

- **Engine:** Unity 6, 2D URP - flat vector illustration sprites, 
  SpriteRenderer
- **Language:** C#
- **Input:** Unity New Input System - touch and mouse share Pointer code path
- **Card system:** Data-driven via ScriptableObjects (CardSO), with fields 
  for play cost, deck slot cost, special effect, and reaction-card flag. 
  DeckDefinitionSO tracks both card count and total slot usage for deck 
  construction
- **Turn loop:** Explicit state machine - DRAW → MAIN_PHASE → 
  DEFENSE_WINDOW → END_PHASE → ENEMY_DRAW → ENEMY_MAIN → 
  PLAYER_DEFENSE → ENEMY_END → WIN / LOSE
- **Mana system:** Per-entity ManaState tracking current and max mana, 
  resetting at the start of each entity's turn
- **AI system:** Strategy pattern. Weight table lookups against live game 
  state and mana budget. Weights and decks defined per captain in CaptainSO
- **Event bus:** Cross-system communication with no singletons, covering 
  card play, damage, combo, mana, DOT, HP, and match state
- **Assembly definitions:** Five asmdef files with enforced one-way 
  dependency direction (Core → Data → Systems → UI → Tools)
- **DOT system:** Active effects tracked as DOTEffectData struct lists 
  on the affected ship - damage per tick, ticks remaining, source card, 
  capped at 2 simultaneous effects

---

## Status
Vertical slice in active development - v6.0 GDD, v8.0 HLD.  
Three captains, full 40-card starter deck, mana economy, combo system, 
DOT system, and meta-progression all designed and documented. Power-ups 
and Spent Shot are cut from this milestone for scope reasons.  
Playable build in progress.

---

## Contact
👤 [LinkedIn](<https://www.linkedin.com/in/peerml/?skipRedirect=true>)  
🎮 [itch.io](<https://tabolrin.itch.io/>)
