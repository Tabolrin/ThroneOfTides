using ThroneOfTides.Data;
using UnityEngine;

namespace ThroneOfTides.UI
{
    public class MainMenuManager : MonoBehaviour
    {
        [Header("Save/Load")]
        [Tooltip("Loaded once at game boot, before anything reads either asset.")]
        [SerializeField] private PlayerInventory     _playerInventory;
        [SerializeField] private ProgressionSO       _progression;
        [SerializeField] private GameplaySettingsSO  _gameplaySettings;

        [Header("Options")]
        [SerializeField] private OptionsPanel _optionsPanel;

        private void Awake()
        {
            if (_playerInventory   != null) _playerInventory.LoadFromDisk();
            if (_progression       != null) _progression.LoadFromDisk();
            if (_gameplaySettings  != null) _gameplaySettings.LoadFromDisk();
        }

        public void OnPlayPressed() =>
            SceneFader.LoadScene("LevelSelect");

        public void OnPortPressed() =>
            SceneFader.LoadScene("Port");

        public void OnOptionsPressed() =>
            _optionsPanel?.Show();

        public void OnQuitPressed()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}