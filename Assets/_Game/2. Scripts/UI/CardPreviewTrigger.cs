// Assets/_Game/2. Scripts/UI/CardPreviewTrigger.cs
using UnityEngine;
using UnityEngine.EventSystems;
using ThroneOfTides.Data;

namespace ThroneOfTides.UI
{
    // Attached to just the embedded card-view area of a card panel (e.g. PortInventoryCard's
    // CardViewSlot) so the large preview only triggers when hovering the card art itself, not
    // the surrounding storage-cost/in-deck labels or the Add button.
    public class CardPreviewTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private RectTransform      _rect;
        private CardSO             _card;
        private CardPreviewTooltip _tooltip;

        private void Awake() => _rect = GetComponent<RectTransform>();

        public void Setup(CardSO card, CardPreviewTooltip tooltip)
        {
            _card    = card;
            _tooltip = tooltip;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_tooltip != null) _tooltip.Show(_card, _rect);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_tooltip != null) _tooltip.Hide();
        }

        private void OnDisable()
        {
            if (_tooltip != null) _tooltip.Hide();
        }
    }
}
