using UnityEditor;
using UnityEngine;
using HierarchyTools.Runtime;

namespace HierarchyTools.Editor
{
    /// <summary>
    /// Registers tool actions into the Unity Editor context and top-level menus.
    /// </summary>
    public static class HierarchyToolsMenu
    {
        /// <summary>
        /// Instantiates a new organizational folder in the hierarchy. 
        /// Automatically detects Canvas contexts to normalize RectTransform data, 
        /// ensuring UI layout elements remain non-disruptive.
        /// </summary>
        [MenuItem("GameObject/Folder %#f", false, 10)]
        public static void CreateFolder(MenuCommand menuCommand)
        {
            GameObject context = menuCommand.context as GameObject;
            GameObject folder = new GameObject("New Folder");
            folder.AddComponent<HierarchyFolder>();

            GameObjectUtility.SetParentAndAlign(folder, context);

            // Normalize RectTransform properties to act as a mathematically transparent layout layer.
            // This ensures that reparented UI elements seamlessly retain their original absolute screen positions and anchors.
            if (folder.transform is RectTransform rectTransform)
            {
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                rectTransform.localScale = Vector3.one;
                rectTransform.localRotation = Quaternion.identity;
            }

            Undo.RegisterCreatedObjectUndo(folder, "Create Folder");
            Selection.activeObject = folder;
        }

        [MenuItem("GameObject/Assign Symbol...", false, 20)]
        private static void OpenSymbolPicker(MenuCommand menuCommand)
        {
            if (Selection.activeGameObject != menuCommand.context)
                return;

            // Resolve proper spawn coordinates, accounting for contexts where Event.current is unavailable
            Vector2 mousePosition = Vector2.zero;
            if (Event.current != null)
            {
                mousePosition = Event.current.mousePosition;
            }
            else
            {
                var focusedWindow = EditorWindow.focusedWindow;
                if (focusedWindow != null)
                    mousePosition = new Vector2(focusedWindow.position.width / 2f, focusedWindow.position.height / 3f);
            }

            var picker = new HierarchySymbolPicker(HierarchyOverlay.SymbolTextures, Selection.gameObjects);
            PopupWindow.Show(new Rect(mousePosition, Vector2.zero), picker);
        }
    }
}