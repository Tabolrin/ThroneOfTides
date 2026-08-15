# ThroneOfTides - Script Architecture

Scope: `Assets/_Game/2. Scripts/**` (88 C# files). Root namespace `ThroneOfTides.*`.
This document is auto-generated from a read-through of every script in this folder and is meant as an onboarding/reference map, not a spec. It reflects the code as of 2026-07-11 (branch `PeerLive`).

## 1. Folder Layout

| Folder | Files | Purpose |
|---|---|---|
| `App/` | 1 | Match scene composition root (`GameBootstrapper`) |
| `Core/` | 11 | Assembly-agnostic contracts: interfaces, enums, structs, generated Input Actions |
| `Data/` | 12 | ScriptableObject definitions (cards, decks, captains, config, progression, inventory) |
| `Data/Effects/` | 9 | Concrete `ActionEffectSO` subclasses, one per Action/Reaction card |
| `Systems/` | 10 | Runtime game engine: state, turn flow, combat resolution, AI |
| `Systems/States/` | 4 | Turn state machine states |
| `Systems/VFX/` | 9 | Presentation-layer VFX controllers driven by `GameEventBus` |
| `UI/` | 23 | HUD, hand/card views, drag-drop, Port (meta-progression) screens |
| `Tools/` | 9 | Editor-only: build pipeline, custom inspectors, property drawers, EditorWindows, overlays |

There's no visible `.asmdef` split - namespaces (`Core/Data/Systems/UI/App/Tools`) are the only boundary. `Tools/` scripts use `UnityEditor` APIs directly with no `#if UNITY_EDITOR` guards, so they must be excluded from player builds by folder convention or an editor asmdef not visible in this scan - worth double-checking.

## 2. Core Layer (`Core/`)

Pure contracts - no `MonoBehaviour`, no `ScriptableObject`:

- **`ICard`** - read-only card data contract (name, type, damage, combo/DOT stats, costs). Implemented by `CardSO`.
- **`ICardEffectContext`** - the interface Action/Reaction effect SOs program against (damage, heal, mana, hand manipulation, discard/snapshot retrieval). Implemented by `Systems.CardEffectContext`.
- **`IHandLayoutManager`** - abstracts hand-view animation from game logic. Implemented by `UI.HandLayoutManager`.
- **`CardType`** enum - `Weapon, Combo, Action, DOT, Reaction`.
- **`DamageTarget`** enum - `Player, Enemy`.
- **`DotEffect`** struct - target/damagePerTurn/turnsRemaining, `[Serializable]` for inspector drawing.
- **`ReactionType`** enum - `DeadMansTurn, BloodForBlood`.
- **`TurnPhase`** enum - 10 phases (Draw/MainPhase/EnemyDraw/Win/Lose/etc). Only `Draw` and `EnemyDraw` are actually fired via `GameEventBus.FireTurnPhaseChanged` in the current code - several declared phases (`MainPhase`, `DefenseWindow`, `PlayerDefense`, `EnemyEnd`) look unused.
- **`UpgradeType`** enum - `MaxMana, MaxHP, MaxStorage`.
- **`GameEventBus`** - static global pub/sub hub (see §5).
- **`ThroneOfTidesInputActions`** - auto-generated Input System wrapper (Gameplay: EndTurn/Pause; UI: unused "New action" stub).
- **`Core/.cs`** - a stray file literally named `.cs`. Confirmed byte-for-byte identical (497 lines, same generated `ThroneOfTidesInputActions` class) to `Systems/ThroneOfTidesInputActions.cs`, except it declares `namespace ThroneOfTides.Core` instead of `ThroneOfTides.Systems`. Almost certainly an accidental emit from the Input Actions asset's "C# Class File" generation path being pointed at (or cleared to) the `Core` folder with an empty filename. It compiles (the differing namespace avoids a duplicate-type collision) but nothing references `ThroneOfTides.Core.ThroneOfTidesInputActions` - dead code, safe to delete when you're ready (not touched here per your instruction not to modify files).

## 3. Data Layer (`Data/`)

All `ScriptableObject`s, `CreateAssetMenu`-driven, designer-facing:

- **`CardSO : ScriptableObject, ICard`** - a single class represents every card type via one flat field set; type-irrelevant fields just sit unused/zero (e.g. `_comboPartner` only matters for Combo cards). An `ActionEffectSO` reference wires in behavior for Action/Reaction cards.
- **`ActionEffectSO`** (abstract) - `Execute(ICardEffectContext)`. Strategy-pattern base for the 9 concrete effects in `Data/Effects/`.
- **`CaptainSO`** - enemy identity: HP, deck, level reward, and a 13-entry AI weight table. `GetWeightForCard` maps a `CardSO` to a float weight using **name-string matching** for special cards ("The Kraken", "Boarding Party", "Torch", etc.) - fragile, see §9.1.
- **`CardTypePaletteSO`** - per-`CardType` visual theme (banner color + icon); consumed by `CardView`, `CardInspectView`, `PortCardRow`, `PortInventoryCard`.
- **`DeckDefinitionSO`** - list of `(CardSO, Count)` entries; `BuildDeck()` expands to a flat `List<CardSO>`.
- **`GameConfigSO`** - global tunables (StartingHP, MaxHandSize, StartingMaxMana, deck thresholds, enemy think-time range).
- **`LevelRewardSO`** - HP-tiered material rewards (High >20 / Mid 10–20 / Low <10 HP); loss = 50% of win reward, no card reward on loss.
- **`PlayerInventory`** - the meta-progression save object: card collection, active deck ref, Rum/Shipwreck currencies, power-ups, 3 upgrade levels (Hull/Cargo/Mana). Has `Reset()`. Acts as save data but persists only as a Unity asset - see §9.5.
- **`ProgressionSO`** (file on disk is `PorgressionSO.cs` - filename typo, class name itself is correct) - 3 boolean level-beaten flags gating level unlock.
- **`PowerUpSO`** - simple name/icon/description data holder; no consumption system present in the scanned scripts.
- **`UpgradeSO`** - per-level cost/value arrays (`ValuePerLevel`, `RumCostPerLevel`, `ShipwreckCostPerLevel`), `MaxLevel`, generic across the 3 `UpgradeType`s.

### `Data/Effects/` - Strategy pattern, one file each

| Effect | Behavior |
|---|---|
| `BloodForBloodEffectSO` | adds a Blood-for-Blood reaction charge |
| `DeadMansTurnEffectSO` | adds a Dead Man's Turn reaction charge |
| `HighSpiritsEffectSO` | +1 max mana |
| `LockerReturnEffectSO` | retrieve N cards from discard |
| `MonkeyGrabEffectSO` | steal enemy card |
| `ReconParrotEffectSO` | reveal enemy hand - UI wiring is a TODO, currently just `Debug.Log` |
| `RumEffectSO` | heal |
| `SirenSongEffectSO` | sets an unblockable-next-attack flag |
| `EssencePlunderEffectSO` | steals enemy mana; HP cost paid by `CombatResolver` beforehand |
| `TreasureChestEffectSO` | return 3 from deck snapshot + 50% coin-flip for +1 mana + draw 1 |

## 4. Systems Layer - the game engine

- **`GameState`** - the mutable authoritative match state: HP, mana (player/enemy), hands/decks, combo stack, DOT list, reaction charges, siren/unblockable flags, turn-play flags, discard piles, original deck snapshot (for Treasure Chest). Every mutator also fires the matching `GameEventBus` event - `GameState` is both source of truth and event emitter, no separate presenter step. `IsGameOver()`/`GetWinner()` check HP ≤ 0 or empty-deck-and-empty-hand for either side.
- **`Deck`** - shuffled `List<CardSO>`, `Draw()`, `ReturnCard()` (bottom-insert, used by Locker Return / Treasure Chest), state enum (`Normal/Low/Empty`) with a change event.
- **`Hand`** - capped `List<CardSO>`, exposes both `ICard` (cross-layer) and `CardSO` (internal) views, state enum (`Normal/Full/Empty`) with change event.
- **`CombatResolver`** - dispatches by `CardType`: Combo (stack/resolve via `GameState`), DOT (adds `DotEffect`), Action/Reaction (builds a `CardEffectContext` and calls `card.ActionEffect.Execute`), Weapon (name-switch for special weapons: "Ram the Hull" HP cost only, "Chain Shot" discards random enemy card, "Tidal Wave" breaks enemy combo). HP cost for any card is paid up-front in `ResolvePlayerCard` before dispatch. Uses a settable `Func<bool> _secondaryDrawCallback` so effect SOs can trigger a second draw without needing a reference to `TurnCoordinator` (avoids an upward dependency) - injected per-play via `SetSecondaryDrawCallback`.
- **`CardEffectContext : ICardEffectContext`** - the concrete adapter Effects operate against; wraps `GameState` + `IHandLayoutManager` + the secondary-draw delegate.
- **`EnemyAI`** - weighted-random card picker; filters by playability (mana, one damage-card + one action-card per turn, reaction cards never AI-played), then does a cumulative-weight roll using `CaptainSO.GetWeightForCard`.
- **`TurnCoordinator : MonoBehaviour`** - the orchestration hub bridging `GameState`/`TurnStateMachine`/`EnemyAI`/`CombatResolver`/`IHandLayoutManager`. Owns: draw logic (`TryDrawCard`, `TryDrawCardSecondary` - ignores the once-per-turn flag, used by Treasure Chest), `HandleCardPlayed` (validate → spend mana → resolve → apply damage → check game-over), the full enemy-turn coroutine (`EnemyTurnRoutine`: DOT ticking, enemy draw, AI pick, enemy attack resolution) including a Kraken-vs-Kraken special-case prompt and a Dead Man's Turn / Blood for Blood reaction prompt flow (`OnShowReactionPrompt` delegate wired to `DeadMansTurnPrompt` UI by `GameBootstrapper`).
- **State machine** (`Systems/States/`): `IState` (Enter/Tick/Exit) → `TurnStateMachine` holds `PlayerTurnState`, `EnemyTurnState`, `GameOverState`. `PlayerTurnState.Enter()` resets turn-play flags/mana and fires `TurnPhase.Draw`. `EnemyTurnState.Enter()` flips `IsPlayerTurn` and calls `GameState.NotifyEnemyTurnReady()`, delegating actual execution to `TurnCoordinator.OnEnemyTurnReady` (coroutines need a MonoBehaviour host, so the state machine itself stays plain C#). `TurnStateMachine.SetCoroutineRunner` is a vestigial no-op - `GameBootstrapper` calls it but it does nothing, since `TurnCoordinator` runs its own coroutines directly.
- **`GameSession`** (static) - cross-scene transient carrier for `SelectedCaptain`/`SelectedLevelIndex`, set by `LevelSelectManager`, read by `GameBootstrapper`.

### VFX subsystem (`Systems/VFX/`)

Pure presentation, entirely event-driven off `GameEventBus`, decoupled from game logic:

- **`CardVFXHandler`** - central VFX router; subscribes to ~13 bus events, has a switch-on-card-name in `PlayCardVFX` to route to prefab spawns / FEEL (MoreMountains.Feedbacks) triggers / DOTween cannonball arcs. Delegates multi-phase creature sequences (Kraken, Siren) to dedicated controllers.
- **`KrakenVFXController`**, **`SirenVFXController`**, **`LightningVFXController`**, **`HailStormVFXController`** - near-identical DOTween `Sequence`-based rise/attack/sink or fade-in/strike/fade-out patterns, each with an `Inject(...)` method for scene-only refs (canvas rect, camera, particle systems), since prefabs can't hold scene references. Each exposes `OnAttackMoment`/`OnStrikeMoment`/`OnSequenceEnd` events. Noticeably duplicated positioning/injection/reset boilerplate across all four - see §9.6.
- **`KrakenTentacleAnimator`** - isolated wind-up/strike rotation tween, signed-angle math to avoid 0/360 wraparound.
- Note: the Hailstorm controller's file is `HailSotrmVFXController.cs` (typo "Sotrm") though the class itself is named `HailstormVFXController`.
- **`Oscillator`** - idle ship bob/drift/sway; live-editable via `ShipOscillatorOverlay` (Tools).
- **`VFXSpawnPosition`** - marker component (enum-typed) for scene VFX anchor points; visualized by `VFXSpawnPointOverlay` (Tools).
- **`VFXTester`** - dev-only keybound (Space/S/L/H) manual trigger for the 4 controllers; explicitly commented as "remove from builds."

## 5. Event Backbone - `GameEventBus`

Static class, ~20 events + matching `Fire*` methods + `ClearAllListeners()` (called from `GameBootstrapper.OnDestroy`). This is the sole cross-cutting channel between `Systems` (which fires) and `UI`/`VFX` (which subscribe). Categories: Turn, Card, Combat, DOT, Mana, Reactions, Action Cards, Creature VFX Sync, Match.

Being static/global means no per-match scoping - it relies entirely on `ClearAllListeners()` running on scene teardown. If a subscriber forgets to unsubscribe in `OnDisable`/`OnDestroy`, or teardown is skipped (e.g. exception mid-transition), stale delegates could persist into the next match.

## 6. App / Composition Root

- **`GameBootstrapper`** (`App/`) - Match scene entry point. Resolves the active captain (`GameSession` or fallback), applies upgrade bonuses from `PlayerInventory` to HP/Mana, builds player/enemy `Deck`s, constructs `GameState` → `TurnStateMachine` → `CombatResolver` → `EnemyAI`, wires `TurnCoordinator.Initialise(...)`, deals opening hands (reaction cards animate to the charge bar, not the hand), subscribes to ~8 bus events, owns `RefreshHUD`/win-loss panel triggering. This is the largest manual dependency-injection point in the codebase - reasonable at this scale, but it means `GameBootstrapper` has intimate knowledge of nearly every other system.
- **`PortManager`** - physically under `UI/PortManager.cs` but namespaced `ThroneOfTides.App`. Meta-progression scene composition root; wires `PortUpgradePanel`, `PortDeckEditor`, `PortInventoryView`. Save is currently editor-only via `AssetDatabase.SaveAssets`, with an explicit TODO for JSON-based build persistence.

Minor inconsistency: `LevelSelectManager` is physically in `UI/` but namespaced `ThroneOfTides.Systems`; `PortManager` is physically in `UI/` but namespaced `ThroneOfTides.App`. Folder location and namespace don't consistently match.

## 7. UI Layer (23 files, largest folder)

**Match HUD** - `GameHUD` (HP/mana/turn text + DOTween fill bars), `ActiveEffectsBar` (mana/DMT/BFB/combo indicators, bus-driven), `TooltipController`, `DeadMansTurnPrompt` (reaction choice modal), `ConcedeButton`, `ResultsPanel` (win/loss screen - grants rewards, updates `ProgressionSO`/`PlayerInventory` directly).

**Card rendering/interaction** - `CardView` (per-card visual, right-click → inspect), `CardDragHandler` (`IBeginDragHandler`/`IDragHandler`/`IEndDragHandler`, reparents to a drag canvas), `PlayZoneHandler` (`IDropHandler`, fires `GameEventBus.FireCardPlayed`), `CardInspectController`/`CardInspectView` - **two parallel implementations** of "inspect a card" (the Controller animates the live `CardView` instance to screen-center; the View is a separately populated overlay driven directly by `CardSO`) - worth confirming which is actually wired into the live prefab, see §9.3. `DeckClickHandler` (static event on deck-art click to draw). `HandLayoutManager` - the largest UI file, implements `IHandLayoutManager`; manages player/enemy hand instantiation, fan-layout math, opening-deal animation, manual-draw arc animation, enemy-card-play travel animation, and a reaction-draw stub (explicit TODO pending a ReactionsBar in-scene).

**Scene navigation** - `MainMenuManager`, `LevelSelectManager` (progression-gated level buttons, sets `GameSession` then loads the Match scene).

**Port (meta) screens** - `PortManager` (root), `PortDeckEditor` (deck list + storage-cap enforcement, mutates `DeckDefinitionSO` directly at runtime), `PortInventoryView` (filterable collection grid), `PortInventoryCard`, `PortCardRow`, `PortUpgradePanel`/`PortUpgradeNode` (3-upgrade purchase UI, pip-based level display).

## 8. Tools Layer (Editor-only, 9 files)

- **`ThroneOfTidesBuildPipeline`** - `IPreprocessBuildWithReport`/`IPostprocessBuildWithReport`; runs `CardAssetSearch.ValidateAllData()` before every build (can hard-block via `BuildFailedException`); 4 menu-driven build variants (Dev/Release/Playtest-config-override/WebGL) with keyboard shortcuts.
- **`CardAssetSearch`** - canonical validation logic reused by both the build pipeline and manual menu validation, plus generic `LoadAll<T>()`/`FindDecksContaining()` asset-search utilities.
- **`CardSOInspector`**, **`DeckDefinitionSOInspector`** - custom `Editor`s with tinted backgrounds, inline validation warnings, art previews.
- **`CardEntryDrawer`**, **`DotEffectDrawer`** - compact single-row `PropertyDrawer`s.
- **`DeckBuilderWindow`** - full `EditorWindow` two-pane deck editor (mirrors `PortDeckEditor`'s runtime functionality, but for editor-time asset editing) with search/type filter, live stats bar, undo-aware save.
- **`SceneSwitcherTool`** - `[InitializeOnLoad]` toolbar dropdown scanning `_Game/7. Scenes` for `.unity` files, refreshed via an `AssetPostprocessor` hook (not polling).
- **`ShipOscillatorOverlay`**, **`VFXSpawnPointOverlay`** - Scene-view `IMGUIOverlay`/`ITransientOverlay` live-tuning panels + gizmo drawing for `Oscillator` and `VFXSpawnPosition` respectively.

## 9. Data Flow Example - playing a card

1. **`CardSO`** assets (Data layer) are compiled into a **`DeckDefinitionSO`**, expanded into a **`Deck`** by `GameBootstrapper` at match start.
2. `Deck.Draw()` moves a card into **`Hand`**; `HandLayoutManager` (UI) instantiates a **`CardView`** and animates it into the fan layout.
3. Player drags the card via **`CardDragHandler`**; dropping on **`PlayZoneHandler`** fires `GameEventBus.FireCardPlayed`.
4. **`TurnCoordinator.HandleCardPlayed`** validates the play, spends mana on **`GameState`**, and calls **`CombatResolver`**.
5. `CombatResolver` dispatches by `CardType` - direct damage on `GameState`, or `ActionEffectSO.Execute(ICardEffectContext)` for Action/Reaction cards (via `CardEffectContext`).
6. Every `GameState` mutation fires a `GameEventBus` event.
7. **UI** (`GameHUD`, `ActiveEffectsBar`, etc.) and **VFX** (`CardVFXHandler` and friends) independently subscribe to those events and update/animate - neither layer talks to `CombatResolver` or `GameState` directly.
8. `TurnCoordinator` checks `GameState.IsGameOver()`; if true, **`TurnStateMachine`** transitions to **`GameOverState`**, and `GameBootstrapper` shows **`ResultsPanel`**, which writes rewards back into **`PlayerInventory`**/**`ProgressionSO`**.

## 10. Singletons / Static Hubs

No `DontDestroyOnLoad`-based persistent singleton was found anywhere in the codebase. Instead:

1. **`GameEventBus`** - fully static, no instance state, the central cross-cutting hub (see §5). Depended on by nearly every gameplay and presentation script.
2. **`GameSession`** - static class holding `SelectedCaptain`/`SelectedLevelIndex`, explicitly documented in-code as "transient session data passed between scenes - not persistent, resets on game launch." Written by `LevelSelectManager`, read by `GameBootstrapper` and `ResultsPanel`.
3. **`DeckClickHandler.OnDeckClicked`** - a `public static Action` acting as a lightweight de-facto singleton signal from the deck-visual click handler to `GameBootstrapper`/`TurnCoordinator.TryDrawCard()`.
4. **`CardInspectController.Instance`** - a classic MonoBehaviour singleton (`static Instance`, set in `Awake`), scoped to the Match scene only. No duplicate-guard or explicit teardown was found, though no case where that matters currently exists in the code.
5. **`CardAssetSearch`** - static editor-time utility/service-locator (`LoadAll<T>()`, `ValidateAllData()`, `FindDecksContaining()`); canonical shared validation logic used by both `DeckBuilderWindow` and `ThroneOfTidesBuildPipeline`.

`GameState`, `TurnCoordinator`, `CombatResolver`, `EnemyAI`, and `TurnStateMachine` are all explicitly **not** singletons - each is a fresh instance built per-match inside `GameBootstrapper.Start()`, which (along with `PortManager` for the Port scene) is the real composition-root "hub" of each scene.

## 11. Cross-Cutting Observations

1. **Card-name string coupling (biggest smell)** - special-case behavior for specific named cards ("The Kraken", "Dead Man's Turn", "Blood for Blood", "Chain Shot", "Tidal Wave", "Ram the Hull", "Torch") is scattered across `CaptainSO.GetWeightForCard`, `CombatResolver.ResolveWeapon`, `TurnCoordinator` (3 places), and `CardVFXHandler.PlayCardVFX`. Nothing centralizes "this card has special logic" - a typo or rename in any `CardSO.Name` field silently breaks AI weighting, combat resolution, or VFX with no compile error.
2. **`GameEventBus` as global static** - simple and effective for a single-match-at-a-time game, but depends on disciplined unsubscription and `ClearAllListeners()` on teardown; no scoping mechanism prevents cross-match leakage if teardown is skipped.
3. **Dual card-inspect implementations** (`CardInspectController` vs `CardInspectView`) - likely one is legacy/unused.
4. **`TurnStateMachine.SetCoroutineRunner`** is a dead no-op still being called from `GameBootstrapper`.
5. **Persistence is asset-based, not file-based** - `PlayerInventory`, `DeckDefinitionSO`, `ProgressionSO` are ScriptableObjects mutated at runtime and saved via `AssetDatabase.SaveAssets()` (editor-only). This persists in-editor and within a single build session, but **not** across app restarts in a real build. Both `PortManager.SaveDeck()` and the reward-writing path in `ResultsPanel` carry explicit TODOs for JSON-based save data before shipping.
6. **VFX controller duplication** - `KrakenVFXController`, `SirenVFXController`, `LightningVFXController`, `HailStormVFXController` share near-identical `Inject`/positioning/reset boilerplate; a shared base class could remove the duplication without hurting each `BuildSequence()`'s readability.
7. **Overall layering is consistent**: Core (contracts) → Data (SO definitions + Strategy-pattern effects) → Systems (engine, event-emitting) → UI/VFX (event-consuming presentation). `GameBootstrapper`/`PortManager` are the only composition roots, keeping DI centralized rather than scattered.

---
*Generated from a full read of all 88 scripts in `Assets/_Game/2. Scripts/`. Third-party plugins (DOTween, Feel/MMFeedbacks) are treated as external dependencies and are out of scope.*
