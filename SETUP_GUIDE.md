# Throne of Tides — Full Setup Guide
*Covers everything from an empty project clone to the current implemented state.*
*Written for a developer or AI agent to reproduce all changes from scratch.*

---

## Project Info
- **Engine:** Unity 6 (6000.3.13f1)
- **Scene:** `Assets/_Game/0. Scenes/NewArt_Match.unity`
- **Root Namespace:** `ThroneOfTides`
- **Feel (MoreMountains):** installed, provides `MMF_Player`
- **TextMeshPro:** installed

---

## Table of Contents
1. [CardTagSO ScriptableObject Assets](#1-cardtagso-scriptableobject-assets)
2. [CardSO Asset Updates — Costs & Tags](#2-cardso-asset-updates--costs--tags)
3. [Assembly Definition Update](#3-assembly-definition-update)
4. [ActiveEffectsBarSetup Editor Script](#4-activeeffectsbarsetup-editor-script)
5. [Run the Setup Script in Unity](#5-run-the-setup-script-in-unity)
6. [FeedbackPlayers — New MMF_Player GameObjects](#6-feedbackplayers--new-mmf_player-gameobjects)
7. [CardVFXHandler — Wire New Slots](#7-cardvfxhandler--wire-new-slots)
8. [Still To Do (Manual)](#8-still-to-do-manual)

---

## 1. CardTagSO ScriptableObject Assets

### What & Why
`CardTagSO` is a ScriptableObject with two fields:
- `_tagName` (string) — display label
- `_symbol` (Sprite) — icon shown in the TagsContainer on a card

Tags are stamped on `CardSO._tags` (a `List<CardTagSO>`) and drive both tooltip display and gameplay filtering.

### How — Write each file directly to disk

Create folder: `Assets/_Game/4. Data/Tags/`

For each asset below, create the `.asset` file **and** a `.asset.meta` file.

The script GUID is: `aaddcac2fe34f4649be08d42d4c3c109`  
*(This is the GUID of `CardTagSO.cs` — find it in `CardTagSO.cs.meta` if different in your project)*

#### Asset file template
```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: aaddcac2fe34f4649be08d42d4c3c109, type: 3}
  m_Name: Tag_TAGNAME
  m_EditorClassIdentifier: ThroneOfTides.Data::ThroneOfTides.Data.CardTagSO
  _tagName: TAGNAME
  _symbol: {fileID: 0}
```

#### Meta file template
```yaml
fileFormatVersion: 2
guid: GUID_HERE
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData:
  assetBundleName:
  assetBundleVariant:
```

#### Create all 8 assets

| Filename | `_tagName` | GUID to use in .meta |
|---|---|---|
| `Tag_Damage.asset` | `Damage` | `7784d32e451df36479e1e4ea1df98ac2` |
| `Tag_DOT.asset` | `DOT` | `a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4` |
| `Tag_ComboStack.asset` | `ComboStack` | `b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5` |
| `Tag_ComboActuator.asset` | `ComboActuator` | `c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6` |
| `Tag_CardAcquisition.asset` | `CardAcquisition` | `d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1` |
| `Tag_Reaction.asset` | `Reaction` | `e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2` |
| `Tag_Mana.asset` | `Mana` | `f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3` |
| `Tag_Heal.asset` | `Heal` | `a7b8c9d0e1f2a7b8c9d0e1f2a7b8c9d0` |

After creating all files, press **Ctrl+R** in Unity to refresh the Asset Database.

---

## 2. CardSO Asset Updates — Costs & Tags

### What & Why
Each existing `CardSO` needs three new cost fields and a tag list:
- `_manaCost` (int) — mana spent to play the card
- `_storageCost` (int) — deck storage cost
- `_hpCost` (int) — HP paid to play (usually 0)
- `_tags` — list of `CardTagSO` references

### How — Edit each .asset file

Find the line `_isEligibleAsActionPair: 0` in each `.asset` file and replace it (keep the original line) with the block below it.

#### Tag GUIDs reference
| Tag | GUID |
|---|---|
| Damage | `7784d32e451df36479e1e4ea1df98ac2` |
| DOT | `a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4` |
| ComboStack | `b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5` |
| ComboActuator | `c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6` |
| CardAcquisition | `d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1` |
| Reaction | `e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2` |
| Mana | `f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3` |
| Heal | `a7b8c9d0e1f2a7b8c9d0e1f2a7b8c9d0` |

#### Tag reference YAML format
```yaml
- {fileID: 11400000, guid: GUID_HERE, type: 2}
```

#### Per-card data

**Attacks folder** (`Assets/_Game/4. Data/Cards/Attacks/`)

`PistolCardSO.asset`
```yaml
  _isEligibleAsActionPair: 0
  _manaCost: 1
  _storageCost: 1
  _hpCost: 0
  _tags:
  - {fileID: 11400000, guid: 7784d32e451df36479e1e4ea1df98ac2, type: 2}
```

`GrapeShotCardSO.asset`
```yaml
  _isEligibleAsActionPair: 0
  _manaCost: 1
  _storageCost: 1
  _hpCost: 0
  _tags:
  - {fileID: 11400000, guid: 7784d32e451df36479e1e4ea1df98ac2, type: 2}
```

`ChainShotCardSO.asset`
```yaml
  _isEligibleAsActionPair: 0
  _manaCost: 2
  _storageCost: 2
  _hpCost: 0
  _tags:
  - {fileID: 11400000, guid: 7784d32e451df36479e1e4ea1df98ac2, type: 2}
```

`CannonBlastCardSO.asset`
```yaml
  _isEligibleAsActionPair: 0
  _manaCost: 3
  _storageCost: 3
  _hpCost: 0
  _tags:
  - {fileID: 11400000, guid: 7784d32e451df36479e1e4ea1df98ac2, type: 2}
```

`HailStormCardSO.asset`
```yaml
  _isEligibleAsActionPair: 0
  _manaCost: 2
  _storageCost: 2
  _hpCost: 0
  _tags:
  - {fileID: 11400000, guid: 7784d32e451df36479e1e4ea1df98ac2, type: 2}
  - {fileID: 11400000, guid: a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4, type: 2}
```

`TidalWaveCardSO.asset`
```yaml
  _isEligibleAsActionPair: 0
  _manaCost: 4
  _storageCost: 4
  _hpCost: 0
  _tags:
  - {fileID: 11400000, guid: 7784d32e451df36479e1e4ea1df98ac2, type: 2}
  - {fileID: 11400000, guid: a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4, type: 2}
```

`LightningStrikeCardSO.asset`
```yaml
  _isEligibleAsActionPair: 0
  _manaCost: 3
  _storageCost: 3
  _hpCost: 0
  _tags:
  - {fileID: 11400000, guid: 7784d32e451df36479e1e4ea1df98ac2, type: 2}
```

`WhirlpoolCardSO.asset`
```yaml
  _isEligibleAsActionPair: 0
  _manaCost: 2
  _storageCost: 2
  _hpCost: 0
  _tags:
  - {fileID: 11400000, guid: 7784d32e451df36479e1e4ea1df98ac2, type: 2}
  - {fileID: 11400000, guid: a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4, type: 2}
```

**Actions folder** (`Assets/_Game/4. Data/Cards/Actions/`)

`MonkeyGrabCardSO.asset`
```yaml
  _isEligibleAsActionPair: 0
  _manaCost: 1
  _storageCost: 1
  _hpCost: 0
  _tags:
  - {fileID: 11400000, guid: d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1, type: 2}
```

`LockerReturnCardSO.asset`
```yaml
  _isEligibleAsActionPair: 0
  _manaCost: 0
  _storageCost: 0
  _hpCost: 0
  _tags:
  - {fileID: 11400000, guid: d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1, type: 2}
```

`SirenSongCardSO.asset`
```yaml
  _isEligibleAsActionPair: 0
  _manaCost: 3
  _storageCost: 3
  _hpCost: 0
  _tags:
  - {fileID: 11400000, guid: f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3, type: 2}
  - {fileID: 11400000, guid: c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6, type: 2}
```

`HighSpiritsCardSO.asset`
```yaml
  _isEligibleAsActionPair: 0
  _manaCost: 2
  _storageCost: 2
  _hpCost: 0
  _tags:
  - {fileID: 11400000, guid: f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3, type: 2}
```

`DeadMansTurnCardSO.asset`
```yaml
  _isEligibleAsActionPair: 0
  _manaCost: 2
  _storageCost: 2
  _hpCost: 0
  _tags:
  - {fileID: 11400000, guid: e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2, type: 2}
```

`BloodForBloodCardSO.asset` *(create this file — see Section 8)*
```yaml
  _isEligibleAsActionPair: 0
  _manaCost: 0
  _storageCost: 0
  _hpCost: 2
  _tags:
  - {fileID: 11400000, guid: e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2, type: 2}
```

`ReconParrotCardSO.asset`
```yaml
  _isEligibleAsActionPair: 0
  _manaCost: 1
  _storageCost: 1
  _hpCost: 0
  _tags:
  - {fileID: 11400000, guid: b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5, type: 2}
```

`StolenWindCardSO.asset` *(create this file — see Section 8)*
```yaml
  _isEligibleAsActionPair: 0
  _manaCost: 2
  _storageCost: 2
  _hpCost: 0
  _tags:
  - {fileID: 11400000, guid: f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3, type: 2}
```

`RumCardSO.asset` *(create this file — see Section 8)*
```yaml
  _isEligibleAsActionPair: 0
  _manaCost: 0
  _storageCost: 0
  _hpCost: 2
  _tags:
  - {fileID: 11400000, guid: a7b8c9d0e1f2a7b8c9d0e1f2a7b8c9d0, type: 2}
  - {fileID: 11400000, guid: f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3, type: 2}
```

`TreasureChestCardSO.asset` *(create this file — see Section 8)*
```yaml
  _isEligibleAsActionPair: 0
  _manaCost: 2
  _storageCost: 2
  _hpCost: 0
  _tags:
  - {fileID: 11400000, guid: d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1, type: 2}
```

After editing, **Ctrl+R** to reimport.

---

## 3. Assembly Definition Update

### What & Why
`ThroneOfTides.Tools.asmdef` is the Editor-only assembly. The new setup script needs to reference `ActiveEffectsBar` (in `ThroneOfTides.UI`), `TextMeshProUGUI` (TMP), and `Image` (UnityEngine.UI) — so those assembly GUIDs must be listed as references.

### How

**File:** `Assets/_Game/2. Scripts/Tools/ThroneOfTides.Tools.asmdef`

Replace the entire file with:

```json
{
    "name": "ThroneOfTides.Tools",
    "rootNamespace": "ThroneOfTides.Tools",
    "references": [
        "GUID:5e07d9d5988b12f4e8e8626be9243ad4",
        "GUID:5850c61d938abf74f86e574834efcfb7",
        "GUID:4fdc6d8088fc0cf488f67b8cc59d7522",
        "GUID:b1daf4e383f172748ba002f2ed721f66",
        "GUID:6055be8ebefd69e48b49212b09b47b2f",
        "GUID:2bafac87e7f4b9b418d9448d219b01ab"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

The three new GUIDs added:
| GUID | Assembly |
|---|---|
| `b1daf4e383f172748ba002f2ed721f66` | ThroneOfTides.UI |
| `6055be8ebefd69e48b49212b09b47b2f` | Unity.TextMeshPro |
| `2bafac87e7f4b9b418d9448d219b01ab` | UnityEngine.UI |

> **Verify GUIDs:** If the project's TMP or UI GUID differs, open the `.asmdef.meta` files in `Packages/com.unity.textmeshpro` and `Packages/com.unity.ugui` to get the correct ones.

---

## 4. ActiveEffectsBarSetup Editor Script

### What & Why
A one-click Editor utility that programmatically builds the entire `ActiveEffectsBar` child hierarchy and wires all serialized references. Eliminates ~15 manual Create operations and ~12 drag-wire operations.

### How — Create the script

**File:** `Assets/_Game/2. Scripts/Tools/Editor/ActiveEffectsBarSetup.cs`

```csharp
// Assets/_Game/2. Scripts/Tools/Editor/ActiveEffectsBarSetup.cs
using TMPro;
using ThroneOfTides.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ThroneOfTides.Tools.EditorSetup
{
    public static class ActiveEffectsBarSetup
    {
        [MenuItem("ThroneOfTides/Setup/Build ActiveEffectsBar Hierarchy")]
        public static void BuildHierarchy()
        {
            var barGO = GameObject.Find("ActiveEffectsBar");
            if (barGO == null)
            {
                Debug.LogError("ActiveEffectsBar not found in scene.");
                return;
            }

            var bar = barGO.GetComponent<ActiveEffectsBar>();
            if (bar == null)
            {
                Debug.LogError("ActiveEffectsBar component missing.");
                return;
            }

            // ── Mana labels ────────────────────────────────────────────────
            var playerManaGO = GetOrCreate(barGO, "PlayerManaLabel");
            EnsureTMP(playerManaGO, "0 / 0");
            SetField(bar, "_playerManaLabel", playerManaGO.GetComponent<TextMeshProUGUI>());

            var enemyManaGO = GetOrCreate(barGO, "EnemyManaLabel");
            EnsureTMP(enemyManaGO, "0 / 0");
            SetField(bar, "_enemyManaLabel", enemyManaGO.GetComponent<TextMeshProUGUI>());

            // ── Combo ──────────────────────────────────────────────────────
            var comboGO = GetOrCreate(barGO, "ComboDisplay");
            SetField(bar, "_comboGroup", comboGO);
            comboGO.SetActive(false);
            var comboLabelGO = GetOrCreate(comboGO, "ComboStackLabel");
            EnsureTMP(comboLabelGO, "x0");
            SetField(bar, "_comboStackLabel", comboLabelGO.GetComponent<TextMeshProUGUI>());

            // ── DOT ────────────────────────────────────────────────────────
            var dotGO = GetOrCreate(barGO, "DOTDisplay");
            SetField(bar, "_dotGroup", dotGO);
            dotGO.SetActive(false);
            var dotLabelGO = GetOrCreate(dotGO, "DotLabel");
            EnsureTMP(dotLabelGO, "0 / 0t");
            SetField(bar, "_dotLabel", dotLabelGO.GetComponent<TextMeshProUGUI>());

            // ── Reactions ──────────────────────────────────────────────────
            var reactionsGO = GetOrCreate(barGO, "ReactionsBar");

            var dmtGO = GetOrCreate(reactionsGO, "DMT_Slot");
            EnsureImage(dmtGO, 48, 48, 0.3f);
            SetField(bar, "_deadMansTurnGroup", dmtGO);
            dmtGO.SetActive(false);
            var dmtLabelGO = GetOrCreate(dmtGO, "DeadMansTurnChargeLabel");
            EnsureTMP(dmtLabelGO, "0");
            SetField(bar, "_deadMansTurnChargeLabel", dmtLabelGO.GetComponent<TextMeshProUGUI>());

            var bfbGO = GetOrCreate(reactionsGO, "BFB_Slot");
            EnsureImage(bfbGO, 48, 48, 0.3f);
            SetField(bar, "_bloodForBloodGroup", bfbGO);
            bfbGO.SetActive(false);
            var bfbLabelGO = GetOrCreate(bfbGO, "BloodForBloodChargeLabel");
            EnsureTMP(bfbLabelGO, "0");
            SetField(bar, "_bloodForBloodChargeLabel", bfbLabelGO.GetComponent<TextMeshProUGUI>());

            EditorUtility.SetDirty(bar);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(barGO.scene);
            Debug.Log("ActiveEffectsBar hierarchy built and wired.");
        }

        // ── Helpers ────────────────────────────────────────────────────────

        static GameObject GetOrCreate(GameObject parent, string name)
        {
            var existing = parent.transform.Find(name);
            if (existing != null) return existing.gameObject;

            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            if (parent.GetComponentInParent<Canvas>() != null)
                go.AddComponent<RectTransform>();
            return go;
        }

        static void EnsureTMP(GameObject go, string defaultText)
        {
            if (go.GetComponent<TextMeshProUGUI>() != null) return;
            var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 30);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = defaultText;
            tmp.fontSize = 18;
        }

        static void EnsureImage(GameObject go, float w, float h, float alpha)
        {
            if (go.GetComponent<Image>() != null) return;
            var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(w, h);
            var img = go.AddComponent<Image>();
            var c = img.color;
            c.a = alpha;
            img.color = c;
        }

        static void SetField(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogWarning($"Field '{fieldName}' not found on {target.name}");
            }
        }
    }
}
```

> **Important namespace note:** The namespace must be `ThroneOfTides.Tools.EditorSetup` — NOT `ThroneOfTides.Tools.Editor`. Using `Editor` as the last segment conflicts with the `UnityEditor.Editor` class and causes CS0118 compile errors throughout the project.

---

## 5. Run the Setup Script in Unity

### Prerequisites in scene
Before running, verify `ActiveEffectsBar` exists in the scene:
- `GameCanvas > HUD > ActiveEffectsBar` 
- It must have the `ActiveEffectsBar` MonoBehaviour component attached

If the GameObject doesn't exist yet:
1. In the Hierarchy, right-click `HUD` → Create Empty Child → name it `ActiveEffectsBar`
2. In the Inspector → Add Component → search `Active Effects Bar` → add it

### Run
1. Open the scene `NewArt_Match`
2. Unity menu bar → **ThroneOfTides** → **Setup** → **Build ActiveEffectsBar Hierarchy**
3. Check Console for: `"ActiveEffectsBar hierarchy built and wired."`

### Expected result in Hierarchy
```
ActiveEffectsBar
├── PlayerManaLabel        (TextMeshProUGUI "0 / 0", 200×30)
├── EnemyManaLabel         (TextMeshProUGUI "0 / 0", 200×30)
├── ComboDisplay           (inactive)
│   └── ComboStackLabel    (TextMeshProUGUI "x0")
├── DOTDisplay             (inactive)
│   └── DotLabel           (TextMeshProUGUI "0 / 0t")
└── ReactionsBar
    ├── DMT_Slot           (Image 48×48, alpha 0.3, inactive)
    │   └── DeadMansTurnChargeLabel  (TextMeshProUGUI "0")
    └── BFB_Slot           (Image 48×48, alpha 0.3, inactive)
        └── BloodForBloodChargeLabel (TextMeshProUGUI "0")
```

All `ActiveEffectsBar` component fields should now be wired (verify by clicking `ActiveEffectsBar` GO and checking Inspector — no None references in the script fields).

**Save the scene:** Ctrl+S

---

## 6. FeedbackPlayers — New MMF_Player GameObjects

### What & Why
`CardVFXHandler` (on `Managers/Visuals/VFX`) has four serialized `MMF_Player` fields that were declared in code but had no scene object assigned:
- `_feedbackManaGain`
- `_feedbackManaSpend`
- `_feedbackReactionCharged`
- `_feedbackReactionFired`

Each needs a child GameObject under `FeedbackPlayers` with an `MMF_Player` component.

### How — In the Unity Editor Hierarchy

1. In the Hierarchy, locate `FeedbackPlayers` (root-level object, sibling of Managers, EnemyShip, etc.)
2. Right-click `FeedbackPlayers` → **Create Empty Child**
3. Name it `FB_ManaGain`
4. With `FB_ManaGain` selected, in Inspector → **Add Component** → search `MMF Player` → add it
5. Right-click `FB_ManaGain` in Hierarchy → **Duplicate** (Ctrl+D)
6. Rename duplicate to `FB_ManaSpend` (press F2 in Hierarchy to rename)
7. Duplicate again → rename to `FB_ReactionCharged`
8. Duplicate again → rename to `FB_ReactionFired`

Result — FeedbackPlayers should contain (at the bottom, after existing FB_ objects):
```
FeedbackPlayers
├── FB_LightHit         (existing)
├── FB_MediumHit        (existing)
├── ...all existing...
├── FB_Win              (existing)
├── FB_Loss             (existing)
├── FB_ManaGain         ← NEW (MMF_Player)
├── FB_ManaSpend        ← NEW (MMF_Player)
├── FB_ReactionCharged  ← NEW (MMF_Player)
└── FB_ReactionFired    ← NEW (MMF_Player)
```

---

## 7. CardVFXHandler — Wire New Slots

### What & Why
Connect the four new `FB_` GameObjects to their corresponding fields on `CardVFXHandler`.

### How

1. In Hierarchy, select `Managers > Visuals > VFX`
2. In Inspector, find the **Card VFX Handler (Script)** component
3. Scroll down to the **FEEL — Mana** section
4. Click the ⊙ (circle picker) next to **Feedback Mana Gain** → in the picker window, click **Scene** tab → double-click `FB_ManaGain`
5. Click the ⊙ next to **Feedback Mana Spend** → double-click `FB_ManaSpend`
6. Scroll to **FEEL — Reactions** section
7. Click the ⊙ next to the first **Feedback Reaction...** (Charged) → double-click `FB_ReactionCharged`
8. Click the ⊙ next to the second **Feedback Reaction...** (Fired) → double-click `FB_ReactionFired`

**Save the scene:** Ctrl+S

---

## 8. Still To Do (Manual)

These items were not completed and need to be done manually:

---

### 8a. Create 4 Missing CardSO Assets

The following cards have their cost/tag data defined in Section 2 but the `.asset` files don't exist yet.

Use the CardSO template below. The `CardSO` script GUID is `c8f548b34a6fce246aadd09ea35a84d5`.

#### CardSO asset template
```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: c8f548b34a6fce246aadd09ea35a84d5, type: 3}
  m_Name: CARD_NAME_CardSO
  m_EditorClassIdentifier: ThroneOfTides.Data::ThroneOfTides.Data.CardSO
  _name: CARD_DISPLAY_NAME
  _cardType: CARD_TYPE_INT
  _description: DESCRIPTION
  _damage: 0
  _comboDamage: 0
  _comboStackBonus: 0
  _comboPartner: {fileID: 0}
  _dotDamagePerTurn: 0
  _dotDuration: 0
  _actionEffect: {fileID: 0}
  _art: {fileID: 0}
  _cardTypeSymbol: {fileID: 0}
  _cardArtAnimator: {fileID: 0}
  _manaCost: MANA_COST
  _storageCost: STORAGE_COST
  _hpCost: HP_COST
  _isEligibleAsActionPair: 0
  _tags:
  - {fileID: 11400000, guid: TAG_GUID, type: 2}
```

`_cardType` values: `0` = Attack, `1` = Defense, `2` = Utility, `3` = DOT, `4` = Action

#### Cards to create

**`RumCardSO.asset`** — path: `Assets/_Game/4. Data/Cards/Actions/`
- `_name`: Rum
- `_cardType`: 2
- `_description`: What doesn't kill you makes you stronger. Pay 2 HP, gain 2 Mana.
- `_manaCost`: 0, `_storageCost`: 0, `_hpCost`: 2
- Tags: Heal + Mana

**`TreasureChestCardSO.asset`** — path: `Assets/_Game/4. Data/Cards/Actions/`
- `_name`: Treasure Chest
- `_cardType`: 2
- `_description`: Fortune favors the prepared. Draw 2 cards.
- `_manaCost`: 2, `_storageCost`: 2, `_hpCost`: 0
- Tags: CardAcquisition

**`BloodForBloodCardSO.asset`** — path: `Assets/_Game/4. Data/Cards/Actions/`
- `_name`: Blood for Blood
- `_cardType`: 2
- `_description`: Pain is currency. Pay 2 HP to deal 4 damage when triggered.
- `_manaCost`: 0, `_storageCost`: 0, `_hpCost`: 2
- Tags: Reaction

**`StolenWindCardSO.asset`** — path: `Assets/_Game/4. Data/Cards/Actions/`
- `_name`: Stolen Wind
- `_cardType`: 2
- `_description`: Ride their momentum. Gain 2 Mana when the enemy plays a card.
- `_manaCost`: 2, `_storageCost`: 2, `_hpCost`: 0
- Tags: Mana

---

### 8b. Add `_reactionSlotTransform` to HandLayoutManager

**File:** `Assets/_Game/2. Scripts/UI/HandLayoutManager.cs`

In the serialized fields block (around line 43, near `_deckTransform`), add:
```csharp
[SerializeField] private RectTransform _reactionSlotTransform;
```

Then update `AnimateReactionDraw()` (currently line ~387) to use it:
```csharp
IEnumerator IHandLayoutManager.AnimateReactionDraw(ICard card)
{
    var cardSO = card as CardSO;
    if (cardSO == null) yield break;

    if (_reactionSlotTransform != null)
    {
        // TODO: tween card from deck position to _reactionSlotTransform
        // e.g. use DOTween or a coroutine arc similar to _feedbackDeckDraw
    }

    GameEventBus.FireCardDrawn(card);
    yield return null;
}
```

**In the scene:** Select the GameObject that has `HandLayoutManager`, then drag `ReactionsBar` (child of `ActiveEffectsBar`) into the new `_reactionSlotTransform` slot.

---

### 8c. Modify CardView Prefab — ManaCostBadge

**Prefab:** `Assets/_Game/1. Prefabs/Cards/Card_UI.prefab` (open by double-clicking)

1. In the prefab Hierarchy, right-click the card art root → **Create Empty Child** → name `ManaCostBadge`
2. `RectTransform`: Width=32, Height=32, Anchor=top-left, Pivot=(0.5, 0.5), Pos=(16, -16)
3. Add `Image` component → set Color to `#2255CC` (blue), Alpha=255
4. Right-click `ManaCostBadge` → Create Empty Child → name `ManaCostLabel`
   - Add `TextMeshProUGUI`: text=`"1"`, font size=16, style=Bold, color=white, alignment=Center

**Wire in CardView script** (after adding fields — see 8e):
- `_manaCostBadge` → `ManaCostBadge` GameObject
- `_manaCostLabel` → `ManaCostLabel` TMP component

---

### 8d. Modify CardView Prefab — HPCostBadge

Same prefab, sibling of ManaCostBadge:

1. Create Empty Child → name `HPCostBadge`
2. `RectTransform`: Width=32, Height=32, Anchor=bottom-left, Pivot=(0.5, 0.5), Pos=(16, 16)
3. Add `Image` → Color `#CC2222` (red), Alpha=255
4. Create Empty Child → name `HPCostLabel`
   - Add `TextMeshProUGUI`: text=`"2"`, font size=16, Bold, white, Center
5. **Set `HPCostBadge` inactive** in the Inspector (uncheck the checkbox top-left of the GO)

**Wire:** `_hpCostBadge`, `_hpCostLabel`

---

### 8e. Modify CardView Prefab — TagsContainer

Same prefab:

1. Create Empty Child → name `TagsContainer`
2. `RectTransform`: Anchor=bottom-stretch (left=0, right=0), Height=20, AnchorMin=(0,0), AnchorMax=(1,0), Pivot=(0.5,0), PosY=2
3. Add `HorizontalLayoutGroup`:
   - Child Alignment: Upper Left
   - Spacing: 2
   - Control Child Size: Width=false, Height=false
   - Child Force Expand: Width=false, Height=false
4. Add `ContentSizeFitter`:
   - Horizontal Fit: Preferred Size
   - Vertical Fit: Min Size

**Wire:** `_tagsContainer` → TagsContainer Transform

---

### 8f. Create TagIcon Prefab

1. In Project window: `Assets/_Game/1. Prefabs/Cards/` → right-click → **Create** → **Prefab** (or drag an empty GO into the folder)
2. Name it `TagIcon`
3. Open the prefab
4. Root `RectTransform`: Width=18, Height=18
5. Add `Image` component (leave sprite as None — assigned at runtime)
6. Add `TagTooltipTrigger` component (check `Assets/_Game/2. Scripts/UI/` for this script; if it doesn't exist, create a stub):

```csharp
// TagTooltipTrigger.cs
using UnityEngine;
using UnityEngine.EventSystems;

namespace ThroneOfTides.UI
{
    public class TagTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private string _tooltipText;

        public void SetTooltip(string text) => _tooltipText = text;

        public void OnPointerEnter(PointerEventData eventData)
        {
            // TODO: show tooltip with _tooltipText
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // TODO: hide tooltip
        }
    }
}
```

**Wire in CardView:** `_tagIconPrefab` → the `TagIcon.prefab` asset (drag from Project window)

---

### 8g. Add New Fields to CardView.cs

**File:** `Assets/_Game/2. Scripts/UI/CardView.cs`

Check if these serialized fields already exist. If not, add them in the `[SerializeField]` block:

```csharp
[Header("Cost Badges")]
[SerializeField] private GameObject        _manaCostBadge;
[SerializeField] private TextMeshProUGUI   _manaCostLabel;
[SerializeField] private GameObject        _hpCostBadge;
[SerializeField] private TextMeshProUGUI   _hpCostLabel;

[Header("Tags")]
[SerializeField] private Transform         _tagsContainer;
[SerializeField] private TagIcon           _tagIconPrefab;
```

And in the method that binds a card (likely `SetCard(CardSO card)` or `Bind(CardSO card)`), add:

```csharp
// Mana cost badge
if (_manaCostBadge != null)
{
    _manaCostLabel.text = card.ManaCost.ToString();
}

// HP cost badge
if (_hpCostBadge != null)
{
    bool hasHPCost = card.HPCost > 0;
    _hpCostBadge.SetActive(hasHPCost);
    if (hasHPCost) _hpCostLabel.text = card.HPCost.ToString();
}

// Tags
if (_tagsContainer != null && _tagIconPrefab != null)
{
    foreach (Transform child in _tagsContainer)
        Destroy(child.gameObject);

    foreach (var tag in card.Tags)
    {
        var icon = Instantiate(_tagIconPrefab, _tagsContainer);
        icon.SetTag(tag);
    }
}
```

*(Adjust property names to match whatever `CardSO` exposes — check `CardSO.cs` for the public getters.)*

---

## Summary Checklist

| # | Task | Status |
|---|---|---|
| 1 | 8 CardTagSO assets created | ✅ Done |
| 2 | All existing CardSO assets updated with costs + tags | ✅ Done |
| 3 | ThroneOfTides.Tools.asmdef updated | ✅ Done |
| 4 | ActiveEffectsBarSetup.cs created | ✅ Done |
| 5 | Setup script run — ActiveEffectsBar hierarchy built + wired | ✅ Done |
| 6 | FB_ManaGain/Spend/ReactionCharged/Fired created under FeedbackPlayers | ✅ Done |
| 7 | CardVFXHandler — 4 new FEEL slots wired | ✅ Done |
| 8a | 4 missing CardSO assets (Rum, TreasureChest, BloodForBlood, StolenWind) | ❌ TODO |
| 8b | `_reactionSlotTransform` field added to HandLayoutManager + wired | ❌ TODO |
| 8c | CardView prefab — ManaCostBadge | ❌ TODO |
| 8d | CardView prefab — HPCostBadge | ❌ TODO |
| 8e | CardView prefab — TagsContainer | ❌ TODO |
| 8f | TagIcon prefab created | ❌ TODO |
| 8g | CardView.cs — new fields + binding logic | ❌ TODO |
