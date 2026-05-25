using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThroneOfTides.Tools
{
    // Toolbar dropdown that lists every .unity file under the project's Scenes folder
    // and switches to the chosen scene, prompting to save unsaved changes first.
    [InitializeOnLoad]
    public static class SceneSwitcherTool
    {
        private const string k_ElementPath = "ThroneOfTides/Scene Switcher";
        private static string[] _scenePaths = new string[0];

        private static string ScenesFolder =>
            Path.Combine(Application.dataPath, "_Game", "7. Scenes");

        static SceneSwitcherTool()
        {
            ScanScenes();
        }

        // Called by SceneAssetWatcher whenever .unity files are imported or deleted.
        // Kept internal so the watcher can invoke it without exposing the full class surface.
        internal static void OnSceneAssetsChanged()
        {
            ScanScenes();
            MainToolbar.Refresh(k_ElementPath);
        }

        private static void ScanScenes()
        {
            if (!Directory.Exists(ScenesFolder))
            {
                if (_scenePaths.Length > 0)
                {
                    _scenePaths = new string[0];
                    MainToolbar.Refresh(k_ElementPath);
                }
                return;
            }

            var found = Directory
                .GetFiles(ScenesFolder, "*.unity", SearchOption.AllDirectories)
                .Select(p => "Assets" + p.Replace(Application.dataPath, "").Replace("\\", "/"))
                .OrderBy(p => p)
                .ToArray();

            // Only refresh the toolbar element if the scene list actually changed,
            // avoiding unnecessary UI rebuilds on unrelated asset imports.
            if (found.SequenceEqual(_scenePaths)) return;
            _scenePaths = found;
            MainToolbar.Refresh(k_ElementPath);
        }

        [MainToolbarElement(k_ElementPath)]
        public static MainToolbarElement CreateSceneSwitcher()
        {
            string activeScene = Application.isPlaying
                ? SceneManager.GetActiveScene().name
                : EditorSceneManager.GetActiveScene().name;

            if (string.IsNullOrEmpty(activeScene)) activeScene = "Untitled";

            var icon    = EditorGUIUtility.IconContent("SceneAsset Icon").image as Texture2D;
            var content = new MainToolbarContent(activeScene, icon, "Switch Scene");

            return new MainToolbarDropdown(content, ShowDropdown);
        }

        private static void ShowDropdown(Rect dropdownRect)
        {
            if (_scenePaths.Length == 0)
            {
                var empty = new GenericMenu();
                empty.AddDisabledItem(new GUIContent("No scenes found"));
                empty.DropDown(dropdownRect);
                return;
            }

            var    menu            = new GenericMenu();
            string activeScenePath = EditorSceneManager.GetActiveScene().path;

            foreach (string path in _scenePaths)
            {
                string sceneName = Path.GetFileNameWithoutExtension(path);
                bool   isActive  = path == activeScenePath;
                string localPath = path;

                menu.AddItem(new GUIContent(sceneName), isActive, () =>
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene(localPath);
                        MainToolbar.Refresh(k_ElementPath);
                    }
                });
            }

            menu.DropDown(dropdownRect);
        }
    }

    // Listens for asset changes via Unity's import pipeline and notifies SceneSwitcherTool
    // only when a .unity file is involved. This replaces the previous polling approach
    // (EditorApplication.update every 2 seconds) with a zero-cost event-driven alternative.
    internal class SceneAssetWatcher : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            bool sceneListChanged =
                importedAssets.Any(p => p.EndsWith(".unity"))  ||
                deletedAssets.Any(p => p.EndsWith(".unity"))   ||
                movedAssets.Any(p => p.EndsWith(".unity"))     ||
                movedFromAssetPaths.Any(p => p.EndsWith(".unity"));

            if (sceneListChanged)
                SceneSwitcherTool.OnSceneAssetsChanged();
        }
    }
}