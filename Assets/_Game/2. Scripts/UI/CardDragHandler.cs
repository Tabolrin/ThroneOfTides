using UnityEngine;
using UnityEngine.EventSystems;

namespace ThroneOfTides.UI
{
    public class CardDragHandler : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Canvas      _dragCanvas;
        [SerializeField] private CanvasGroup _canvasGroup;

        private RectTransform _rectTransform;
        private Transform     _originalParent;
        private int           _originalSiblingIndex;
        private Vector2       _originalPosition;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        public void SetDragCanvas(Canvas canvas) => _dragCanvas = canvas;

        public void OnBeginDrag(PointerEventData eventData)
        {
            _originalParent       = transform.parent;
            _originalSiblingIndex = transform.GetSiblingIndex();
            _originalPosition     = _rectTransform.anchoredPosition;

            transform.SetParent(_dragCanvas.transform, true);
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.alpha          = 0.8f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            _rectTransform.anchoredPosition += eventData.delta / _dragCanvas.scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.alpha          = 1f;

            var cardView = GetComponent<CardView>();

            // Card was accepted - destroy it
            if (cardView != null && cardView.WasPlayed)
            {
                Destroy(gameObject);
                return;
            }

            // Reset being played flag on snap back
            if (cardView != null)
                cardView.IsBeingPlayed = false;

            // Snap back to original position in hand
            transform.SetParent(_originalParent, true);
            transform.SetSiblingIndex(_originalSiblingIndex);
            _rectTransform.anchoredPosition = _originalPosition;
        }
    }
}