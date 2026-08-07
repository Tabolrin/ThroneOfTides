# Persistent Effect Badges — What Was Done, What's Left, and Full Recovery Status

## TL;DR

Almost everything from the original data-loss incident has been recovered — either by independent work on your end or by directly following the earlier `SESSION_CHANGES_LOST.md` audit. I re-verified every item against the actual current repo state (not assumptions) before writing this. **The one system that was still genuinely gone is the persistent effect badge/bar system** — this document covers exactly what I rebuilt for it, what's still outstanding, and the full final status of everything else.

---

## Part A — Persistent Effect Badges: What I Did This Session

### The problem
You remembered correctly, and I had it wrong initially: the `ActiveEffectsBar` system currently in the repo is a **different, simpler reimplementation** than what we originally built — it uses a hand-placed `List<StatusBadge>` (fixed `GameObject Root` + `TextMeshProUGUI CountLabel` per status, toggled active/inactive) instead of the original **dynamic spawn-on-demand** design. On top of that, I discovered `ActiveEffectsBar` isn't even placed anywhere in the Match scene right now — the active-effects display is currently just absent from the match entirely, not merely simplified.

### What we originally built (recap)
- `ShipStatusType` (Core enum) — one identity per persistent per-ship status.
- `GameEventBus.OnShipStatusCountChanged(ShipStatusType, DamageTarget, int)` — the single event both the HUD badge and the world-space indicator subscribe to.
- `EffectSymbolPaletteSO` — one shared asset mapping `ShipStatusType → Sprite`.
- `ShipStatusIndicator` — world-space fader, one per status-sprite-per-ship.
- `ActiveEffectsBar` — one instance per ship, spawning/destroying badge instances from a single shared prefab on demand, nothing pre-placed.
- `GameHUD` mana fill.

