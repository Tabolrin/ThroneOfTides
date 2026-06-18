using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using HierarchyTools.Runtime;

namespace HierarchyTools.Editor
{
    /// <summary>
    /// Manages the custom GUI overlay for the Unity Hierarchy window, handling the rendering 
    /// of custom folder backgrounds and assigned symbol icons.
    /// </summary>
    [InitializeOnLoad]
    public static class HierarchyOverlay
    {
        // Lookup caches optimized for high-frequency GUI events
        private static readonly HashSet<int> folderIds = new();
        private static readonly Dictionary<int, string> symbolByInstanceId = new();
        private static readonly Dictionary<string, Texture2D> symbolTextures = new();

        public static IReadOnlyDictionary<string, Texture2D> SymbolTextures => symbolTextures;

        private static Texture2D folderIcon;
        private static Color defaultRowBackgroundColor;
        private static Color unitySelectionBlue;

        static HierarchyOverlay()
        {
            folderIcon = EditorGUIUtility.FindTexture("Folder Icon");
            
            defaultRowBackgroundColor = EditorGUIUtility.isProSkin 
                ? new Color(0.22f, 0.22f, 0.22f, 1f) 
                : new Color(0.78f, 0.78f, 0.78f, 1f);

            unitySelectionBlue = new Color(0.17f, 0.36f, 0.53f, 1f);

            LoadSymbolTextures();
            RebuildHierarchyCache();

            EditorApplication.projectChanged += LoadSymbolTextures;
            EditorApplication.hierarchyChanged += RebuildHierarchyCache;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
        }

        /// <summary>
        /// Discovers and caches designated symbol textures from the project resources.
        /// </summary>
        private static void LoadSymbolTextures()
        {
            symbolTextures.Clear();

            string[] guids = AssetDatabase.FindAssets("t:Texture2D");
            if (guids == null || guids.Length == 0)
                return;

            foreach (string guid in guids)
            {
                string filePath = AssetDatabase.GUIDToAssetPath(guid);
        
                string resourcesToken = "/Resources/";
                int resourcesIndex = filePath.IndexOf(resourcesToken, System.StringComparison.OrdinalIgnoreCase);
        
                // Restrict loading strictly to the defined symbol directory hierarchy
                if (resourcesIndex == -1 || !filePath.Contains("/HierarchySymbols/")) 
                    continue;

                string key = Path.GetFileNameWithoutExtension(filePath);

                // Format path for runtime Resource loading API
                int startPathIndex = resourcesIndex + resourcesToken.Length;
                int extensionIndex = filePath.LastIndexOf('.');
                string resourcePath = filePath.Substring(startPathIndex, extensionIndex - startPathIndex);

                Texture2D tex = Resources.Load<Texture2D>(resourcePath);
        
                if (tex != null)
                {
                    // Force lowercase keys to guarantee case-insensitive lookups
                    symbolTextures[key.ToLower()] = tex;
                }
            }
        }

        /// <summary>
        /// Rebuilds the internal mapping of instance IDs to their respective custom hierarchy components.
        /// </summary>
        public static void RebuildHierarchyCache()
        {
            folderIds.Clear();
            symbolByInstanceId.Clear();

            foreach (var folder in Object.FindObjectsByType<HierarchyFolder>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                folderIds.Add(folder.gameObject.GetInstanceID());

            foreach (var symbol in Object.FindObjectsByType<HierarchySymbol>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!string.IsNullOrEmpty(symbol.symbolKey))
                    symbolByInstanceId[symbol.gameObject.GetInstanceID()] = symbol.symbolKey.ToLower();
            }
        }

        private static void OnHierarchyGUI(int instanceId, Rect selectionRect)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            bool isFolder = folderIds.Contains(instanceId);
            bool hasSymbol = symbolByInstanceId.TryGetValue(instanceId, out string symbolKey);

            // Fallback resolution for newly created or modified objects not yet captured by the cache
            if (!isFolder && !hasSymbol)
            {
#pragma warning disable CS0618
                GameObject targetGo = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
#pragma warning restore CS0618
                if (targetGo != null)
                {
                    var comp = targetGo.GetComponent<HierarchySymbol>();
                    if (comp != null && !string.IsNullOrEmpty(comp.symbolKey))
                    {
                        hasSymbol = true;
                        symbolKey = comp.symbolKey.ToLower();
                    }
                }
            }

            if (!isFolder && !hasSymbol)
                return; 

#pragma warning disable CS0618
            GameObject go = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
#pragma warning restore CS0618

            if (go == null) return;

            bool isSelected = Selection.activeGameObject == go || System.Array.IndexOf(Selection.gameObjects, go) >= 0;
            Rect iconRect = new Rect(selectionRect.x, selectionRect.y, 16, 16);

            if (isFolder)
            {
                EditorGUI.DrawRect(iconRect, isSelected ? unitySelectionBlue : defaultRowBackgroundColor);
                if (folderIcon != null)
                {
                    GUI.DrawTexture(iconRect, folderIcon);
                }
            }
            else if (hasSymbol && symbolTextures.TryGetValue(symbolKey, out Texture2D icon))
            {
                EditorGUI.DrawRect(iconRect, isSelected ? unitySelectionBlue : defaultRowBackgroundColor);
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            }
        }
    }
}