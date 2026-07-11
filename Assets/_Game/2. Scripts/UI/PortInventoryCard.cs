// Assets/_Game/2. Scripts/UI/PortInventoryCard.cs
using TMPro;
using ThroneOfTides.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ThroneOfTides.UI
{
    // One card panel in the collection grid.
    // Shows art, name, type, costs, owned/in-deck counts and an Add button.
    // Prefab: VerticalLayout root.
    public class PortInventoryCard : MonoBehaviour
    {
        // Type is conveyed via _typeLabel's text — no per-type background tint anymore.
        [SerializeField] private Image            _cardArt;
        [SerializeField] private TextMeshProUGUI  _nameLabel;
        [SerializeField] private TextMeshProUGUI  _typeLabel;
        [SerializeField] private TextMeshProUGUI  _manaCostLabel;
        [SerializeField] private TextMeshProUGUI  _storageCostLabel;
        [SerializeField] private TextMeshProUGUI  _ownedLabel;
        [SerializeField] private TextMeshProUGUI  _inDeckLabel;
        [SerializeField] private Button           _addButton;
        [SerializeField] private TextMeshProUGUI  _addButtonLabel;

        public void Setup(CardSO card,
                          int ownedCount, int inDeckCount,
                          bool canAdd, System.Action onAdd)
        {
            if (_cardArt != null)
            {
                _cardArt.gameObject.SetActive(card.Art != null);
                if (card.Art != null) _cardArt.sprite = card.Art;
            }

            if (_nameLabel        != null) _nameLabel.text        = card.Name;
            if (_typeLabel        != null) _typeLabel.text        = card.CardType.ToString();
            if (_manaCostLabel    != null) _manaCostLabel.text    = $"{card.ManaCost} mana";
            if (_storageCostLabel != null) _storageCostLabel.text = $"{card.StorageCost} slots";
            if (_ownedLabel       != null) _ownedLabel.text       = $"Owned: {ownedCount}";
            if (_inDeckLabel      != null) _inDeckLabel.text      = $"In deck: {inDeckCount}";

            _addButton.interactable = canAdd;

            if (_addButtonLabel != null)
            {
                _addButtonLabel.text = canAdd                   ? "Add"
                                     : inDeckCount >= ownedCount ? "All in deck"
                                                                 : "Storage full";
            }

            _addButton.onClick.RemoveAllListeners();
            if (canAdd) _addButton.onClick.AddListener(() => onAdd?.Invoke());
        }
    }
}