using UnityEditor;
using UnityEngine;
using HierarchyTools.Runtime;

namespace HierarchyTools.Editor
{
    public static class HierarchyToolsMenu
    {
        [MenuItem("GameObject/Create/Folder %#f", false, 10)]
        public static void CreateFolder(MenuCommand menuCommand)
        {
            GameObject folder = new GameObject("New Folder");
            folder.AddComponent<HierarchyFolder>();

            GameObjectUtility.SetParentAndAlign(folder, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(folder, "Create Folder");
            Selection.activeObject = folder;
        }

        [MenuItem("GameObject/Assign Symbol...", false, 20)]
        private static void OpenSymbolPicker(MenuCommand menuCommand)
        {
            if (Selection.activeGameObject != menuCommand.context)
                return;

            // Safe fallback checking loop if Event.current is null outside an explicit IMGUI cycle pass
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