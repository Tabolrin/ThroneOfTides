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

        private RectTransform _rectTransform;
        private Transform     _originalParent;
        private int           _originalSiblingIndex;
        private Vector2       _originalPosition;
        private CardView      _cardView;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _cardView      = GetComponent<CardView>();
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

            // Notify HandLayoutManager — triggers gap close
            OnDragStarted?.Invoke(_cardView);
        }

        public void OnDrag(PointerEventData eventData)
        {
            _rectTransform.anchoredPosition += eventData.delta / _dragCanvas.scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.alpha          = 1f;

            // Card was accepted by play zone — destroy handled by caller
            if (_cardView != null && _cardView.WasPlayed)
            {
                OnDragEnded?.Invoke(_cardView);
                Destroy(gameObject);
                return;
            }

            if (_cardView != null)
                _cardView.IsBeingPlayed = false;

            // Snap back to original position in hand
            transform.SetParent(_originalParent, true);
            transform.SetSiblingIndex(_originalSiblingIndex);
            _rectTransform.anchoredPosition = _originalPosition;

            // Notify HandLayoutManager — triggers slot reopen
            OnDragEnded?.Invoke(_cardView);
        }
    }
}