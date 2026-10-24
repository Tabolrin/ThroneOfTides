// Assets/_Game/2. Scripts/UI/WebBuildQuitButtonHider.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ThroneOfTides.UI
{
    // Application.Quit does nothing in a browser, so in WebGL builds every Quit button is hidden
    // instead of being left on screen as a dead button. Self-bootstraps like SceneFader, so no
    // scene or prefab needs a component added - it finds Quit buttons by what their onClick
    // calls (MainMenuManager.OnQuitPressed, MoreMountains' MMApplicationQuit.Quit) on every
    // scene load. Inactive objects are included, so a Quit button inside a closed panel (e.g.
    // the Options panel) is hidden before the panel is ever opened.
    // Compiled into WebGL player builds only - Editor play mode and desktop builds are untouched.
    public static class WebBuildQuitButtonHider
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        private static readonly string[] QuitMethodNames = { "OnQuitPressed", "Quit" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap() => SceneManager.sceneLoaded += (scene, mode) => HideQuitButtons();

        private static void HideQuitButtons()
        {
            var buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var button in buttons)
            {
                if (IsQuitButton(button))
                    button.gameObject.SetActive(false);
            }
        }

        private static bool IsQuitButton(Button button)
        {
            var onClick = button.onClick;
            for (int i = 0; i < onClick.GetPersistentEventCount(); i++)
            {
                string method = onClick.GetPersistentMethodName(i);
                foreach (var quitName in QuitMethodNames)
                {
                    if (method == quitName)
                        return true;
                }
            }
            return false;
        }
#endif
    }
}
