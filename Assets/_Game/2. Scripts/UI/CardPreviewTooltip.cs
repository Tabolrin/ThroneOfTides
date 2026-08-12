// Assets/_Game/2. Scripts/UI/CardPreviewTooltip.cs
using UnityEngine;
using UnityEngine.UI;
using ThroneOfTides.Data;

namespace ThroneOfTides.UI
{
    // Large read-only copy of a card shown floating above the cursor while hovering a small
    // card view elsewhere (e.g. the Port collection grid). One shared instance, parented
    // directly under the scene's root Canvas rather than inside whatever ScrollRect/Mask the
    // hovered card lives in — so a preview configured larger than the scroll viewport is never
    // clipped by it.
    public class CardPreviewTooltip : MonoBehaviour
    {
        [SerializeField] private CardView _cardViewPrefab;
        [SerializeField] private RectTransform _rect;
        [SerializeField] private float _previewScale = 1.5f;
        [SerializeField] private Vector2 _offset = new Vector2(0f, 40f);

        private CardView _instance;

        private void Awake()
        {
            if (_rect == null) _rect = GetComponent<RectTransform>();

            // Must never intercept raycasts itself: the preview renders on top of (and often
            // overlaps) the small trigger area that summoned it, and its embedded CardView's
            // Images/Buttons are raycast targets by default. Without this, showing the preview
            // covers the trigger, which fires OnPointerExit and hides it, which re-exposes the
            // trigger and fires OnPointerEnter again — an enter/exit feedback loop that reads as
            // the tooltip rapidly flashing in and out the whole time it's hovered.
            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable   = false;

            gameObject.SetActive(false);
        }

        private void EnsureInstance()
        {
            if (_instance != null || _cardViewPrefab == null) return;

            _instance = Instantiate(_cardViewPrefab, _rect);
            _instance.transform.localScale = Vector3.one * _previewScale;

            // Read-only preview — not draggable/playable.
            var drag = _instance.GetComponent<CardDragHandler>();
            if (drag != null) Destroy(drag);
        }

        public void Show(CardSO card, RectTransform anchor)
        {
            if (card == null || anchor == null) return;

            EnsureInstance();
            if (_instance == null) return;

            _instance.Setup(card);
            gameObject.SetActive(true);
            _rect.position = anchor.position + (Vector3)_offset;

            // Size varies with _previewScale/card content, so the rect only reflects its true
            // footprint after layout catches up — rebuild now so the clamp below measures this
            // frame's size instead of whatever was left over from the previous Show().
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rect);
            ClampToScreen();
        }

        private void ClampToScreen()
        {
            // Lives on a Screen Space - Overlay canvas, where a RectTransform's world position
            // maps 1:1 to screen pixels regardless of CanvasScaler — so clamping against
            // Screen.width/height is enough to keep it fully visible.
            Vector2 size = _rect.rect.size * _rect.lossyScale;
            Vector3 pos  = _rect.position;
            pos.x = Mathf.Clamp(pos.x, size.x * _rect.pivot.x, Screen.width  - size.x * (1f - _rect.pivot.x));
            pos.y = Mathf.Clamp(pos.y, size.y * _rect.pivot.y, Screen.height - size.y * (1f - _rect.pivot.y));
            _rect.position = pos;
        }

        public void Hide()
        {
            // Guards against Unity's "fake null" for a component whose GameObject was destroyed
            // mid scene-teardown.
            if (this == null) return;
            gameObject.SetActive(false);
        }
    }
}
