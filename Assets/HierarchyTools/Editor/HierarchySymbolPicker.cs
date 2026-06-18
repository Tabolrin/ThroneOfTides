using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HierarchyTools.Runtime;

namespace HierarchyTools.Editor
{
    /// <summary>
    /// Editor popup interface for selecting and applying visual symbols to GameObjects.
    /// </summary>
    public class HierarchySymbolPicker : PopupWindowContent
    {
        private const int Columns = 8;
        private const float ButtonSize = 38f;
        private const float Padding = 4f;

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

            return allSymbolKeys.Where(key => key.ToLower().Contains(searchString.ToLower())).ToList();
        }

        public override Vector2 GetWindowSize()
        {
            List<string> filteredKeys = GetFilteredKeys();
            int totalItems = filteredKeys.Count + 1;
            int rows = Mathf.CeilToInt(totalItems / (float)Columns);
            
            float width = (Columns * (ButtonSize + Padding)) + 20f;
            float calculatedHeight = (rows * (ButtonSize + Padding)) + 65f;
            
            // Constrain maximum window height to prevent overflow on smaller displays
            float height = Mathf.Min(calculatedHeight, 350f); 

            return new Vector2(width, height);
        }

        public override void OnGUI(Rect rect)
        {
            GUILayout.Label("Select Symbol", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            searchString = EditorGUILayout.TextField(searchString, EditorStyles.toolbarSearchField);
            if (EditorGUI.EndChangeCheck())
            {
                editorWindow.Repaint();
            }
            
            EditorGUILayout.Space(6);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false);

            List<string> filteredKeys = GetFilteredKeys();
            int index = 0;

            bool showNone = string.IsNullOrEmpty(searchString) || "none".Contains(searchString.ToLower());
            
            if (!showNone && filteredKeys.Count == 0)
            {
                GUILayout.Label("No symbols found...", EditorStyles.miniLabel);
            }

            while (index <= filteredKeys.Count)
            {
                if (index == 0 && !showNone)
                {
                    index++;
                    continue;
                }

                GUILayout.BeginHorizontal();
                for (int i = 0; i < Columns && index <= filteredKeys.Count; i++, index++)
                {
                    if (index == 0)
                    {
                        if (GUILayout.Button("None", GUILayout.Width(ButtonSize), GUILayout.Height(ButtonSize)))
                        {
                            Apply(null);
                            editorWindow.Close();
                        }
                    }
                    else
                    {
                        if (index - 1 >= filteredKeys.Count) break;

                        string key = filteredKeys[index - 1];
                        if (GUILayout.Button(new GUIContent(symbolTextures[key], key), GUILayout.Width(ButtonSize), GUILayout.Height(ButtonSize)))
                        {
                            Apply(key);
                            editorWindow.Close();
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }

        /// <summary>
        /// Assigns the selected symbol key to target objects and registers the changes for Undo/Redo.
        /// </summary>
        private void Apply(string key)
        {
            foreach (GameObject go in targets)
            {
                if (go == null) continue;

                var symbol = go.GetComponent<HierarchySymbol>();

                if (string.IsNullOrEmpty(key))
                {
                    if (symbol != null)
                    {
                        Undo.DestroyObjectImmediate(symbol);
                    }
                }
                else
                {
                    if (symbol == null)
                    {
                        symbol = Undo.AddComponent<HierarchySymbol>(go);
                    }
                    else
                    {
                        Undo.RecordObject(symbol, "Change Symbol");
                    }

                    symbol.symbolKey = key;
                    EditorUtility.SetDirty(symbol);
                }
                
                EditorSceneManager.MarkSceneDirty(go.scene);
            }

            // Invalidate and rebuild cache to reflect modifications immediately
            HierarchyOverlay.RebuildHierarchyCache();
            EditorApplication.RepaintHierarchyWindow();
        }
    }
}