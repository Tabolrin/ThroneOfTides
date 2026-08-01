// Assets/_Game/2. Scripts/UI/ResultsPanel.cs
using TMPro;
using ThroneOfTides.Systems;
using ThroneOfTides.Data;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ThroneOfTides.UI
{
    public class ResultsPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInventory _playerInventory;
        [SerializeField] private ProgressionSO   _progression;

        [Header("UI")]
        [SerializeField] private TextMeshProUGUI _titleLabel;
        [SerializeField] private TextMeshProUGUI _coinRewardLabel;
        [SerializeField] private Transform       _rewardCardsContainer;
        [SerializeField] private TextMeshProUGUI _rewardCardNamePrefab;

        [Header("Buttons")]
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _mainMenuButton;
        [SerializeField] private Button _portButton;
        [SerializeField] private Button _levelSelectButton;

        private void Awake()
        {
            gameObject.SetActive(false);

            _retryButton.onClick.AddListener(OnRetryPressed);
            _mainMenuButton.onClick.AddListener(OnMainMenuPressed);
            _portButton.onClick.AddListener(OnPortPressed);
            _levelSelectButton.onClick.AddListener(OnLevelSelectPressed);
        }

        public void ShowWin(LevelRewardSO reward, int playerHP)
        {
            gameObject.SetActive(true);
            _titleLabel.text = "Victory!";

            var result = MatchRewardGranter.GrantWin(
                _progression, _playerInventory, reward, GameSession.SelectedLevelIndex, playerHP);

            _coinRewardLabel.text = $"+{result.Coins} Coins";

            foreach (Transform child in _rewardCardsContainer)
                Destroy(child.gameObject);

            foreach (var card in result.RewardCards)
            {
                var label = Instantiate(_rewardCardNamePrefab, _rewardCardsContainer);
                label.text = card.Name;
            }
        }

        public void ShowLoss(LevelRewardSO reward, int playerHP)
        {
            gameObject.SetActive(true);
            _titleLabel.text = "Defeated";

            var result = MatchRewardGranter.GrantLoss(_playerInventory, reward, playerHP);
            _coinRewardLabel.text = $"+{result.Coins} Coins";

            foreach (Transform child in _rewardCardsContainer)
                Destroy(child.gameObject);
        }

        private void OnRetryPressed()
        {
            gameObject.SetActive(false);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnMainMenuPressed() => SceneManager.LoadScene("MainMenu");

        private void OnPortPressed() => SceneManager.LoadScene("Port");

        private void OnLevelSelectPressed() => SceneManager.LoadScene("LevelSelect");
    }
}