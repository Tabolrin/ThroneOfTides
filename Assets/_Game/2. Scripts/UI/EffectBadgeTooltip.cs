// Assets/_Game/2. Scripts/UI/EffectBadgeTooltip.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
            if (this == null || string.IsNullOrEmpty(text) || anchor == null) return;
            gameObject.SetActive(true);
            _label.text = text;
            _rect.position = anchor.position + (Vector3)_offset;

            // Text length varies per card/effect, so the rect only reflects its true footprint
            // after layout catches up with the new text — rebuild now so the clamp below measures
            // this frame's size instead of whatever was left over from the previous Show().
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rect);
            ClampToScreen();
        }

        private void ClampToScreen()
        {
            // Lives on a Screen Space - Overlay canvas (TopUICanvas), where a RectTransform's
            // world position maps 1:1 to screen pixels regardless of CanvasScaler — so clamping
            // against Screen.width/height is enough to keep it fully visible without needing a
            // Canvas reference or any camera/viewport math.
            Vector2 size = _rect.rect.size * _rect.lossyScale;
            Vector3 pos  = _rect.position;
            pos.x = Mathf.Clamp(pos.x, size.x * _rect.pivot.x, Screen.width  - size.x * (1f - _rect.pivot.x));
            pos.y = Mathf.Clamp(pos.y, size.y * _rect.pivot.y, Screen.height - size.y * (1f - _rect.pivot.y));
            _rect.position = pos;
        }

        public void Hide()
        {
            // Guards against Unity's "fake null": callers may still hold a reference to this
            // component after its GameObject was destroyed (e.g. mid scene-teardown ordering),
            // and `this == null` — unlike a bare `?.` at the call site — respects Unity's
            // overloaded equality check that correctly detects that case.
            if (this == null) return;
            gameObject.SetActive(false);
        }
    }
}
