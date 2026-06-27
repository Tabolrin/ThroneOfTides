using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HierarchyTools.Runtime;

namespace HierarchyTools.Editor
{
    /// <summary>
    /// Registers the tool's actions into the Unity Editor context and top-level menus.
    /// </summary>
    public static class HierarchyToolsMenu
    {
        /// <summary>
        /// Creates a new organizational folder under the selected object. When the context lives
        /// under a Canvas the folder is given a stretched RectTransform so it acts as a layout-
        /// transparent UI layer. With no context the folder is placed into whichever stage is
        /// currently open, so creation works correctly inside the Prefab Stage.
        /// </summary>
        [MenuItem("GameObject/Folder %#f", false, 10)]
        public static void CreateFolder(MenuCommand menuCommand)
        {
            // Items under "GameObject/" are dispatched once per selected object when triggered
            // from the Hierarchy context menu. Acting only for the first selected object keeps a
            // multi-selection from spawning one folder per object.
            GameObject[] selected = Selection.gameObjects;
            if (menuCommand.context != null && selected.Length > 1 && menuCommand.context != selected[0])
                return;

            GameObject context = menuCommand.context as GameObject;
            GameObject folder = new GameObject("New Folder");
            folder.AddComponent<HierarchyFolder>();

            if (context != null)
            {
                bool isUiContext = context.GetComponentInParent<Canvas>(true) != null;
                if (isUiContext)
                {
                    // A plain GameObject parented under a Canvas keeps its regular Transform, which
                    // the UI system does not treat as a layout element. Adding a RectTransform
                    // replaces that Transform, letting the folder host UI children correctly.
                    RectTransform rect = folder.AddComponent<RectTransform>();
                    GameObjectUtility.SetParentAndAlign(folder, context);

                    // Stretch to fill the parent with zero offsets so reparented UI children keep
                    // their anchored positions when the folder is dissolved at build time.
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.localScale = Vector3.one;
                    rect.localRotation = Quaternion.identity;
                }
                else
                {
                    // SetParentAndAlign also moves the folder into the context's scene, so a
                    // context inside an open Prefab Stage places the folder in that stage.
                    GameObjectUtility.SetParentAndAlign(folder, context);
                }
            }
            else
            {
                // Created from the top menu or empty Hierarchy space. The object was spawned in
                // the active main scene, which is hidden while a prefab is open, so re-home it
                // into the current stage (main scene or open Prefab Stage) rather than leaving it
                // stranded in the wrong scene.
                StageUtility.PlaceGameObjectInCurrentStage(folder);
            }

            Undo.RegisterCreatedObjectUndo(folder, "Create Folder");
            Selection.activeObject = folder;
        }

        [MenuItem("GameObject/Assign Symbol...", false, 20)]
        private static void OpenSymbolPicker(MenuCommand menuCommand)
        {
            // Same per-object dispatch behaviour as above: open the picker exactly once, for the
            // active object, while still applying to the full selection via Selection.gameObjects.
            if (Selection.activeGameObject != menuCommand.context)
                return;

            // Resolve spawn coordinates, accounting for contexts where Event.current is null.
            Vector2 mousePosition = Vector2.zero;
            if (Event.current != null)
            {
                mousePosition = Event.current.mousePosition;
            }
            else
            {
                EditorWindow focusedWindow = EditorWindow.focusedWindow;
                if (focusedWindow != null)
                    mousePosition = new Vector2(focusedWindow.position.width / 2f, focusedWindow.position.height / 3f);
            }

            HierarchySymbolPicker picker = new HierarchySymbolPicker(HierarchyOverlay.SymbolTextures, Selection.gameObjects);
            PopupWindow.Show(new Rect(mousePosition, Vector2.zero), picker);
        }
    }
}