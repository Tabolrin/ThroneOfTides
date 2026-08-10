// Assets/_Game/2. Scripts/UI/CardDragHandler.cs
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ThroneOfTides.UI
{
    public class CardDragHandler : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Canvas      _dragCanvas;
        [SerializeField] private CanvasGroup _canvasGroup;

        // Fired when drag starts — HandLayoutManager closes the gap
        public Action<CardView> OnDragStarted;
        // Fired when drag ends — HandLayoutManager reopens or confirms removal
        public Action<CardView> OnDragEnded;

        private RectTransform    _rectTransform;
        private RectTransform    _dragCanvasRect;
        private Transform        _originalParent;
        private int              _originalSiblingIndex;
        private Vector2          _originalPosition;
        // Offset between the pointer and the card's anchored position, captured once at drag
        // start (in drag-canvas local space) — added back every frame so the card keeps whatever
        // offset it was grabbed at instead of snapping its pivot under the cursor.
        private Vector2          _pointerToCardOffset;
        private CardView         _cardView;
        private CardHoverEffect  _hoverEffect;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _cardView      = GetComponent<CardView>();
            _hoverEffect   = GetComponent<CardHoverEffect>();
        }

        public void SetDragCanvas(Canvas canvas) => _dragCanvas = canvas;

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Must happen before capturing original position/scale below — otherwise a card
            // picked up mid-hover snaps back to its enlarged/raised hover state instead of its
            // true resting slot when a drag is later cancelled.
            _hoverEffect?.CancelHover();

            _originalParent       = transform.parent;
            _originalSiblingIndex = transform.GetSiblingIndex();
            _originalPosition     = _rectTransform.anchoredPosition;

            // worldPositionStays reparenting recalculates local scale to preserve *world* scale,
            // so this must be reset after moving to _dragCanvas, not before — the hand containers
            // and the drag canvas apply different local scales, so resetting beforehand just gets
            // overwritten by the reparent itself. OnDrag's anchoredPosition += delta math assumes
            // local scale == 1 relative to the drag canvas; a card that carries over any other
            // scale (a lingering hover scale, or a stale scale from being reparented in from
            // another hand e.g. Monkey Grab) would otherwise visibly drift away from the cursor.
            transform.SetParent(_dragCanvas.transform, true);
            transform.localScale = Vector3.one;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.alpha          = 0.8f;

            // Recomputed from the absolute pointer position every frame in OnDrag rather than
            // accumulating eventData.delta frame-to-frame — delta accumulation can drift out of
            // sync with the actual cursor position under fast mouse movement (dropped/coalesced
            // move events), producing a growing offset the longer/faster a drag goes on.
            _dragCanvasRect = _dragCanvas.transform as RectTransform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _dragCanvasRect, eventData.position, null, out var pointerLocal);
            _pointerToCardOffset = _rectTransform.anchoredPosition - pointerLocal;

            // Notify HandLayoutManager — triggers gap close
            OnDragStarted?.Invoke(_cardView);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // null camera is correct here — _dragCanvas is Screen Space Overlay (see
            // CardPresentationPlayer's WorldToCanvasLocalPoint for the same rule/rationale).
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _dragCanvasRect, eventData.position, null, out var pointerLocal);
            _rectTransform.anchoredPosition = pointerLocal + _pointerToCardOffset;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.alpha          = 1f;

            // Card was accepted by play zone — release-to-pool handled by HandLayoutManager
            // via OnDragEnded (it owns the CardView pool and knows the card's release state).
            if (_cardView != null && _cardView.WasPlayed)
            {
                OnDragEnded?.Invoke(_cardView);
                return;
            }

            if (_cardView != null)
                _cardView.IsBeingPlayed = false;

            // Snap back to original position in hand
            transform.SetParent(_originalParent, true);
            transform.localScale = Vector3.one; // see the matching comment in OnBeginDrag
            transform.SetSiblingIndex(_originalSiblingIndex);
            _rectTransform.anchoredPosition = _originalPosition;

            // Notify HandLayoutManager — triggers slot reopen
            OnDragEnded?.Invoke(_cardView);
        }
    }
}