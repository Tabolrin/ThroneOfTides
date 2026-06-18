using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using HierarchyTools.Runtime;

namespace HierarchyTools.Editor
{
    [InitializeOnLoad]
    public static class HierarchyOverlay
    {
        // Internal tracking sets for assigned icons and folders
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

        private static void LoadSymbolTextures()
        {
            symbolTextures.Clear();

            // 1. Search the entire project for all Texture2D assets everywhere.
            // This will locate .png, .jpg, .tga, .psd, etc., no matter where they are.
            string[] guids = AssetDatabase.FindAssets("t:Texture2D");
            if (guids == null || guids.Length == 0)
                return;

            foreach (string guid in guids)
            {
                string filePath = AssetDatabase.GUIDToAssetPath(guid);
        
                // 2. Filter strictly for files living inside your target folder layout
                string resourcesToken = "/Resources/";
                int resourcesIndex = filePath.IndexOf(resourcesToken, System.StringComparison.OrdinalIgnoreCase);
        
                if (resourcesIndex == -1 || !filePath.Contains("/HierarchySymbols/")) 
                    continue;

                // Isolate the clean file name to use as your short symbolKey
                string key = Path.GetFileNameWithoutExtension(filePath);

                // 3. Dynamic Subfolder path calculation for Resources.Load
                // Removes "Assets/.../Resources/" and strips the file extension out completely
                int startPathIndex = resourcesIndex + resourcesToken.Length;
                int extensionIndex = filePath.LastIndexOf('.');
                string resourcePath = filePath.Substring(startPathIndex, extensionIndex - startPathIndex);

                // 4. Load the asset natively through the UPM Resources engine
                Texture2D tex = Resources.Load<Texture2D>(resourcePath);
        
                if (tex != null)
                {
                    // Cached as lowercase so lookups remain bulletproof and case-insensitive
                    symbolTextures[key.ToLower()] = tex;
                }
            }
        }

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

            // Quick Direct Fallback Guard Loop
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