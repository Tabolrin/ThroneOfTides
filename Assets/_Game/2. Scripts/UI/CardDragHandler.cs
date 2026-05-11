using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ThroneOfTides.UI
{
    // Handles drag and drop for card UI elements
    // Card re-parents to DragCanvas during drag for correct sort order
    public class CardDragHandler : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Canvas    _dragCanvas;
        [SerializeField] private CanvasGroup _canvasGroup;

        private RectTransform _rectTransform;
        private Transform     _originalParent;
        private int           _originalSiblingIndex;
        private Vector2       _originalPosition;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _originalParent       = transform.parent;
            _originalSiblingIndex = transform.GetSiblingIndex();
            _originalPosition     = _rectTransform.anchoredPosition;

            // Move to drag canvas so card renders above everything
            transform.SetParent(_dragCanvas.transform, true);

            // Allow raycasts to pass through to PlayZone during drag
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

            // Snap back to hand if not dropped on a valid target
            transform.SetParent(_originalParent, true);
            transform.SetSiblingIndex(_originalSiblingIndex);
            _rectTransform.anchoredPosition = _originalPosition;
        }
        
        public void SetDragCanvas(Canvas canvas)
        {
            _dragCanvas = canvas;
        }
    }
}