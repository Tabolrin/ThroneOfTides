using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ThroneOfTides.Core;
using ThroneOfTides.Data;

namespace ThroneOfTides.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class CardView : MonoBehaviour
    {
        [SerializeField] private Image            _cardBackground;
        [SerializeField] private Image            _cardArt;
        [SerializeField] private Image            _cardTypeSymbol;
        [SerializeField] private TextMeshProUGUI  _nameLabel;
        [SerializeField] private TextMeshProUGUI  _damageLabel;
        [SerializeField] private TextMeshProUGUI  _descriptionLabel;
        [SerializeField] private RectTransform    _damageBadge;
        [SerializeField] private Animator         _animator;

        public CardSO        CardData { get; private set; }
        public RectTransform Rect     { get; private set; }

        private void Awake()
        {
            Rect = (RectTransform)transform;
        }

        public void Setup(CardSO card)
        {
            CardData              = card;
            _nameLabel.text       = card.Name;
            _descriptionLabel.text = card.Description;
            _cardArt.sprite       = card.Art;

            if (_cardTypeSymbol != null)
                _cardTypeSymbol.sprite = card.CardTypeSymbol;

            // Show damage badge only for cards that deal damage
            bool hasDamage = card.Damage > 0;
            if (_damageBadge != null)
                _damageBadge.gameObject.SetActive(hasDamage);
            if (hasDamage)
                _damageLabel.text = card.Damage.ToString();

            _cardBackground.color = card.CardType == CardType.Combo
                ? new Color(1f, 0.85f, 0f)
                : new Color(0.3f, 0.5f, 1f);

            if (_animator != null && card.CardArtAnimator != null)
                _animator.runtimeAnimatorController = card.CardArtAnimator;
        }

        public Vector3 GetWorldPosition() => transform.position;
    }
}
