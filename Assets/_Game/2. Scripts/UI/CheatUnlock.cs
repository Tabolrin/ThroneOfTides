// Assets/_Game/2. Scripts/UI/CheatUnlock.cs
using System;
using ThroneOfTides.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ThroneOfTides.UI
{
    /// <summary>
    /// Gates every cheat panel behind a typed code, in every build (Windows, WebGL, development
    /// or release). Typing Code anywhere in the game toggles the cheats on, typing it again
    /// toggles them back off. The panels themselves (CheatsPanel, CardCheatPanel,
    /// ForceEnemyCardCheatPanel) subscribe to Changed and only show their toggle buttons while
    /// unlocked. Unlocked state lasts for the current session only and is never saved, so a
    /// player who finds the code doesn't carry it into their next launch.
    /// Self-bootstraps like SceneFader - no scene setup needed. Starts unlocked in the Editor so
    /// testing in Play Mode works the same as before.
    /// </summary>
    public static class CheatUnlock
    {
        // Change this to pick a different code. Case doesn't matter, letters only is safest.
        public const string Code = "mutiny";

        public static bool IsUnlocked { get; private set; }
        public static event Action<bool> Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            // Reset explicitly - statics survive between Play Mode sessions when domain reload
            // is turned off in the Editor.
            Changed    = null;
            IsUnlocked = Application.isEditor;

            var host = new GameObject("[CheatUnlock]") { hideFlags = HideFlags.HideInHierarchy };
            UnityEngine.Object.DontDestroyOnLoad(host);
            host.AddComponent<CodeListener>();
        }

        private static void Toggle()
        {
            IsUnlocked = !IsUnlocked;
            Changed?.Invoke(IsUnlocked);
            GameEventBus.FireMatchNote(IsUnlocked ? "Cheats enabled." : "Cheats disabled.");
        }

        // Listens to typed characters (not raw key presses), so it works the same with any
        // keyboard layout, on desktop and in the browser.
        private class CodeListener : MonoBehaviour
        {
            private Keyboard _hookedKeyboard;
            private string   _typed = string.Empty;

            // Keyboard.current can be null at startup or change if a keyboard is reconnected,
            // so the hook is (re)attached whenever the current keyboard changes.
            private void Update()
            {
                var keyboard = Keyboard.current;
                if (keyboard == _hookedKeyboard) return;

                if (_hookedKeyboard != null) _hookedKeyboard.onTextInput -= OnTextInput;
                _hookedKeyboard = keyboard;
                if (_hookedKeyboard != null) _hookedKeyboard.onTextInput += OnTextInput;
            }

            private void OnDestroy()
            {
                if (_hookedKeyboard != null) _hookedKeyboard.onTextInput -= OnTextInput;
            }

            private void OnTextInput(char character)
            {
                _typed += char.ToLowerInvariant(character);
                if (_typed.Length > Code.Length)
                    _typed = _typed.Substring(_typed.Length - Code.Length);

                if (_typed != Code) return;

                _typed = string.Empty;
                Toggle();
            }
        }
    }
}
