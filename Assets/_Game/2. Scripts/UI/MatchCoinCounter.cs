// Assets/_Game/2. Scripts/UI/MatchCoinCounter.cs
using TMPro;
using ThroneOfTides.Data;
using UnityEngine;

namespace ThroneOfTides.UI
{
    // Live coin counter shown during a match — lets the player see coins gained/spent mid-fight
    // (Treasure Chest's reward, The Kraken's materials cost) rather than only finding out at the
    // Port. Purely a readout; never mutates PlayerInventory itself.
    public class MatchCoinCounter : MonoBehaviour
    {
        [SerializeField] private PlayerInventory  _playerInventory;
        [SerializeField] private TextMeshProUGUI  _coinsLabel;

        private void OnEnable()
        {
            if (_playerInventory == null) return;
            _playerInventory.OnCoinsChanged += Refresh;
            Refresh(_playerInventory.Coins);
        }

        private void OnDisable()
        {
            if (_playerInventory != null) _playerInventory.OnCoinsChanged -= Refresh;
        }

        private void Refresh(int coins)
        {
            if (_coinsLabel != null) _coinsLabel.text = coins.ToString();
        }
    }
}
