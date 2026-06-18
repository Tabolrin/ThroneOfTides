using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HierarchyTools.Runtime;

namespace HierarchyTools.Editor
{
    /// <summary>
    /// Editor popup for selecting and applying a visual symbol to the selected GameObjects.
    /// </summary>
    public class HierarchySymbolPicker : PopupWindowContent
    {
        private const int Columns = 8;
        private const float ButtonSize = 38f;
        private const float Padding = 4f;
        private const float MaxHeight = 350f;

        private readonly Dictionary<string, Texture2D> symbolTextures;
        private readonly List<string> allSymbolKeys;
        private readonly GameObject[] targets;

        private Vector2 scrollPosition;
        private string searchString = string.Empty;

        public HierarchySymbolPicker(IReadOnlyDictionary<string, Texture2D> textures, GameObject[] targets)
        {
            symbolTextures = new Dictionary<string, Texture2D>(textures);
            allSymbolKeys = symbolTextures.Keys.ToList();
            this.targets = targets;
        }

        private List<string> GetFilteredKeys()
        {
            if (string.IsNullOrEmpty(searchString))
                return allSymbolKeys;

            // Keys are stored lower-cased, so only the needle needs normalizing.
            string needle = searchString.ToLowerInvariant();
            return allSymbolKeys.Where(key => key.Contains(needle)).ToList();
        }

        private bool ShouldShowNone()
        {
            return string.IsNullOrEmpty(searchString) || "none".Contains(searchString.ToLowerInvariant());
        }

        public override Vector2 GetWindowSize()
        {
            int total = GetFilteredKeys().Count + (ShouldShowNone() ? 1 : 0);
            int rows = Mathf.Max(1, Mathf.CeilToInt(total / (float)Columns));

            float width = (Columns * (ButtonSize + Padding)) + 20f;
            float height = Mathf.Min((rows * (ButtonSize + Padding)) + 65f, MaxHeight);

            return new Vector2(width, height);
        }

        public override void OnGUI(Rect rect)
        {
            GUILayout.Label("Select Symbol", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            searchString = EditorGUILayout.TextField(searchString, EditorStyles.toolbarSearchField);
            if (EditorGUI.EndChangeCheck())
                editorWindow.Repaint();

            EditorGUILayout.Space(6);

            List<string> keys = GetFilteredKeys();
            bool showNone = ShouldShowNone();

            if (!showNone && keys.Count == 0)
            {
                GUILayout.Label("No symbols found...", EditorStyles.miniLabel);
                return;
            }

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false);

            // An optional "None" entry (which clears the symbol) precedes the symbol buttons.
            // Driving both through one flat index keeps the grid uniform and easy to reason about.
            int total = keys.Count + (showNone ? 1 : 0);
            int noneOffset = showNone ? 1 : 0;

            for (int drawn = 0; drawn < total;)
            {
                GUILayout.BeginHorizontal();
                for (int col = 0; col < Columns && drawn < total; col++, drawn++)
                {
                    if (showNone && drawn == 0)
                    {
                        if (GUILayout.Button("None", GUILayout.Width(ButtonSize), GUILayout.Height(ButtonSize)))
                        {
                            Apply(null);
                            editorWindow.Close();
                        }
                        continue;
                    }

                    string key = keys[drawn - noneOffset];
                    GUIContent content = new GUIContent(symbolTextures[key], key);
                    if (GUILayout.Button(content, GUILayout.Width(ButtonSize), GUILayout.Height(ButtonSize)))
                    {
                        Apply(key);
                        editorWindow.Close();
                    }
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }

        /// <summary>
        /// Assigns (or clears) the symbol on each target and registers the change for Undo/Redo.
        /// Passing a null/empty key removes the symbol component entirely.
        /// </summary>
        private void Apply(string key)
        {
            foreach (GameObject go in targets)
            {
                if (go == null)
                    continue;

                HierarchySymbol symbol = go.GetComponent<HierarchySymbol>();

                if (string.IsNullOrEmpty(key))
                {
                    if (symbol != null)
                        Undo.DestroyObjectImmediate(symbol);
                }
                else
                {
                    if (symbol == null)
                        symbol = Undo.AddComponent<HierarchySymbol>(go);
                    else
                        Undo.RecordObject(symbol, "Change Symbol");

                    symbol.symbolKey = key;
                    EditorUtility.SetDirty(symbol);
                }

                EditorSceneManager.MarkSceneDirty(go.scene);
            }

            HierarchyOverlay.RebuildHierarchyCache();
            EditorApplication.RepaintHierarchyWindow();
        }
    }
}