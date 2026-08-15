// Assets/_Game/2. Scripts/UI/ResultsPanel.cs
using System.Collections.Generic;
using System.Linq;
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
        [SerializeField] private TextMeshProUGUI _rewardsListingLabel;
        [SerializeField] private Transform       _rewardCardsContainer;

        [Header("Reward Card Views")]
        [Tooltip("Same CardView prefab used elsewhere (e.g. Port's collection grid) - spawned read-only, one per rewarded card, with a hover trigger wired to _previewTooltip.")]
        [SerializeField] private CardView           _rewardCardViewPrefab;
        [SerializeField] private float              _rewardCardViewScale = 0.55f;
        [SerializeField] private CardPreviewTooltip _previewTooltip;

        [Header("Buttons")]
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _mainMenuButton;
        [SerializeField] private Button _portButton;
        [SerializeField] private Button _levelSelectButton;

        private void Awake()
        {
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
            UpdateRewardsListing(result);
            SpawnRewardCardViews(result.RewardCards);
        }

        public void ShowLoss(LevelRewardSO reward, int playerHP)
        {
            gameObject.SetActive(true);
            _titleLabel.text = "Defeated";

            var result = MatchRewardGranter.GrantLoss(_playerInventory, reward, playerHP);
            _coinRewardLabel.text = $"+{result.Coins} Coins";
            UpdateRewardsListing(result);
            SpawnRewardCardViews(result.RewardCards);
        }

        private void UpdateRewardsListing(MatchRewardResult result)
        {
            if (_rewardsListingLabel == null) return;

            string coinsLine = $"+{result.Coins} Coins";
            _rewardsListingLabel.text = result.RewardCards.Count == 0
                ? coinsLine
                : $"{string.Join(", ", result.RewardCards.Select(c => c.Name))}\n{coinsLine}";
        }

        private void SpawnRewardCardViews(IReadOnlyList<CardSO> cards)
        {
            foreach (Transform child in _rewardCardsContainer)
                Destroy(child.gameObject);

            if (_rewardCardViewPrefab == null) return;

            foreach (var card in cards)
            {
                var cardView = Instantiate(_rewardCardViewPrefab, _rewardCardsContainer);
                cardView.transform.localScale = Vector3.one * _rewardCardViewScale;
                cardView.Setup(card);

                // Read-only display on the results screen - not draggable/playable.
                var drag = cardView.GetComponent<CardDragHandler>();
                if (drag != null) Destroy(drag);

                var trigger = cardView.GetComponent<CardPreviewTrigger>()
                              ?? cardView.gameObject.AddComponent<CardPreviewTrigger>();
                trigger.Setup(card, _previewTooltip);
            }
        }

        private void OnRetryPressed()
        {
            gameObject.SetActive(false);
            SceneFader.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnMainMenuPressed() => SceneFader.LoadScene("MainMenu");

        private void OnPortPressed() => SceneFader.LoadScene("Port");

        private void OnLevelSelectPressed() => SceneFader.LoadScene("LevelSelect");
    }
}