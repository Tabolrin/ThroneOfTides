using TMPro;
using UnityEngine;
using ThroneOfTides.Data;

namespace ThroneOfTides.UI
{
    public class TooltipController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _cardNameLabel;
        [SerializeField] private TextMeshProUGUI _descriptionLabel;
        [SerializeField] private Camera          _mainCamera;

        // World-unit offset above the card before converting to screen space
        [SerializeField] private Vector3 _worldOffset = new Vector3(0f, 1.5f, 0f);

        private RectTransform _rectTransform;

        private void Awake() => _rectTransform = GetComponent<RectTransform>();

        public void Show(CardSO card, Vector3 worldPosition)
        {
            _cardNameLabel.text    = card.Name;
            _descriptionLabel.text = card.Description;

            // Convert world position to screen space for Canvas overlay positioning
            Vector3 screenPos      = _mainCamera.WorldToScreenPoint(worldPosition + _worldOffset);
            _rectTransform.position = screenPos;

            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}