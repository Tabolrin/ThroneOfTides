// Assets/_Game/2. Scripts/UI/OptionsPanel.cs
using System;
using System.Collections.Generic;
using MoreMountains.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ThroneOfTides.UI
{
    /// <summary>
    /// Simple options panel: Master/Music/Sfx volume (Sfx slider drives both the Sfx and UI
    /// mixer tracks together - the game only ever exposes one combined "sound effects" slider
    /// to the player), fullscreen, resolution, and VSync. Audio volumes are persisted by
    /// MMSoundManager's own settings system; display settings persist via their own small save
    /// file here, independent of everything else.
    /// </summary>
    public class OptionsPanel : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private Slider _masterVolumeSlider;
        [SerializeField] private Slider _musicVolumeSlider;
        [SerializeField] private Slider _sfxVolumeSlider;

        [Header("Display")]
        [SerializeField] private Toggle _fullscreenToggle;
        [SerializeField] private TMP_Dropdown _resolutionDropdown;
        [SerializeField] private Toggle _vsyncToggle;

        [Header("Gameplay")]
        [SerializeField] private ThroneOfTides.Data.GameplaySettingsSO _gameplaySettings;
        [Tooltip("On: the enemy's played-card reveal waits for a click. Off: it auto-dismisses after the slider's duration.")]
        [SerializeField] private Toggle _requireClickToDismissToggle;
        [SerializeField] private Slider _enemyCardDismissDurationSlider;
        [SerializeField] private TextMeshProUGUI _enemyCardDismissDurationLabel;

        [Header("Panel")]
        [SerializeField] private Button _closeButton;

        [Header("Playtest")]
        [Tooltip("Wipes saved progress (coins, collection, deck, upgrades, level-beaten flags) back to a fresh-install state. Left unassigned in builds where this isn't wired.")]
        [SerializeField] private ThroneOfTides.Data.PlayerInventory _playerInventory;
        [SerializeField] private ThroneOfTides.Data.ProgressionSO   _progression;
        [Tooltip("Clicking once arms the button (label/color change); the actual reset only fires on the confirming second click, so this can't be triggered by a stray misclick.")]
        [SerializeField] private Button _resetDataButton;
        [SerializeField] private TextMeshProUGUI _resetDataButtonLabel;
        [SerializeField] private string _resetDataDefaultText = "Reset Player Data";
        [SerializeField] private string _resetDataConfirmText = "Click again to confirm";
        [SerializeField] private float  _resetDataConfirmWindow = 3f;

        [Serializable]
        private class DisplaySaveData
        {
            public bool Fullscreen = true;
            public int  ResolutionIndex = -1; // -1 = not set yet, keep whatever's current
            public bool VSync = true;
        }

        private const string SaveFileName   = "display.save";
        private const string SaveFolderName = "ThroneOfTides/";

        private List<Resolution> _resolutions = new List<Resolution>();
        private bool _initialising;
        private bool _resetArmed;

        private void Awake()
        {
            gameObject.SetActive(false);

            if (_closeButton != null) _closeButton.onClick.AddListener(Hide);
            if (_resetDataButton != null) _resetDataButton.onClick.AddListener(OnResetDataClicked);
            SetResetButtonArmed(false);

            if (_masterVolumeSlider != null) _masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            if (_musicVolumeSlider  != null) _musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            if (_sfxVolumeSlider    != null) _sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);

            if (_fullscreenToggle   != null) _fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            if (_vsyncToggle        != null) _vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);
            if (_resolutionDropdown != null) _resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);

            if (_requireClickToDismissToggle    != null) _requireClickToDismissToggle.onValueChanged.AddListener(OnRequireClickToDismissChanged);
            if (_enemyCardDismissDurationSlider != null) _enemyCardDismissDurationSlider.onValueChanged.AddListener(OnEnemyCardDismissDurationChanged);

            BuildDistinctResolutions();
            ApplySavedDisplaySettings();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Show()
        {
            gameObject.SetActive(true);
            RefreshFromCurrentState();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            SetResetButtonArmed(false);
        }

        // ── Playtest: Reset Player Data ──────────────────────────────────────

        private void OnResetDataClicked()
        {
            if (!_resetArmed)
            {
                SetResetButtonArmed(true);
                CancelInvoke(nameof(DisarmResetButton));
                Invoke(nameof(DisarmResetButton), _resetDataConfirmWindow);
                return;
            }

            CancelInvoke(nameof(DisarmResetButton));
            SetResetButtonArmed(false);

            if (_playerInventory != null)
            {
                _playerInventory.Reset();
                _playerInventory.Save();
            }

            if (_progression != null)
            {
                _progression.Reset();
                _progression.Save();
            }

            Debug.Log("[OptionsPanel] Player data reset to a clean slate.");
        }

        private void DisarmResetButton() => SetResetButtonArmed(false);

        private void SetResetButtonArmed(bool armed)
        {
            _resetArmed = armed;
            if (_resetDataButtonLabel != null)
                _resetDataButtonLabel.text = armed ? _resetDataConfirmText : _resetDataDefaultText;
        }

        // ── Init / Refresh ────────────────────────────────────────────────────

        private void RefreshFromCurrentState()
        {
            _initialising = true;

            if (MMSoundManager.HasInstance)
            {
                if (_masterVolumeSlider != null) _masterVolumeSlider.value = MMSoundManager.Instance.GetTrackVolume(MMSoundManager.MMSoundManagerTracks.Master, false);
                if (_musicVolumeSlider  != null) _musicVolumeSlider.value  = MMSoundManager.Instance.GetTrackVolume(MMSoundManager.MMSoundManagerTracks.Music, false);
                if (_sfxVolumeSlider    != null) _sfxVolumeSlider.value    = MMSoundManager.Instance.GetTrackVolume(MMSoundManager.MMSoundManagerTracks.Sfx, false);
            }

            if (_fullscreenToggle != null) _fullscreenToggle.isOn = Screen.fullScreen;
            if (_vsyncToggle      != null) _vsyncToggle.isOn      = QualitySettings.vSyncCount > 0;

            PopulateResolutionDropdown();

            if (_gameplaySettings != null)
            {
                if (_requireClickToDismissToggle    != null) _requireClickToDismissToggle.isOn = _gameplaySettings.RequireClickToDismissEnemyCard;
                if (_enemyCardDismissDurationSlider != null) _enemyCardDismissDurationSlider.value = _gameplaySettings.EnemyCardAutoDismissDuration;
                RefreshDismissDurationInteractable();
                RefreshDismissDurationLabel();
            }

            _initialising = false;
        }

        // Screen.resolutions repeats each size once per supported refresh rate - collapse to
        // one entry per width/height. Built once and reused so the saved dropdown index and
        // Screen.resolutions' index never disagree with each other.
        private void BuildDistinctResolutions()
        {
            _resolutions.Clear();
            foreach (var res in Screen.resolutions)
            {
                if (_resolutions.Count > 0)
                {
                    var last = _resolutions[_resolutions.Count - 1];
                    if (last.width == res.width && last.height == res.height) continue;
                }
                _resolutions.Add(res);
            }
        }

        private void PopulateResolutionDropdown()
        {
            if (_resolutionDropdown == null) return;

            if (_resolutions.Count == 0) BuildDistinctResolutions();

            var options = new List<string>();
            int currentIndex = 0;

            for (int i = 0; i < _resolutions.Count; i++)
            {
                var res = _resolutions[i];
                options.Add($"{res.width} x {res.height}");

                if (res.width == Screen.width && res.height == Screen.height)
                    currentIndex = i;
            }

            _resolutionDropdown.ClearOptions();
            _resolutionDropdown.AddOptions(options);
            _resolutionDropdown.SetValueWithoutNotify(currentIndex);
        }

        // ── Audio Callbacks ───────────────────────────────────────────────────

        private void OnMasterVolumeChanged(float value)
        {
            MMSoundManagerTrackEvent.Trigger(MMSoundManagerTrackEventTypes.SetVolumeTrack, MMSoundManager.MMSoundManagerTracks.Master, value);
            SaveAudioSettings();
        }

        private void OnMusicVolumeChanged(float value)
        {
            MMSoundManagerTrackEvent.Trigger(MMSoundManagerTrackEventTypes.SetVolumeTrack, MMSoundManager.MMSoundManagerTracks.Music, value);
            SaveAudioSettings();
        }

        // One slider drives both the Sfx and UI mixer tracks together.
        private void OnSfxVolumeChanged(float value)
        {
            MMSoundManagerTrackEvent.Trigger(MMSoundManagerTrackEventTypes.SetVolumeTrack, MMSoundManager.MMSoundManagerTracks.Sfx, value);
            MMSoundManagerTrackEvent.Trigger(MMSoundManagerTrackEventTypes.SetVolumeTrack, MMSoundManager.MMSoundManagerTracks.UI, value);
            SaveAudioSettings();
        }

        private static void SaveAudioSettings()
        {
            if (MMSoundManager.HasInstance) MMSoundManager.Instance.SaveSettings();
        }

        // ── Gameplay Callbacks ────────────────────────────────────────────────

        private void OnRequireClickToDismissChanged(bool value)
        {
            _gameplaySettings?.SetRequireClickToDismissEnemyCard(value);
            RefreshDismissDurationInteractable();
        }

        private void OnEnemyCardDismissDurationChanged(float value)
        {
            if (!_initialising) _gameplaySettings?.SetEnemyCardAutoDismissDuration(value);
            RefreshDismissDurationLabel();
        }

        // The duration only matters when NOT waiting for a click - grey it out otherwise so the
        // player isn't left wondering why changing it does nothing.
        private void RefreshDismissDurationInteractable()
        {
            if (_enemyCardDismissDurationSlider == null) return;
            _enemyCardDismissDurationSlider.interactable =
                _requireClickToDismissToggle == null || !_requireClickToDismissToggle.isOn;
        }

        private void RefreshDismissDurationLabel()
        {
            if (_enemyCardDismissDurationLabel != null && _enemyCardDismissDurationSlider != null)
                _enemyCardDismissDurationLabel.text = $"Enemy Card Duration: {_enemyCardDismissDurationSlider.value:0.0}s";
        }

        // ── Display Callbacks ─────────────────────────────────────────────────

        private void OnFullscreenChanged(bool isFullscreen)
        {
            Screen.fullScreen = isFullscreen;
            if (!_initialising) SaveDisplaySettings();
        }

        private void OnVSyncChanged(bool isOn)
        {
            QualitySettings.vSyncCount = isOn ? 1 : 0;
            if (!_initialising) SaveDisplaySettings();
        }

        private void OnResolutionChanged(int index)
        {
            if (index < 0 || index >= _resolutions.Count) return;
            var res = _resolutions[index];
            Screen.SetResolution(res.width, res.height, Screen.fullScreen);
            if (!_initialising) SaveDisplaySettings();
        }

        // ── Display Save/Load ─────────────────────────────────────────────────

        private void SaveDisplaySettings()
        {
            var data = new DisplaySaveData
            {
                Fullscreen      = Screen.fullScreen,
                ResolutionIndex = _resolutionDropdown != null ? _resolutionDropdown.value : -1,
                VSync           = QualitySettings.vSyncCount > 0,
            };
            MMSaveLoadManager.Save(data, SaveFileName, SaveFolderName);
        }

        // Applied once at Awake so display prefs take effect even before the panel is opened.
        private void ApplySavedDisplaySettings()
        {
            var data = (DisplaySaveData)MMSaveLoadManager.Load(typeof(DisplaySaveData), SaveFileName, SaveFolderName);
            if (data == null) return;

            Screen.fullScreen         = data.Fullscreen;
            QualitySettings.vSyncCount = data.VSync ? 1 : 0;

            if (data.ResolutionIndex >= 0 && data.ResolutionIndex < _resolutions.Count)
            {
                var res = _resolutions[data.ResolutionIndex];
                Screen.SetResolution(res.width, res.height, data.Fullscreen);
            }
        }
    }
}
