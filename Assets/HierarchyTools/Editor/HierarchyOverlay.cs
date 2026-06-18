using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using HierarchyTools.Runtime;

namespace HierarchyTools.Editor
{
    /// <summary>
    /// Draws custom folder backgrounds and assigned symbol icons over rows in the Unity
    /// Hierarchy window. All work is cache-driven so the per-row GUI callback stays cheap even
    /// in large scenes.
    /// </summary>
    [InitializeOnLoad]
    public static class HierarchyOverlay
    {
        private const float IconSize = 16f;

        // Caches sized for high-frequency repaint events.
        private static readonly HashSet<int> folderIds = new();
        private static readonly Dictionary<int, string> symbolByInstanceId = new();
        private static readonly Dictionary<string, Texture2D> symbolTextures = new();

        // Instance IDs already confirmed to be neither a folder nor a symbol. This stops the
        // overlay from re-resolving and re-probing ordinary rows on every repaint; entries are
        // cleared whenever the hierarchy changes.
        private static readonly HashSet<int> probedNonMatches = new();

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
        /// Caches symbol textures found in any folder named "HierarchySymbols".
        /// </summary>
        private static void LoadSymbolTextures()
        {
            symbolTextures.Clear();

            // Scope the scan to the symbol folders rather than enumerating every Texture2D in
            // the project. This matters because the method is bound to projectChanged, which
            // fires on any asset import or move. Loading through AssetDatabase (not Resources)
            // also keeps these editor-only icons out of player builds.
            string[] symbolFolders = AssetDatabase.FindAssets("HierarchySymbols t:Folder")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith("/HierarchySymbols"))
                .ToArray();

            if (symbolFolders.Length == 0)
                return;

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", symbolFolders))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex == null)
                    continue;

                string key = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                symbolTextures[key] = tex;
            }
        }

        /// <summary>
        /// Rebuilds the instance-ID lookups from the currently loaded scenes and resets the
        /// negative cache so newly added folders/symbols are picked up.
        /// </summary>
        public static void RebuildHierarchyCache()
        {
            folderIds.Clear();
            symbolByInstanceId.Clear();
            probedNonMatches.Clear();

            foreach (HierarchyFolder folder in Object.FindObjectsByType<HierarchyFolder>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                folderIds.Add(folder.gameObject.GetInstanceID());

            foreach (HierarchySymbol symbol in Object.FindObjectsByType<HierarchySymbol>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!string.IsNullOrEmpty(symbol.symbolKey))
                    symbolByInstanceId[symbol.gameObject.GetInstanceID()] = symbol.symbolKey.ToLowerInvariant();
            }
        }

        private static void OnHierarchyGUI(int instanceId, Rect selectionRect)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            bool isFolder = folderIds.Contains(instanceId);
            string symbolKey = null;
            bool hasSymbol = !isFolder && symbolByInstanceId.TryGetValue(instanceId, out symbolKey);

            if (!isFolder && !hasSymbol)
            {
                // Already known to be an ordinary row: nothing to draw, no work to do.
                if (probedNonMatches.Contains(instanceId))
                    return;

                // First sighting of this id. Resolve and probe it exactly once; the result is
                // recorded in a cache so subsequent repaints skip straight past it. This covers
                // objects that appear without a hierarchyChanged rebuild (e.g. some prefab edits).
                GameObject probed = ResolveGameObject(instanceId);
                if (probed != null && probed.TryGetComponent<HierarchyFolder>(out _))
                {
                    isFolder = true;
                    folderIds.Add(instanceId);
                }
                else if (probed != null && probed.TryGetComponent<HierarchySymbol>(out HierarchySymbol comp) && !string.IsNullOrEmpty(comp.symbolKey))
                {
                    symbolKey = comp.symbolKey.ToLowerInvariant();
                    symbolByInstanceId[instanceId] = symbolKey;
                    hasSymbol = true;
                }
                else
                {
                    probedNonMatches.Add(instanceId);
                    return;
                }
            }

            GameObject go = ResolveGameObject(instanceId);
            if (go == null)
                return;

            bool isSelected = System.Array.IndexOf(Selection.gameObjects, go) >= 0;
            Rect iconRect = new Rect(selectionRect.x, selectionRect.y, IconSize, IconSize);
            Color background = isSelected ? unitySelectionBlue : defaultRowBackgroundColor;

            if (isFolder)
            {
                EditorGUI.DrawRect(iconRect, background);
                if (folderIcon != null)
                    GUI.DrawTexture(iconRect, folderIcon);
            }
            else if (symbolTextures.TryGetValue(symbolKey, out Texture2D icon))
            {
                EditorGUI.DrawRect(iconRect, background);
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            }
        }

        private static GameObject ResolveGameObject(int instanceId)
        {
            // The non-generic overload is the only one available across the editor versions this
            // tool targets; the obsolete warning is suppressed deliberately.
#pragma warning disable CS0618
            return EditorUtility.InstanceIDToObject(instanceId) as GameObject;
#pragma warning restore CS0618
        }
    }
}