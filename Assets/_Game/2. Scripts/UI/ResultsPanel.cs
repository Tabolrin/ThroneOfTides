// Assets/_Game/2. Scripts/UI/ResultsPanel.cs
using System.Collections.Generic;
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

            _progression.SetLevelBeaten(GameSession.SelectedLevelIndex);

            var rewardCards = new List<CardSO>(reward.RewardCards);
            _playerInventory.AddCards(rewardCards);

            int coins = reward.GetCoinReward(playerHP, isWin: true);
            _playerInventory.AddCoins(coins);
            _coinRewardLabel.text = $"+{coins} Coins";

            foreach (Transform child in _rewardCardsContainer)
                Destroy(child.gameObject);

            foreach (var card in rewardCards)
            {
                var label = Instantiate(_rewardCardNamePrefab, _rewardCardsContainer);
                label.text = card.Name;
            }
        }

        public void ShowLoss(LevelRewardSO reward, int playerHP)
        {
            gameObject.SetActive(true);
            _titleLabel.text = "Defeated";

            // Loss — coins only at 50%, no card reward
            int coins = reward.GetCoinReward(playerHP, isWin: false);
            _playerInventory.AddCoins(coins);
            _coinRewardLabel.text = $"+{coins} Coins";

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