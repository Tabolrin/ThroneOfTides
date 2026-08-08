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

### Update — scene placement completed

Unity MCP connectivity was fine in the follow-up session; the placement described below is now done and live-verified.

- `ActiveEffectsBar_Player` was added as a child of `PlayerSidePanel` (under `GameCanvas/CanvasShakeContainer/PlayerSidePanel`), `ActiveEffectsBar_Enemy` as a child of `EnemySidePanel`. Both panels are plain (non-prefab) GameObjects, so this sidesteps the stripped-prefab-instance risk entirely — nothing was inserted into the nested prefab that holds the HP/mana bars themselves.
- Each got `EffectsContainer` / `ReactionsContainer` children with a `HorizontalLayoutGroup` (spacing 6, no child-control/force-expand) and got assigned to `_effectsContainer` / `_reactionsContainer`.
- `_badgePrefab` → `EffectBadgeView.prefab`, `_palette` → `Effect Symbol Palette SO.asset`, `_deadMansTurnIcon` → `DeadMansTurnArt.png`, `_counterGaleIcon` → `Turnado Card Art.png` (Counter Gale's actual card art asset, despite the filename).
- Verified live in Play mode by calling `GameEventBus.FireShipStatusCountChanged` / `FireReactionCharged` directly (Unity MCP `execute_code`): badges spawned in the correct container for both Player and Enemy, and despawned correctly when the count dropped to 0. No errors from any of the new scripts.
- Unrelated pre-existing errors observed in the same Play session (not caused by this change, different files entirely): a `NullReferenceException` in `GameBootstrapper.OnEnable` (input actions ordering) and repeated `NullReferenceException`s in `MMSMPlaylistManager.Update` (Feel asset audio playlist). Worth a look separately if not already known.

**Positioning follow-up (same session):** the first placement (bars at panel-local Y = -140, tucked directly under the mana row) put the badges directly behind an unrelated bottom-left/top-right icon cluster (deck-order preview icons), invisible in-game even though they were spawning correctly. Verified this with an actual screenshot via Unity MCP (`manage_camera` → `screenshot`), not just by reading transform data. Fixed by moving both bars into the open water area just outside their panel's top/bottom edge instead of layering them inside the tightly-packed panel: `ActiveEffectsBar_Player` local position `(0, 280, 0)`, `ActiveEffectsBar_Enemy` local position `(0, -280, 0)` (mirrored, since the enemy panel is top-anchored and its clear space is below it). Also widened the gap between each bar's `EffectsContainer` and `ReactionsContainer` from local Y `-30` to `-75` — at `-30` the two rows overlapped almost entirely (badges are 60 units tall), making a 2-badge situation look like a single row. Re-verified visually after both fixes: two distinct, unobstructed rows per side. Final position is a reasonable default, not a pixel-perfect art placement — worth a look in the actual Editor to confirm it reads well against final art/animation.

This closes out the one remaining item from Part A — the persistent effect badge system is now fully recovered and working end-to-end.

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
| **Persistent effect badges (dynamic spawn/destroy system)** | ✅ Fully recovered — scripts, prefab, and scene placement all done and live-verified (Part A above) |

### The one other thing worth a decision, not a fix
`Captain_Loreley`/`Captain_Nurgle` now coexist with `Captain_Rumboat` (three Captains total) rather than replacing older placeholders — this matches what I'd flagged as needing your call, and it looks like the resolution was simply "keep all three." No action needed unless that's not actually what you want.

---

## What to do next

1. Get Unity MCP reachable from this session (client config pointing at the right server/transport), or do the 5-step scene placement above manually.
2. Once `ActiveEffectsBar` is placed and wired, do the live verification pass (play each status-granting card, confirm badges spawn/despawn on the correct ship).
3. Nothing else on the original recovery list needs attention — it's all confirmed present and correct as of this audit.
