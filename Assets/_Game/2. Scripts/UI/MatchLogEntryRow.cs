// Assets/_Game/2. Scripts/UI/MatchLogEntryRow.cs
using TMPro;
using ThroneOfTides.Data;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ThroneOfTides.UI
{
    // One line in the match log. If Setup is given a card, hovering the line shows that card's
    // name/description via the shared EffectBadgeTooltip — reused as-is since it's already a
    // generic "text near this RectTransform" tooltip, not badge-specific.
    public class MatchLogEntryRow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TextMeshProUGUI _label;

        private RectTransform      _rect;
        private CardSO             _card;
        private EffectBadgeTooltip _tooltip;

        private void Awake() => _rect = GetComponent<RectTransform>();

        public void Setup(string text, CardSO card, EffectBadgeTooltip tooltip)
        {
            if (_label != null) _label.text = text;
            _card    = card;
            _tooltip = tooltip;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_card == null || _tooltip == null) return;
            _tooltip.Show($"{_card.Name}\n{_card.Description}", _rect);
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
