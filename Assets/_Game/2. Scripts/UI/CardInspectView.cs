using DG.Tweening;
using TMPro;
using ThroneOfTides.Core;
using ThroneOfTides.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ThroneOfTides.UI
{
    public class CardInspectView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image           _overlay;
        [SerializeField] private RectTransform   _cardView;
        [SerializeField] private Image           _cardArt;
        [SerializeField] private Image           _cardFrame;
        [SerializeField] private TextMeshProUGUI _cardName;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private GameObject      _damageBadge;
        [SerializeField] private TextMeshProUGUI _damageLabel;

        [Header("Animation")]
        [SerializeField] private float _openDuration  = 0.25f;
        [SerializeField] private float _closeDuration = 0.18f;

        private void Awake() => gameObject.SetActive(false);

        public void Show(CardSO card)
        {
            gameObject.SetActive(true);

            _cardName.text        = card.Name;
            _descriptionText.text = card.Description;

            bool hasDamage = card.Damage > 0 ||
                             card.CardType == CardType.Combo ||
                             card.CardType == CardType.DOT;
            _damageBadge.SetActive(hasDamage);

            if (hasDamage)
                _damageLabel.text = card.CardType == CardType.DOT
                    ? $"{card.DotDamagePerTurn}x{card.DotDuration}"
                    : card.Damage.ToString();

            if (card.Art != null)
                _cardArt.sprite = card.Art;

            if (_cardFrame != null)
            {
                _cardFrame.color = card.CardType switch
                {
                    CardType.Weapon => new Color(0.3f, 0.5f, 1f),
                    CardType.Combo  => new Color(1f, 0.85f, 0f),
                    CardType.Action => new Color(0.3f, 0.8f, 0.4f),
                    CardType.DOT    => new Color(0.8f, 0.3f, 0.3f),
                    _               => Color.white
                };
            }

            _overlay.DOFade(0.75f, _openDuration).From(0f);
            _cardView.DOScale(Vector3.one, _openDuration)
                     .From(Vector3.one * 0.7f)
                     .SetEase(Ease.OutBack);
        }

        public void Hide()
        {
            _overlay.DOFade(0f, _closeDuration);
            _cardView.DOScale(Vector3.one * 0.7f, _closeDuration)
                     .SetEase(Ease.InBack)
                     .OnComplete(() => gameObject.SetActive(false));
        }

        // Click anywhere on overlay closes inspect
        public void OnPointerClick(PointerEventData eventData) => Hide();
    }
}