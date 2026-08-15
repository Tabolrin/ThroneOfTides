// Assets/_Game/2. Scripts/UI/PortUpgradeNode.cs
using TMPro;
using ThroneOfTides.Core;
using ThroneOfTides.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ThroneOfTides.UI
{
    // One upgrade node - icon, level pips, current value, next value preview, buy button.
    // Driven entirely by data pushed from PortUpgradePanel.
    public class PortUpgradeNode : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private Image           _icon;
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private TextMeshProUGUI _descriptionLabel;

        [Header("Level Display")]
        [SerializeField] private TextMeshProUGUI _levelLabel;
        [SerializeField] private Image[]         _levelPips;
        private static readonly Color PipFilled = new Color(1.00f, 0.85f, 0.20f);
        private static readonly Color PipEmpty  = new Color(0.25f, 0.25f, 0.25f, 0.8f);

        [Header("Value Display")]
        [SerializeField] private TextMeshProUGUI _currentValueLabel;
        [SerializeField] private TextMeshProUGUI _nextValueLabel;

        [Header("Purchase")]
        [SerializeField] private TextMeshProUGUI _costLabel;
        [SerializeField] private Button          _buyButton;
        [SerializeField] private GameObject      _maxedDisplay;

        private UpgradeSO       _upgrade;
        private PlayerInventory _inventory;
        private int             _baseValue;
        private System.Action   _onBuyClicked;

        // ── Setup ──────────────────────────────────────────────────────────────

        public void Initialise(UpgradeSO upgrade, PlayerInventory inventory,
                               int baseValue, System.Action onBuyClicked)
        {
            _upgrade      = upgrade;
            _inventory    = inventory;
            _baseValue    = baseValue;
            _onBuyClicked = onBuyClicked;

            if (_icon != null && upgrade.Icon != null) _icon.sprite = upgrade.Icon;
            if (_nameLabel != null)        _nameLabel.text        = upgrade.DisplayName;
            if (_descriptionLabel != null) _descriptionLabel.text = upgrade.Description;

            _buyButton.onClick.AddListener(() => _onBuyClicked?.Invoke());

            Refresh(inventory.GetUpgradeLevel(upgrade.Type));
        }

        // Called by PortUpgradePanel after any purchase changes the state
        public void Refresh(int currentLevel)
        {
            bool isMaxed = _upgrade.IsMaxed(currentLevel);

            if (_levelLabel != null)
                _levelLabel.text = $"Lv {currentLevel} / {_upgrade.MaxLevel}";

            RefreshPips(currentLevel);

            int currentTotal = _baseValue + _upgrade.GetValueAtLevel(currentLevel);
            if (_currentValueLabel != null)
                _currentValueLabel.text = currentTotal.ToString();

            if (isMaxed)
            {
                _buyButton.gameObject.SetActive(false);
                if (_maxedDisplay   != null) _maxedDisplay.SetActive(true);
                if (_costLabel      != null) _costLabel.gameObject.SetActive(false);
                if (_nextValueLabel != null) _nextValueLabel.gameObject.SetActive(false);
            }
            else
            {
                _buyButton.gameObject.SetActive(true);
                if (_maxedDisplay != null) _maxedDisplay.SetActive(false);

                int  cost      = _upgrade.GetCoinCostToLevel(currentLevel);
                bool canAfford = _inventory.CanAfford(cost);

                _buyButton.interactable = canAfford;

                if (_costLabel != null)
                {
                    _costLabel.gameObject.SetActive(true);
                    _costLabel.text  = $"{cost} Coins";
                    _costLabel.color = canAfford ? Color.white : new Color(1f, 0.35f, 0.35f);
                }

                if (_nextValueLabel != null)
                {
                    int nextTotal = _baseValue + _upgrade.GetValueAtLevel(currentLevel + 1);
                    _nextValueLabel.gameObject.SetActive(true);
                    _nextValueLabel.text = $"\u2192 {nextTotal}";
                }
            }
        }

        private void RefreshPips(int currentLevel)
        {
            for (int i = 0; i < _levelPips.Length; i++)
                _levelPips[i].color = i < currentLevel ? PipFilled : PipEmpty;
        }
    }
}