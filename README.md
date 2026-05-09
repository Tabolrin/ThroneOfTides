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

Each turn you must play at least one card - a damage card, an action card, 
or both. Your hand and deck are a single shared resource. Let both run dry 
and your ship sinks.

I am the sole designer and programmer on this project. Every system was 
designed and documented before implementation, with a full GDD, HLD, and 
balance spreadsheet maintained across four major versions.

---

## Design Documents
📄 [Game Design Document (GDD) — v4](<https://docs.google.com/document/d/1MkV1CaZWJsu4zOP6ZjAu_O4Zo4t17NGckKYAUY4Jhxk/edit?usp=sharing>)  
📄 [High-Level Design Document (HLD) — v5](<https://docs.google.com/document/d/133qzpSa4RO27y_zbFs6qLWrUmpmaZXtODrBtMcZaaso/edit?usp=sharing>)  
📊 [Balance Spreadsheet](<https://docs.google.com/spreadsheets/d/1qG3uNH2hfbM_wV13tYVdbw4M_pffa1z3ndoEvEb-3go/edit?usp=sharing>)  
📋 [Master Asset List](<https://miro.com/app/board/uXjVHdt4P7U=/?moveToWidget=3458764669833709512&cot=14>)

---

## Key Design Features

**Card System**
- 43-card starter deck - curated spread of weapon, action, combo, and DOT cards
- Weapon cards deal damage and double as defensive blocks
- Action cards (Siren Song, Dead Man's Turn, Monkey Grab, etc.) pair with 
  damage cards for amplified or reactive turns
- Combo system: Gunpowder Barrel stacks ComboStackCount each consecutive 
  turn. Torch resolves total damage (8 + ((stack - 1) × 2)). any 
  interruption resets the stack
- DOT effects (Hail Storm, Whirlpool) stack simultaneously and tick at the 
  start of the affected player's Draw Phase
- High-stakes ultimates: The Kraken deals 15 unblockable damage but costs 
  3 HP + 33% of earned materials

**Turn Structure**
- Draw → Main → Defense Window → End Phase, mirrored for the AI
- Player must play at least 1 card per turn - passing is not an option
- Dead Man's Turn creates a reactive sub-state: game pauses mid-enemy-attack, 
  player chooses to negate or take the hit
- Power-ups (Grab Some Grub, Calipso's Aid, Shiver Their Timbers) are 
  declared during Main Phase - one of each type per turn, permanently consumed

**AI Architecture**
- Strategy pattern - each archetype implements IAIStrategy
- Three captains, three archetypes, each designed to implicitly teach 
  counterplay through encounter design:

| Captain | Archetype | HP | Counter-Play Taught |
|---|---|---|---|
| Captain Rumboat | Aggressive | 30 | Play defensively, outlast the chaos |
| Siren Captain Schrei | Defensive / Reserved | 40 | Apply pressure, force blocks early |
| Silas Deepbound | Control / Hoarder | 50 | Disrupt hand size, strike before the Kraken |

- AI decisions driven by per-captain weight tables across 12 action categories
- Enemy decks support illegal card counts by design for unique challenge profiles

**Meta-Progression & Economy**
- Dual currencies: Rum (Hull Reinforcement → +5 max HP per tier) and 
  Shipwrecks (Expanded Cargo Hold → +3 max deck size per tier)
- 5 upgrade tiers per axis. cost scales per level
- Reward drops are performance-tiered (High / Mid / Low HP remaining)
- Win: full material drop + guaranteed card reward
- Loss: 50% material drop, no card reward
- Deck management in The Port: min 40 cards, max 45 (expandable), 
  free card swaps between stages

---

## Technical Architecture

- **Engine:** Unity 2D URP - pixel art sprites, Pixel Perfect Camera
- **Language:** C#
- **Input:** Unity New Input System - touch and mouse share Pointer code path
- **Card system:** Data-driven via ScriptableObjects (CardSO). 
  DeckDefinitionSO for deck construction with card counts
- **Turn loop:** Explicit state machine - DRAW → MAIN_PHASE → 
  DEFENSE_WINDOW → END_PHASE → ENEMY_DRAW → ENEMY_MAIN → 
  PLAYER_DEFENSE → ENEMY_END → WIN / LOSE
- **AI system:** Strategy pattern. weight table lookups against live game 
  state. weights and decks defined per captain in CaptainSO
- **Event bus:** Cross-system communication with no singletons. 
  16 named events covering card play, damage, combo, DOT, HP, and match state
- **Assembly definitions:** Five asmdef files with enforced dependency direction
- **DOT system:** Active effects tracked as DOTEffectData struct lists 
  on the affected ship - damage per tick, ticks remaining, source card

---

## Status
Vertical slice in active development - v4.0 GDD, v5.0 HLD.  
Three captains, full 43-card deck, combo system, DOT system, meta-progression, 
power-ups, and tutorial all designed and documented.  
Playable build in progress.

---

## Contact
👤 [LinkedIn](<www.linkedin.com/in/peerml>)  
🎮 [itch.io](<https://tabolrin.itch.io/>)  
