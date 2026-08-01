# Card Cheat Panel

Playtest-only debug tool: a top-right toggle button opens a scrollable list of every card in
the game's Card Registry. Clicking a name adds that card to the player's hand instantly.

## Files

| File | Role |
|---|---|
| [`UI/CardCheatPanel.cs`](2.%20Scripts/UI/CardCheatPanel.cs) | Toggle button, lazy list population, add-to-hand logic. |
| [`UI/CardCheatEntryButton.cs`](2.%20Scripts/UI/CardCheatEntryButton.cs) | One pooled row in the scroll list (name + click). |
| [`Data/CardDatabaseSO.cs`](2.%20Scripts/Data/CardDatabaseSO.cs) | Added `AllCards` — the registry could previously only look up by id, not enumerate. |
| [`App/GameBootstrapper.cs`](2.%20Scripts/App/GameBootstrapper.cs) | Composition root — wires `CardCheatPanel.Initialise(...)`, same pattern as `CheatsPanel`. |
| `Assets/_Game/4. Data/CardDatabaseSO.asset` | The registry asset itself (created this session, populated with all 19 cards). |
| `Assets/_Game/3. Prefabs/UI/Cheats/CardCheatEntryButton.prefab` | The pooled row prefab. |

## How it interacts with the registry

`CardDatabaseSO` is a flat `List<CardSO>` (`_allCards`), designer-populated by dragging every
`CardSO` asset into the list in the Inspector. It's used elsewhere for id→card lookups
(`GetById`, used by `PlayerInventory`'s save/load). This feature added a second read path:

```csharp
public IReadOnlyList<CardSO> AllCards => _allCards.AsReadOnly();
```

`CardCheatPanel` holds a reference to this asset (wired via `GameBootstrapper`) and iterates
`AllCards` once, on the first time the panel is opened, to build the scroll list. It doesn't
re-populate on subsequent opens — the registry's contents don't change at runtime, so there's
nothing to refresh.

## How "add to hand" works

Mirrors the existing pattern found in `CardEffectContext.AddCardToPlayerHand` — adding a card
is a two-part operation, both required:

```csharp
_gameState.PlayerHand.AddCard(card, _maxHandSize);   // data model
_handLayout.AddCardToPlayerHand(card);                // visual CardView
GameEventBus.FireCardDrawn(card);                     // notify (currently no subscribers)
```

The panel checks `_gameState.PlayerHand.Count >= _maxHandSize` **before** attempting the add,
so a full hand is rejected with a clear log line instead of the silent no-op `Hand.AddCard`
would otherwise produce.

## Adding a new debug feature to this panel family

- **A new individual cheat button** (like `CheatsPanel`'s HP/mana buttons): add a button to
  `CheatsPanel`'s scene hierarchy, wire its `OnClick` to a new `GameState.Cheat*` method (or a
  new method on `CheatsPanel` if it needs shared UI state), call `RefreshAfterCheat()` — no
  script changes needed beyond the new method.
- **A new scrollable/data-driven debug list** (like this one): follow `CardCheatPanel`'s shape —
  a toggle button + panel root + pooled `ObjectPool<T>` for rows, populated from whatever data
  source you're browsing. Keep debug-build gating (`if (!Debug.isDebugBuild) { ... return; }`)
  in `Awake()` and wire it from `GameBootstrapper` the same way.

## Known limitation

Cards added via this cheat bypass mana cost, draw-once-per-turn, and turn-phase rules — by
design, since it's a cheat. It does **not** bypass the hand-size cap; that's respected
intentionally, since other systems (`HandLayoutManager`'s fan layout, `CanDraw()`) assume the
cap is never violated.
