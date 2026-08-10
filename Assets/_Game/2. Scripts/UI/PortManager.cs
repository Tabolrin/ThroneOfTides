// Assets/_Game/2. Scripts/App/PortManager.cs
using TMPro;
using ThroneOfTides.Core;
using ThroneOfTides.Data;
using ThroneOfTides.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ThroneOfTides.App
{
    // Scene composition root for the Port.
    public class PortManager : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private PlayerInventory _playerInventory;
        [SerializeField] private GameConfigSO    _config;

        [Header("Upgrade SOs")]
        [SerializeField] private UpgradeSO _manaUpgrade;
        [SerializeField] private UpgradeSO _hpUpgrade;
        [SerializeField] private UpgradeSO _storageUpgrade;

        [Header("UI Panels")]
        [SerializeField] private PortUpgradePanel  _upgradePanel;
        [SerializeField] private PortDeckEditor    _deckEditor;
        [SerializeField] private PortInventoryView _inventoryView;

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI _coinsLabel;

        [Header("Navigation")]
        [SerializeField] private Button _mainMenuButton;
        [SerializeField] private Button _levelSelectButton;

        // ── Unity ──────────────────────────────────────────────────────────────

        private void Start()
        {
            if (_playerInventory.PlayerDeck == null)
            {
                Debug.LogError("[PortManager] PlayerInventory has no PlayerDeck assigned. " +
                               "Assign a DeckDefinitionSO to PlayerInventory._playerDeck.");
                return;
            }

            int effectiveStorage = ComputeEffectiveStorage();

            _upgradePanel.Initialise(
                _playerInventory,
                _manaUpgrade, _hpUpgrade, _storageUpgrade,
                _config.StartingMaxMana, _config.StartingHP, _config.BaseStorageCapacity,
                OnUpgradePurchased);

            _deckEditor.Initialise(_playerInventory.PlayerDeck, effectiveStorage);
            _deckEditor.OnSaveRequested += SaveDeck;

            _inventoryView.Initialise(_playerInventory, _deckEditor);
            _inventoryView.OnAddCardRequested += OnAddCardRequested;

            _mainMenuButton?.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));
            _levelSelectButton?.onClick.AddListener(() => SceneManager.LoadScene("LevelSelect"));

            RefreshCoins();
        }

        private void OnDestroy()
        {
            if (_deckEditor    != null) _deckEditor.OnSaveRequested       -= SaveDeck;
            if (_inventoryView != null) _inventoryView.OnAddCardRequested -= OnAddCardRequested;
        }

        // ── Event Handlers ─────────────────────────────────────────────────────

        private void OnAddCardRequested(CardSO card)
        {
            bool added = _deckEditor.TryAddCard(card);

            if (!added)
                Debug.Log($"[Port] Cannot add {card.Name} — storage full " +
                          $"({_deckEditor.GetStorageUsed()} / {_deckEditor.MaxStorage})");

            _inventoryView.Refresh();
        }

        private void OnUpgradePurchased(UpgradeType type)
        {
            if (type == UpgradeType.MaxStorage)
            {
                _deckEditor.UpdateMaxStorage(ComputeEffectiveStorage());
                _inventoryView.Refresh();
            }

            RefreshCoins();
        }

        // ── Save ───────────────────────────────────────────────────────────────

        private void SaveDeck()
        {
            // Persists to disk (see PlayerInventory.Save) so the deck survives an actual build,
            // not just the current Editor session.
            _playerInventory.Save();
            Debug.Log("[Port] Deck saved.");
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private int ComputeEffectiveStorage()
        {
            int bonus = _storageUpgrade != null
                ? _storageUpgrade.GetValueAtLevel(
                    _playerInventory.GetUpgradeLevel(UpgradeType.MaxStorage))
                : 0;
            return _config.BaseStorageCapacity + bonus;
        }

        private void RefreshCoins()
        {
            if (_coinsLabel != null)
                _coinsLabel.text = $"Coins: {_playerInventory.Coins}";
        }
    }
}