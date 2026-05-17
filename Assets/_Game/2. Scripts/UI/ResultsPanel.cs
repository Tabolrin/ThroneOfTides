using System.Collections.Generic;
using TMPro;
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

        [Header("UI")]
        [SerializeField] private TextMeshProUGUI _titleLabel;
        [SerializeField] private TextMeshProUGUI _materialRewardLabel;
        [SerializeField] private Transform       _rewardCardsContainer;
        [SerializeField] private TextMeshProUGUI _rewardCardPrefab;

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

            // Grant reward cards to inventory
            var rewardCards = new List<CardSO>(reward.RewardCards);
            _playerInventory.AddCards(rewardCards);

            // Get material reward based on HP tier
            MaterialReward materials = reward.GetMaterialReward(playerHP, isWin: true);
            _playerInventory.AddMaterials(materials.Rum, materials.Shipwrecks);

            // Display material reward
            _materialRewardLabel.text = $"Rum: +{materials.Rum}    Shipwrecks: +{materials.Shipwrecks}";

            // Display reward cards
            foreach (Transform child in _rewardCardsContainer)
                Destroy(child.gameObject);

            foreach (var card in rewardCards)
            {
                var label = Instantiate(_rewardCardPrefab, _rewardCardsContainer);
                label.text = card.Name;
            }
        }

        public void ShowLoss(LevelRewardSO reward, int playerHP)
        {
            gameObject.SetActive(true);
            _titleLabel.text = "Defeated";

            // Loss - materials only at 50%, no card reward
            MaterialReward materials = reward.GetMaterialReward(playerHP, isWin: false);
            _playerInventory.AddMaterials(materials.Rum, materials.Shipwrecks);

            _materialRewardLabel.text = $"Rum: +{materials.Rum}    Shipwrecks: +{materials.Shipwrecks}";

            // Clear reward cards container - no cards on loss
            foreach (Transform child in _rewardCardsContainer)
                Destroy(child.gameObject);
        }

        private void OnRetryPressed()
        {
            gameObject.SetActive(false);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnMainMenuPressed() =>
            SceneManager.LoadScene("MainMenu");

        private void OnPortPressed()
        {
            // TODO - load Port scene when built
            Debug.Log("Port - not yet implemented");
        }

        private void OnLevelSelectPressed()
        {
            // TODO - load Level Select scene when built
            Debug.Log("Level Select - not yet implemented");
        }
    }
}