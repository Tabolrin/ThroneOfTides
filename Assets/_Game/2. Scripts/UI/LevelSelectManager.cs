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

        [Header("Port")]
        [Tooltip("The Port is always optional and freely revisitable from the map — never gated behind a level and never accessible from the main menu, so this is the only entry point.")]
        [SerializeField] private Button     _portButton;
        [Tooltip("Small indicator shown on the Port node when there's coin or an unused unlocked card worth spending — a nudge, not a gate.")]
        [SerializeField] private GameObject _portNotificationBadge;
        [SerializeField] private PlayerInventory _playerInventory;

        [Header("Navigation")]
        [SerializeField] private Button _mainMenuButton;

        private void Start()
        {
            RefreshNodes();
            RefreshPortBadge();

            _level1Button.onClick.AddListener(() => OnLevelSelected(_captain1, 1));
            _level2Button.onClick.AddListener(() => OnLevelSelected(_captain2, 2));
            _level3Button.onClick.AddListener(() => OnLevelSelected(_captain3, 3));
            if (_portButton != null) _portButton.onClick.AddListener(OnPortSelected);
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
            SceneManager.LoadScene("Match");
        }

        private void OnPortSelected() => SceneManager.LoadScene("Port");

        private void OnMainMenuPressed() =>
            SceneManager.LoadScene("MainMenu");

        // A nudge, not a gate — lights up when there's coin sitting unspent or a card the
        // player has unlocked but never actually put in their deck.
        private void RefreshPortBadge()
        {
            if (_portNotificationBadge == null || _playerInventory == null) return;

            bool hasUnspentCoins = _playerInventory.Coins > 0;
            bool hasUnusedCard   = false;

            if (_playerInventory.PlayerDeck != null)
            {
                foreach (var card in _playerInventory.Collection)
                {
                    if (!_playerInventory.PlayerDeck.Cards.Exists(e => e.Card == card))
                    {
                        hasUnusedCard = true;
                        break;
                    }
                }
            }

            _portNotificationBadge.SetActive(hasUnspentCoins || hasUnusedCard);
        }
    }
}