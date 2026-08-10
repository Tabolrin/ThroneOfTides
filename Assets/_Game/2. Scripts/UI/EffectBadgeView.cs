// Assets/_Game/2. Scripts/UI/EffectBadgeView.cs
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ThroneOfTides.UI
{
    // One instantiated badge — an icon plus an optional count label. Spawned on demand by
    // ActiveEffectsBar when a status/reaction becomes active, destroyed when it clears.
    // Hovering shows a short explanation via the shared EffectBadgeTooltip.
    public class EffectBadgeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image           _icon;
        [SerializeField] private TextMeshProUGUI _countLabel;

        private RectTransform       _rect;
        private string              _description;
        private EffectBadgeTooltip  _tooltip;

        private void Awake() => _rect = GetComponent<RectTransform>();

        public void Setup(Sprite icon, int? count, string description = null, EffectBadgeTooltip tooltip = null)
        {
            if (_icon != null)
            {
                _icon.sprite = icon;
                _icon.enabled = icon != null;
            }

            if (_countLabel != null)
                _countLabel.text = count.HasValue ? count.Value.ToString() : string.Empty;

            _description = description;
            _tooltip     = tooltip;
        }

        public void OnPointerEnter(PointerEventData eventData) => _tooltip?.Show(_description, _rect);
        public void OnPointerExit(PointerEventData eventData)  => _tooltip?.Hide();

        private void OnDisable() => _tooltip?.Hide();
    }
}
