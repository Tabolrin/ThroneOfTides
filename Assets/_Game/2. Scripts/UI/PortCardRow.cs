// Assets/_Game/2. Scripts/UI/PortCardRow.cs
using TMPro;
using ThroneOfTides.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ThroneOfTides.UI
{
    // One card row in the deck list — type band, name, costs, remove button.
    // Prefab: HorizontalLayout root with 4 children.
    public class PortCardRow : MonoBehaviour
    {
        [SerializeField] private Image            _typeBand;
        [SerializeField] private TextMeshProUGUI  _nameLabel;
        [SerializeField] private TextMeshProUGUI  _manaCostLabel;
        [SerializeField] private TextMeshProUGUI  _storageCostLabel;
        [SerializeField] private Button           _removeButton;

        public void Setup(CardSO card, CardTypePaletteSO palette, System.Action onRemove)
        {
            if (_typeBand != null && palette != null)
                _typeBand.color = palette.GetColor(card.CardType);

            if (_nameLabel        != null) _nameLabel.text        = card.Name;
            if (_manaCostLabel    != null) _manaCostLabel.text    = $"{card.ManaCost}";
            if (_storageCostLabel != null) _storageCostLabel.text = $"{card.StorageCost} slots";

            _removeButton.onClick.RemoveAllListeners();
            _removeButton.onClick.AddListener(() => onRemove?.Invoke());
        }
    }
}