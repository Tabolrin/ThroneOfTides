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
    // Creates and wires all sub-panels; handles save, navigation, and
    // routing upgrade purchases back to the correct panels.
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
        [SerializeField] private TextMeshProUGUI _rumLabel;
        [SerializeField] private TextMeshProUGUI _shipwrecksLabel;

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

            // Upgrade panel — pass base values so nodes can display actual totals
            _upgradePanel.Initialise(
                _playerInventory,
                _manaUpgrade, _hpUpgrade, _storageUpgrade,
                _config.StartingMaxMana, _config.StartingHP, _config.BaseStorageCapacity,
                OnUpgradePurchased);

            // Deck editor — starts from the player's configured deck SO
            _deckEditor.Initialise(_playerInventory.PlayerDeck, effectiveStorage);
            _deckEditor.OnSaveRequested += SaveDeck;

            // Inventory view — reads collection, queries deck editor for counts
            _inventoryView.Initialise(_playerInventory, _deckEditor);
            _inventoryView.OnAddCardRequested += OnAddCardRequested;

            _mainMenuButton?.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));
            _levelSelectButton?.onClick.AddListener(() => SceneManager.LoadScene("LevelSelect"));

            RefreshMaterials();
        }

        private void OnDestroy()
        {
            if (_deckEditor    != null) _deckEditor.OnSaveRequested    -= SaveDeck;
            if (_inventoryView != null) _inventoryView.OnAddCardRequested -= OnAddCardRequested;
        }

        // ── Event Handlers ─────────────────────────────────────────────────────

        private void OnAddCardRequested(CardSO card)
        {
            bool added = _deckEditor.TryAddCard(card);

            if (!added)
                Debug.Log($"[Port] Cannot add {card.Name} — storage full " +
                          $"({_deckEditor.GetStorageUsed()} / {_deckEditor.MaxStorage})");

            // Refresh inventory so Add buttons reflect the new state
            _inventoryView.Refresh();
        }

        private void OnUpgradePurchased(UpgradeType type)
        {
            // If storage was upgraded, push new cap to the deck editor and
            // refresh inventory so previously-greyed Add buttons may re-enable
            if (type == UpgradeType.MaxStorage)
            {
                _deckEditor.UpdateMaxStorage(ComputeEffectiveStorage());
                _inventoryView.Refresh();
            }

            RefreshMaterials();
            // Upgrade panel refreshes itself internally after purchase
        }

        // ── Save ───────────────────────────────────────────────────────────────

        private void SaveDeck()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(_playerInventory.PlayerDeck);
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log("[Port] Deck saved to asset.");
#endif
            // TODO (post-vertical-slice): replace with JSON serialization for builds
            // The deck SO is already mutated in memory so the match will use the
            // updated configuration within this play session even without file-save.
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

        private void RefreshMaterials()
        {
            if (_rumLabel        != null) _rumLabel.text        = $"Rum: {_playerInventory.Rum}";
            if (_shipwrecksLabel != null) _shipwrecksLabel.text = $"Shipwrecks: {_playerInventory.Shipwrecks}";
        }
    }
}