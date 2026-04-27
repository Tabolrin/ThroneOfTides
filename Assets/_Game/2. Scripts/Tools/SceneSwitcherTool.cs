using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThroneOfTides.Tools
{
    [InitializeOnLoad]
    public static class SceneSwitcherTool
    {
        private const string k_ElementPath = "ThroneOfTides/Scene Switcher";
        private static string[] _scenePaths = new string[0];
        private static float    _lastScanTime;

        private static string ScenesFolder =>
            Path.Combine(Application.dataPath, "_Game", "7. Scenes");

        static SceneSwitcherTool()
        {
            EditorApplication.update -= CheckForSceneChanges;
            EditorApplication.update += CheckForSceneChanges;
            ScanScenes();
        }

        private static void CheckForSceneChanges()
        {
            if (EditorApplication.timeSinceStartup - _lastScanTime < 2f) return;
            _lastScanTime = (float)EditorApplication.timeSinceStartup;
            ScanScenes();
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

            // Only refresh toolbar if list actually changed
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
}