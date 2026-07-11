// Assets/_Game/2. Scripts/UI/CardInspectView.cs
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
        [Header("Structure")]
        [SerializeField] private Image         _overlay;
        [SerializeField] private RectTransform _cardView;

        // Banner background color no longer varies per type — only the symbol icon does.
        [Header("Type Banner")]
        [SerializeField] private Image _typeSymbolIcon;

        [Header("Identity")]
        [SerializeField] private Image           _cardArt;
        [SerializeField] private TextMeshProUGUI _cardName;
        [SerializeField] private TextMeshProUGUI _descriptionText;

        [Header("Damage Badge")]
        [SerializeField] private GameObject      _damageBadge;
        [SerializeField] private TextMeshProUGUI _damageLabel;

        [Header("Data")]
        [SerializeField] private CardTypePaletteSO _palette;

        [Header("Animation")]
        [SerializeField] private float _openDuration  = 0.25f;
        [SerializeField] private float _closeDuration = 0.18f;

        private void Awake() => gameObject.SetActive(false);

        // ── Public API ─────────────────────────────────────────────────────────

        public void Show(CardSO card)
        {
            gameObject.SetActive(true);

            _cardName.text        = card.Name;
            _descriptionText.text = card.Description;

            if (_cardArt != null && card.Art != null)
                _cardArt.sprite = card.Art;

            ApplyTypeVisuals(card);
            SetupDamageBadge(card);

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

        public void OnPointerClick(PointerEventData eventData) => Hide();

        // ── Private setup ──────────────────────────────────────────────────────

        private void ApplyTypeVisuals(CardSO card)
        {
            if (_palette == null) return;

            var visuals = _palette.GetVisuals(card.CardType);

            if (_typeSymbolIcon != null)
            {
                bool hasSymbol = visuals.TypeSymbol != null;
                _typeSymbolIcon.gameObject.SetActive(hasSymbol);
                if (hasSymbol) _typeSymbolIcon.sprite = visuals.TypeSymbol;
            }
        }

        private void SetupDamageBadge(CardSO card)
        {
            bool hasDamage = card.Damage > 0         ||
                             card.CardType == CardType.Combo ||
                             card.CardType == CardType.DOT;

            if (_damageBadge != null) _damageBadge.SetActive(hasDamage);

            if (hasDamage && _damageLabel != null)
                _damageLabel.text = card.CardType == CardType.DOT
                    ? $"{card.DotDamagePerTurn}\u00D7{card.DotDuration}"
                    : card.Damage.ToString();
        }
    }
}