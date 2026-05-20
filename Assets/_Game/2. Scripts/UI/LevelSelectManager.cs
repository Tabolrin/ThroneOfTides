using TMPro;
using ThroneOfTides.Data;
using UnityEngine;
using UnityEngine.SceneManagement;
using ThroneOfTides.Systems;
using UnityEngine.UI;

namespace ThroneOfTides.Systems
{
    public class LevelSelectManager : MonoBehaviour
    {
        [Header("Progression")]
        [SerializeField] private ProgressionSO _progression;

        [Header("Captains")]
        [SerializeField] private CaptainSO _captain1;
        [SerializeField] private CaptainSO _captain2;
        [SerializeField] private CaptainSO _captain3;

        [Header("Level Nodes")]
        [SerializeField] private Button          _level1Button;
        [SerializeField] private Button          _level2Button;
        [SerializeField] private Button          _level3Button;
        [SerializeField] private TextMeshProUGUI _level1Label;
        [SerializeField] private TextMeshProUGUI _level2Label;
        [SerializeField] private TextMeshProUGUI _level3Label;
        [SerializeField] private GameObject      _level2Lock;
        [SerializeField] private GameObject      _level3Lock;

        [Header("Navigation")]
        [SerializeField] private Button _mainMenuButton;

        private void Start()
        {
            RefreshNodes();

            _level1Button.onClick.AddListener(() => OnLevelSelected(_captain1, 1));
            _level2Button.onClick.AddListener(() => OnLevelSelected(_captain2, 2));
            _level3Button.onClick.AddListener(() => OnLevelSelected(_captain3, 3));
            _mainMenuButton.onClick.AddListener(OnMainMenuPressed);
        }

        private void RefreshNodes()
        {
            _level1Label.text = _captain1.CaptainName;
            _level2Label.text = _captain2.CaptainName;
            _level3Label.text = _captain3.CaptainName;

            _level2Button.interactable = _progression.Level2Unlocked;
            _level3Button.interactable = _progression.Level3Unlocked;

            _level2Lock.SetActive(!_progression.Level2Unlocked);
            _level3Lock.SetActive(!_progression.Level3Unlocked);
        }

        private void OnLevelSelected(CaptainSO captain, int levelIndex)
        {
            GameSession.SetLevel(captain, levelIndex);
            SceneManager.LoadScene("Game_Canvas");
        }

        private void OnMainMenuPressed() =>
            SceneManager.LoadScene("MainMenu");
    }
}