using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Game._2._Scripts.card
{
    [RequireComponent(typeof(RectTransform))]
    public class CardHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Scale")]
        [SerializeField] private float hoverScale = 1.15f;      // How much the card grows
        [SerializeField] private float scaleSpeed = 8f;          // Transition speed

        [Header("Frame")]
        [SerializeField] private Image frameImage;               // Drag the frame Image here
        [SerializeField] private Color frameColor = Color.yellow; // Changeable color
        [SerializeField] private float frameFadeSpeed = 10f;

        private Vector3 _originalScale;
        private Vector3 _targetScale;
        private Color _visibleColor;
        private Color _hiddenColor;

        private void Awake()
        {
            _originalScale = transform.localScale;
            _targetScale = _originalScale;

            if (frameImage != null)
            {
                _visibleColor = frameColor;
                _visibleColor.a = 1f;
                _hiddenColor = frameColor;
                _hiddenColor.a = 0f;
                frameImage.color = _hiddenColor;
            }
        }

        private void Update()
        {
            // Smoothly scale up/down
            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * scaleSpeed);

            // Fade the frame in/out
            if (frameImage != null)
            {
                frameImage.color = Color.Lerp(frameImage.color,
                    _targetScale == _originalScale ? _hiddenColor : _visibleColor,
                    Time.deltaTime * frameFadeSpeed);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _targetScale = _originalScale * hoverScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _targetScale = _originalScale;
        }

        // Call this at runtime to change the frame color dynamically
        // (e.g. a different color per card type)
        public void SetFrameColor(Color newColor)
        {
            frameColor = newColor;
            _visibleColor = newColor;
            _visibleColor.a = 1f;
            _hiddenColor = newColor;
            _hiddenColor.a = 0f;
        }
    }
}