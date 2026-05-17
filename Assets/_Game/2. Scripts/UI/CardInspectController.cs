using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ThroneOfTides.UI
{
    // Manages the card inspect overlay - animates the actual CardView to center screen
    public class CardInspectController : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image           _overlay;
        [SerializeField] private Image           _descriptionPanel;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private Canvas          _gameCanvas;

        [Header("Animation")]
        [SerializeField] private float _openDuration  = 0.25f;
        [SerializeField] private float _closeDuration = 0.15f;
        [SerializeField] private float _inspectScale  = 2.5f;
        [SerializeField] private float _inspectCardVerticalOffset  = 2.5f;

        private CardView      _currentCard;
        private Transform     _originalParent;
        private int           _originalSiblingIndex;
        private Vector2       _originalPosition;
        private Vector3       _originalScale;
        private Vector2       _inspectPosition;
        private CanvasGroup   _descriptionCanvasGroup;

        public static CardInspectController Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            _descriptionCanvasGroup = _descriptionPanel.GetComponent<CanvasGroup>();
            if (_descriptionCanvasGroup == null)
                _descriptionCanvasGroup = _descriptionPanel.gameObject.AddComponent<CanvasGroup>();

            _overlay.gameObject.SetActive(false);
            _descriptionPanel.gameObject.SetActive(false);
            _inspectPosition = new Vector2(0, _inspectCardVerticalOffset);
        }

        public void Show(CardView card)
        {
            if (_currentCard != null) return;

            _currentCard          = card;
            _originalParent       = card.transform.parent;
            _originalSiblingIndex = card.transform.GetSiblingIndex();
            _originalPosition     = card.GetComponent<RectTransform>().anchoredPosition;
            _originalScale        = card.transform.localScale;

            // Re-parent to GameCanvas so it renders above overlay
            card.transform.SetParent(_gameCanvas.transform, true);
            card.transform.SetAsLastSibling();

            // Fill description from card data
            _descriptionText.text = card.CardData.Description;

            // Activate overlay and description
            _overlay.gameObject.SetActive(true);
            _descriptionPanel.gameObject.SetActive(true);

            var cardRect = card.GetComponent<RectTransform>();

            // Fade overlay in
            _overlay.color = new Color(0f, 0f, 0f, 0f);
            _overlay.DOFade(0.8f, _openDuration);

            // Description fades in slightly after card arrives
            _descriptionCanvasGroup.alpha = 0f;
            _descriptionCanvasGroup.DOFade(1f, _openDuration).SetDelay(_openDuration * 0.5f);

            // Move card to center and scale up simultaneously
            cardRect.DOAnchorPos(_inspectPosition, _openDuration).SetEase(Ease.OutCubic);
            card.transform.DOScale(_originalScale * _inspectScale, _openDuration).SetEase(Ease.OutBack);
        }

        public void Hide()
        {
            if (_currentCard == null) return;

            var card     = _currentCard;
            var cardRect = card.GetComponent<RectTransform>();

            // Fade overlay and description out
            _overlay.DOFade(0f, _closeDuration);
            _descriptionCanvasGroup.DOFade(0f, _closeDuration);

            // Re-parent back to original hand position
            card.transform.SetParent(_originalParent, true);
            card.transform.SetSiblingIndex(_originalSiblingIndex);
            
            // Move card back and scale down simultaneously
            cardRect.DOAnchorPos(_originalPosition, _closeDuration)
                    .SetEase(Ease.InCubic);

            card.transform.DOScale(_originalScale, _closeDuration)
                .SetEase(Ease.InCubic)
                .OnComplete(() =>
                {
                    _overlay.gameObject.SetActive(false);
                    _descriptionPanel.gameObject.SetActive(false);
                    _currentCard = null;
                });
        }

        // Click on overlay closes inspect
        public void OnPointerClick(PointerEventData eventData) => Hide();
    }
}