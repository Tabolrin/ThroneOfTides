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
        [SerializeField] private TextMeshProUGUI _materialRewardLabel;
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

            // Mark level beaten in progression
            _progression.SetLevelBeaten(GameSession.SelectedLevelIndex);

            // Grant reward cards
            var rewardCards = new List<CardSO>(reward.RewardCards);
            _playerInventory.AddCards(rewardCards);

            // Grant materials based on HP tier
            MaterialReward materials = reward.GetMaterialReward(playerHP, isWin: true);
            _playerInventory.AddMaterials(materials.Rum, materials.Shipwrecks);

            _materialRewardLabel.text = $"Rum: +{materials.Rum}    Shipwrecks: +{materials.Shipwrecks}";

            // Display reward card names
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

            // Loss - materials only at 50%, no card reward
            MaterialReward materials = reward.GetMaterialReward(playerHP, isWin: false);
            _playerInventory.AddMaterials(materials.Rum, materials.Shipwrecks);

            _materialRewardLabel.text = $"Rum: +{materials.Rum}    Shipwrecks: +{materials.Shipwrecks}";

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

        private void OnLevelSelectPressed() =>
            SceneManager.LoadScene("LevelSelect");
    }
}