// Assets/_Game/2. Scripts/UI/PortUpgradePanel.cs
using ThroneOfTides.Core;
using ThroneOfTides.Data;
using UnityEngine;

namespace ThroneOfTides.UI
{
    // Manages the three upgrade nodes and delegates purchase logic to PortManager
    // via the OnUpgradePurchased callback.
    public class PortUpgradePanel : MonoBehaviour
    {
        [SerializeField] private PortUpgradeNode _manaNode;
        [SerializeField] private PortUpgradeNode _hpNode;
        [SerializeField] private PortUpgradeNode _storageNode;

        private PlayerInventory          _inventory;
        private UpgradeSO                _manaUpgrade;
        private UpgradeSO                _hpUpgrade;
        private UpgradeSO                _storageUpgrade;
        private System.Action<UpgradeType> _onPurchased;

        // ── Init ───────────────────────────────────────────────────────────────

        public void Initialise(PlayerInventory inventory,
                               UpgradeSO manaUpgrade,
                               UpgradeSO hpUpgrade,
                               UpgradeSO storageUpgrade,
                               int baseMana, int baseHP, int baseStorage,
                               System.Action<UpgradeType> onPurchased)
        {
            _inventory      = inventory;
            _manaUpgrade    = manaUpgrade;
            _hpUpgrade      = hpUpgrade;
            _storageUpgrade = storageUpgrade;
            _onPurchased    = onPurchased;

            _manaNode.Initialise(manaUpgrade,    inventory, baseMana,    () => TryPurchase(UpgradeType.MaxMana));
            _hpNode.Initialise(hpUpgrade,        inventory, baseHP,      () => TryPurchase(UpgradeType.MaxHP));
            _storageNode.Initialise(storageUpgrade, inventory, baseStorage, () => TryPurchase(UpgradeType.MaxStorage));
        }

        // ── Purchase ───────────────────────────────────────────────────────────

        private void TryPurchase(UpgradeType type)
        {
            UpgradeSO upgrade = type switch
            {
                UpgradeType.MaxMana    => _manaUpgrade,
                UpgradeType.MaxHP      => _hpUpgrade,
                UpgradeType.MaxStorage => _storageUpgrade,
                _                     => null
            };

            if (upgrade == null) return;
            if (!_inventory.TryPurchaseUpgrade(upgrade)) return;

            RefreshAll();
            _onPurchased?.Invoke(type);
        }

        public void RefreshAll()
        {
            _manaNode.Refresh(_inventory.GetUpgradeLevel(UpgradeType.MaxMana));
            _hpNode.Refresh(_inventory.GetUpgradeLevel(UpgradeType.MaxHP));
            _storageNode.Refresh(_inventory.GetUpgradeLevel(UpgradeType.MaxStorage));
        }
    }
}