**Confirmed still intact and correct** (did not need touching): `ShipStatusType.cs`, `GameEventBus.OnShipStatusCountChanged`, `DotEffect.Source`, `CardSO.StatusType`, `ShipStatusIndicator.cs` (even picked up a nice independent upgrade — an `_autoRevealOnActivate` flag to sync reveal timing to a thrown-projectile VFX's impact), and `GameHUD`'s mana fill. None of these needed to be rebuilt.

**Confirmed gone and rebuilt this session:**

1. **`Assets/_Game/2. Scripts/Data/EffectSymbolPaletteSO.cs`** — recreated from scratch. There's an orphaned data asset, `Effect Symbol Palette SO.asset`, still sitting in the project with its original 5 icon assignments (Gunpowder, Whirlpool, Hail Storm, High Spirits, Siren Song) intact but pointing at a script that no longer existed. I rebuilt the script matching its exact expected field structure (`_icons`, a `List<IconEntry>` with public `Type`/`Icon` fields) **and gave the new script's `.meta` file the same guid the orphaned asset already references** (`f0393e3e816e1514fbbd47b093669516`). That means the asset's 5 already-configured icons should re-link automatically — nothing needs re-assigning.

2. **`Assets/_Game/2. Scripts/UI/EffectBadgeView.cs`** — new script, the generic badge component. One `Setup(Sprite icon, int? count)` method, used for both status badges and reaction-charge badges. New guid, nothing to preserve here since nothing referenced it before.

3. **`Assets/_Game/3. Prefabs/UI/EffectBadgeView.prefab`** — new prefab, built to match `PersistentBadge.prefab`'s existing visual style (same badge-background sprite, same count-label font/size/alignment settings) but with a swappable icon `Image` instead of `PersistentBadge`'s hardcoded Siren-only sprite.

4. **`Assets/_Game/2. Scripts/UI/ActiveEffectsBar.cs`** — rewritten back to the dynamic instantiate/destroy pattern (`Dictionary<ShipStatusType, EffectBadgeView>` / `Dictionary<ReactionType, EffectBadgeView>`, spawned into `_effectsContainer`/`_reactionsContainer` via `_badgePrefab` + `_palette`, destroyed when a count hits zero). Kept the current, already-correct per-side `DamageTarget`-aware event signatures (`OnReactionCharged(ReactionType, DamageTarget, int)` etc.) rather than reverting those. One thing I had to catch and fix mid-build: reactions (Dead Man's Turn / Counter Gale) aren't `ShipStatusType`s, so they can't come from the palette lookup — added two direct `Sprite` fields (`_deadMansTurnIcon`, `_counterGaleIcon`) instead of the wrong palette-based stub I first wrote.

### What's still outstanding for this system

**Scene placement.** `ActiveEffectsBar` needs to actually be placed in `Match.unity` — it isn't there at all right now. I attempted this via Unity MCP but hit a live connectivity issue this session (the MCP server my session talks to and the one Unity's bridge connected to appear to be two separate processes not sharing state — see your MCP client config for how `unityMCP` is set up, whether it should point at the HTTP server Unity's bridge auto-starts on `127.0.0.1:8080` rather than spawning its own stdio process). I did not hand-author this part into the scene YAML directly, on purpose — the HP/mana bars this needs to sit near live inside a **nested prefab instance** with "stripped" component references, meaning I can't read their real positions from the scene file text, and blindly inserting new UI siblings into that hierarchy risks corrupting the prefab-instance override records in a way I can't verify without visual feedback.

**Once Unity MCP is reachable (or if you do it manually in the meantime), here's exactly what's needed:**

1. Under `GameCanvas`, create two empty UI GameObjects — one near the player's HP/mana area, one near the enemy's. Name them e.g. `ActiveEffectsBar_Player` / `ActiveEffectsBar_Enemy`.
2. Add the `ActiveEffectsBar` component to each.
   - Player instance: `_side = Player`.
   - Enemy instance: `_side = Enemy`.
3. On each, add two empty child `RectTransform`s (`EffectsContainer`, `ReactionsContainer` — a horizontal or vertical layout group on each is fine, your call on the exact look) and assign them to `_effectsContainer` / `_reactionsContainer`.
4. On both instances, assign:
   - `_badgePrefab` → `EffectBadgeView.prefab`
   - `_palette` → `Effect Symbol Palette SO.asset`
   - `_deadMansTurnIcon` → Dead Man's Turn's card art (or any icon you prefer)
   - `_counterGaleIcon` → Counter Gale's card art (or any icon you prefer)
5. Compile, enter Play mode, and trigger each status (play Gunpowder Barrel, Whirlpool, High Spirits, Siren Song, charge a reaction) to confirm a badge spawns and disappears correctly on both sides.

I was not able to complete or verify step 5 this session due to the connection issue above — once Unity MCP is reachable, I can do the placement and the live verification directly rather than you having to check it by hand.

---

## Part B — Final Recovery Status of Everything Else

I re-verified every item from the original `SESSION_CHANGES_LOST.md` against the live repo state before writing this (not from memory). Here's where things actually stand now:

| Item | Status |
|---|---|
| Rum card + effect wiring | ✅ Recovered — `RumCardSO.asset` exists |
| Treasure Chest card + effect | ✅ Recovered — `TreasureChestCardSO.asset` exists |
| `CardId.TidalWave` enum entry | ✅ Recovered — appended at the end, exactly as originally fixed (even the code comment matches) |
| `CardDatabase.asset` registry | ✅ Recovered — 21 entries (up from the broken 19), matching the full intended roster |
| Per-Captain HP/Mana override | ✅ Recovered — `CaptainSO._maxMana` present, read by `GameBootstrapper` |
| AI weight extensions (mana-steal, reaction weights, `ChooseReaction`) | ✅ Recovered — `WeightActionManaSteal`, `WeightReactionCounter`, `ChooseReaction` all present. Rebuilt slightly differently (routed through a generic `EffectRole` enum instead of direct `CardId` switch cases) — a reasonable, arguably cleaner, evolution of the original design |
| Per-side reaction charge system | ✅ Recovered — `PlayerDeadMansTurnCharges`/`EnemyDeadMansTurnCharges`/etc. all present, `GameEventBus` reaction events carry `DamageTarget` |
| Captain Loreley / Nurgle + AI tuning | ✅ Recovered — both assets exist alongside `Captain_Rumboat` |
| Build pipeline per-build-type deck profiles | ✅ Recovered — `BuildDeckProfileSO.cs` and `BuildDeckProfiles.asset` both present |
| Port scene scroll rect fixes | ✅ Recovered — `DeckContent` has `VerticalLayoutGroup` (spacing 6, matching the original fix exactly) + `ContentSizeFitter`; `PortInventoryCard.prefab` (now relocated to `.../UI/Port/`) has `LayoutElement` components |
| `ResultsPanel.Awake()` first-loss bug | ✅ Recovered — the destructive `gameObject.SetActive(false);` line is gone from `Awake()` |
| Cheats panel → game-over check | ✅ Recovered — `OnCheatApplied` → `TurnCoordinator.CheckGameOver()` wired in `GameBootstrapper` |
| Card descriptions (Chain Shot/Torch/Counter Gale/High Spirits) | Independently rewritten with new flavor text (different from both the original terse versions and my session's fixes) — this is deliberate design work on your end, not a gap, nothing to redo |
| **Persistent effect badges (dynamic spawn/destroy system)** | ⚠️ **Scripts + prefab rebuilt this session (Part A above); scene placement still outstanding** |

### The one other thing worth a decision, not a fix
`Captain_Loreley`/`Captain_Nurgle` now coexist with `Captain_Rumboat` (three Captains total) rather than replacing older placeholders — this matches what I'd flagged as needing your call, and it looks like the resolution was simply "keep all three." No action needed unless that's not actually what you want.

---

## What to do next

1. Get Unity MCP reachable from this session (client config pointing at the right server/transport), or do the 5-step scene placement above manually.
2. Once `ActiveEffectsBar` is placed and wired, do the live verification pass (play each status-granting card, confirm badges spawn/despawn on the correct ship).
3. Nothing else on the original recovery list needs attention — it's all confirmed present and correct as of this audit.
