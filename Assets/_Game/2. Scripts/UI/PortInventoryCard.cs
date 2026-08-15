// Assets/_Game/2. Scripts/UI/PortInventoryCard.cs
using TMPro;
using ThroneOfTides.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ThroneOfTides.UI
{
    // One card panel in the collection grid - shows the card exactly as it renders in hand
    // (embeds a real CardView) plus Port-specific info (storage cost, current/limit copies in
    // deck) and an Add button below it. Pooled by PortInventoryView, so the embedded CardView is
    // created once per panel instance and just re-Setup() on every refresh.
    public class PortInventoryCard : MonoBehaviour
    {
        [SerializeField] private CardView  _cardViewPrefab;
        [SerializeField] private Transform _cardViewParent;
        [SerializeField] private float     _cardViewScale = 0.55f;

        [SerializeField] private TextMeshProUGUI _storageCostLabel;
        [SerializeField] private TextMeshProUGUI _inDeckLabel;
        [SerializeField] private Button          _addButton;
        [SerializeField] private TextMeshProUGUI _addButtonLabel;

        private CardView            _cardViewInstance;
        private CardPreviewTrigger  _previewTrigger;

        // maxCopies is CardSO.MaxCopiesInDeck - ownership is unlocked-or-not, not a copy count,
        // so what's worth showing here is the deck-building cap, not how many you "own".
        public void Setup(CardSO card, int maxCopies, int inDeckCount, bool canAdd, System.Action onAdd,
            CardPreviewTooltip previewTooltip = null)
        {
            EnsureCardView();
            _cardViewInstance?.Setup(card);

            if (_previewTrigger == null && _cardViewParent != null)
                _previewTrigger = _cardViewParent.GetComponent<CardPreviewTrigger>()
                                  ?? _cardViewParent.gameObject.AddComponent<CardPreviewTrigger>();
            _previewTrigger?.Setup(card, previewTooltip);

            if (_storageCostLabel != null)
                _storageCostLabel.text = $"{card.StorageCost} slots";

            if (_inDeckLabel != null)
                _inDeckLabel.text = maxCopies >= 99
                    ? $"In deck: {inDeckCount}"
                    : $"In deck: {inDeckCount} / {maxCopies}";

            _addButton.interactable = canAdd;

            if (_addButtonLabel != null)
            {
                _addButtonLabel.text = canAdd                  ? "Add to Deck"
                                     : inDeckCount >= maxCopies ? "Limit reached"
                                                                : "Storage full";
            }

            _addButton.onClick.RemoveAllListeners();
            if (canAdd) _addButton.onClick.AddListener(() => onAdd?.Invoke());
        }

        private void EnsureCardView()
        {
            if (_cardViewInstance != null || _cardViewPrefab == null || _cardViewParent == null) return;

            _cardViewInstance = Instantiate(_cardViewPrefab, _cardViewParent);
            _cardViewInstance.transform.localScale = Vector3.one * _cardViewScale;

            // Read-only display - not draggable/playable, and has no inspect-overlay wired here.
            var drag = _cardViewInstance.GetComponent<CardDragHandler>();
            if (drag != null) Destroy(drag);
        }
    }
}
