// Assets/_Game/2. Scripts/UI/PortCardRow.cs
using TMPro;
using ThroneOfTides.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ThroneOfTides.UI
{
    // One row per card TYPE in the deck list (not one row per copy) - a stepper showing the
    // current copy count, a "−" that decrements (removing the row entirely once count hits 0)
    // and a "+" that increments (disabled once storage or the card's own MaxCopiesInDeck would
    // be exceeded). Mirrors DeckBuilderWindow's editor-only equivalent.
    // Prefab: HorizontalLayout root.
    public class PortCardRow : MonoBehaviour
    {
        [SerializeField] private Image            _typeBand;
        [SerializeField] private TextMeshProUGUI  _nameLabel;
        [SerializeField] private TextMeshProUGUI  _manaCostLabel;
        [SerializeField] private TextMeshProUGUI  _storageCostLabel;
        [SerializeField] private Button           _minusButton;
        [SerializeField] private TextMeshProUGUI  _countLabel;
        [SerializeField] private Button           _plusButton;

        public void Setup(CardSO card, int count, CardTypePaletteSO palette, bool canIncrement,
            System.Action onIncrement, System.Action onDecrement)
        {
            if (_typeBand != null && palette != null)
                _typeBand.color = palette.GetColor(card.CardType);

            if (_nameLabel        != null) _nameLabel.text        = card.Name;
            if (_manaCostLabel    != null) _manaCostLabel.text    = $"Mana: {card.ManaCost}";
            if (_storageCostLabel != null) _storageCostLabel.text = $"{card.StorageCost} slots";
            if (_countLabel       != null) _countLabel.text       = count.ToString();

            _plusButton.interactable = canIncrement;

            _minusButton.onClick.RemoveAllListeners();
            _minusButton.onClick.AddListener(() => onDecrement?.Invoke());

            _plusButton.onClick.RemoveAllListeners();
            if (canIncrement) _plusButton.onClick.AddListener(() => onIncrement?.Invoke());
        }
    }
}
