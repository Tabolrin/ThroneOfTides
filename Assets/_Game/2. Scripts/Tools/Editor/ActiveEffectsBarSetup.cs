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
