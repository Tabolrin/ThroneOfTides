// Assets/_Game/2. Scripts/UI/EffectBadgeTooltip.cs
using TMPro;
using UnityEngine;

namespace ThroneOfTides.UI
{
    /// <summary>
    /// Small floating tooltip shown above a hovered persistent-effect badge, explaining what it
    /// means in one short line. One shared instance reused by every EffectBadgeView on both
    /// ActiveEffectsBars — position it under the same Canvas as the badges.
    /// </summary>
    public class EffectBadgeTooltip : MonoBehaviour
    {
        [SerializeField] private RectTransform   _rect;
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private Vector2         _offset = new Vector2(0f, 40f);

        private void Awake() => gameObject.SetActive(false);

        public void Show(string text, RectTransform anchor)
        {
            if (string.IsNullOrEmpty(text) || anchor == null) return;
            gameObject.SetActive(true);
            _label.text = text;
            _rect.position = anchor.position + (Vector3)_offset;
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
