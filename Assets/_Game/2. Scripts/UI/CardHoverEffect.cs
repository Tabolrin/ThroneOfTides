// Assets/_Game/2. Scripts/UI/CardHoverEffect.cs
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ThroneOfTides.UI
{
    // Generic pointer-hover growth for a UI card: scales up and rises while hovered, optionally
    // jumping to the front of its parent's draw order, reverting on exit. Attach to a card-like
    // RectTransform that should react to hover without needing to be draggable/playable (e.g.
    // EnemyHandRevealPanel's read-only card display, or a hand card via Configure()).
    // Base scale/position are captured fresh on every hover-enter rather than once in OnEnable -
    // pooled hand cards get repositioned by hand-layout re-fanning throughout their lifetime, so
    // a one-time cached base would go stale and snap back to the wrong spot.
    public class CardHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private float _hoverScale    = 1.15f;
        [SerializeField] private float _hoverRise     = 30f;
        [SerializeField] private float _tweenDuration = 0.15f;
        [SerializeField] private Ease  _ease          = Ease.OutBack;
        [SerializeField] private bool  _bringToFront;

        private RectTransform _rect;
        private Vector3       _baseScale;
        private Vector2       _basePos;
        private int           _baseSiblingIndex;
        private bool          _isHovering;

        private void Awake() => _rect = GetComponent<RectTransform>();

        /// <summary>Overrides the inspector/AddComponent defaults at runtime.</summary>
        public void Configure(float hoverScale, float hoverRise, bool bringToFront)
        {
            _hoverScale    = hoverScale;
            _hoverRise     = hoverRise;
            _bringToFront  = bringToFront;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isHovering) return;
            _isHovering = true;

            // Complete (not just stop) any tween already running on this transform BEFORE
            // reading its current values - a plain DOKill() freezes a tween wherever it happened
            // to be mid-flight, so a hover-enter that arrives while the hand layout's gap-close
            // tween (or a previous hover's not-yet-finished shrink-back) is still animating would
            // otherwise capture that half-finished, wrong position/scale as "base". Every
            // subsequent enlarge/restore cycle would then compound on top of that wrong base,
            // which is how a card ends up visibly stuck enlarged or offset after rapid
            // hover/drag interactions.
            _rect.DOKill(true);

            _baseScale = _rect.localScale;
            _basePos   = _rect.anchoredPosition;

            if (_bringToFront)
            {
                _baseSiblingIndex = _rect.GetSiblingIndex();
                _rect.SetAsLastSibling();
            }

            _rect.DOScale(_baseScale * _hoverScale, _tweenDuration).SetEase(_ease);
            _rect.DOAnchorPos(_basePos + new Vector2(0f, _hoverRise), _tweenDuration).SetEase(_ease);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_isHovering) return;
            _isHovering = false;

            // See the matching comment in OnPointerEnter - completes any in-flight tween first so
            // this shrink-back always starts from the true enlarged state, not an interrupted one.
            _rect.DOKill(true);
            _rect.DOScale(_baseScale, _tweenDuration).SetEase(_ease);
            _rect.DOAnchorPos(_basePos, _tweenDuration).SetEase(_ease)
                .OnComplete(() =>
                {
                    // Restored only after the tween settles - reordering mid-tween would make
                    // an overlapping neighbor draw on top of the still-animating card.
                    if (_bringToFront) _rect.SetSiblingIndex(_baseSiblingIndex);
                });
        }

        // A card can be disabled mid-hover - played away, or released back to HandLayoutManager's
        // pool via DisableHover - with no OnPointerExit ever firing. Without restoring here too,
        // the card would stay at its enlarged scale/raised position; the *next* hover-enter would
        // then capture that already-enlarged state as its "base" and enlarge again on top of it,
        // compounding on every subsequent hover. Reset unconditionally (not just when
        // _isHovering) since a component freshly re-enabled by AddComponent/enabled=true should
        // never carry over a stale flag from a previous life either.
        private void OnDisable()
        {
            RestoreToBase();
            _isHovering = false;
        }

        // Called by CardDragHandler the instant a drag begins - a card picked up mid-hover (a
        // very common "hover to preview, then drag" flow) would otherwise start the drag still
        // enlarged/raised, and OnDrag's anchoredPosition += delta math assumes scale == 1, so a
        // lingering hover scale makes the card visibly drift away from the cursor. Snaps back
        // instantly (no tween) since a drag is starting this same frame.
        public void CancelHover()
        {
            if (!_isHovering) return;
            RestoreToBase();
            _isHovering = false;
        }

        // Snaps (no tween) straight back to the last captured base - shared by CancelHover and
        // OnDisable so there's exactly one place that knows how to fully undo the hover state.
        private void RestoreToBase()
        {
            _rect.DOKill();
            if (!_isHovering) return;

            _rect.localScale       = _baseScale;
            _rect.anchoredPosition = _basePos;

            // gameObject.activeSelf is still true here when OnDisable fired because a PARENT
            // (e.g. PlayerHandContainer) is being deactivated and cascaded down to us - Unity
            // forbids sibling-index changes during that cascade ("Cannot change sibling position
            // ... while activating or deactivating the parent"). Only reorder when this object
            // was deactivated directly (activeSelf already false by the time OnDisable runs),
            // e.g. HandLayoutManager releasing it back to its pool.
            if (_bringToFront && !gameObject.activeSelf) _rect.SetSiblingIndex(_baseSiblingIndex);
        }
    }
}
